using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Pragma.Navigator
{
    public class Navigator : MonoBehaviour, INavigator
    {
        [SerializeField] private NavigatorConfig _config;
        
        private readonly List<Screen> _screens = new();
        private readonly Stack<Screen> _opened = new();
        
        private readonly Queue<Screen> _next = new();
        private readonly List<WaitScreenData> _waitScreen = new();

        private IScreenFactory _factory;
        
        private Screen Current => _opened.Count > 0 ? _opened.Peek() : null;
        private IReadOnlyCollection<Screen> Prefabs => _config.Prefabs;
        private int Count => _opened.Count;

        public string CurrentScreenName => Current.name;

        private bool _isScreenOperationInProgress;

        private void Awake()
        {
            _factory ??= new UnityScreenFactory();

            foreach (var screen in Prefabs)
            {
                Create(screen);
            }

            foreach (var screen in _screens)
            {
                screen.gameObject.SetActive(false);
            }
        }

        private T Create<T>(T prefab) where T : Screen
        {
            var screen = _factory.Create(prefab, transform);
            screen.Initialize(this);
            _screens.Add(screen);

            return (T)screen;
        }

        private T Get<T>(T screen = null) where T : Screen
        {
            if (screen != null)
            {
                return (T)GetScreenByInstance(screen);
            }

            return GetScreenByType<T>();
        }

        private Screen GetScreenByInstance(Screen screen)
        {
            if (_screens.Contains(screen))
            {
                return screen;
            }

            var type = screen.GetType();

            var instance = _screens.Find(s => s.GetType() == type);

            return instance != null ? instance : Create(screen);
        }

        private T GetScreenByType<T>() where T : Screen
        {
            var screenInstance = _screens.Find(s => s is T);

            if (screenInstance == null)
            {
                var screenPrefab = Prefabs.FirstOrDefault(s => s is T);

                if (screenPrefab != null)
                {
                    screenInstance = Create(screenPrefab);
                }
                else
                    Debug.LogError("Screen instance " + typeof(T) + " can't created!");
            }

            return (T) screenInstance;
        }

        private void AddScreenToOpenedStack(Screen screen)
        {
            var rectTransform = screen.GetComponent<RectTransform>();
            rectTransform.SetSiblingIndex(Count);

            _opened.Push(screen);
        }

        public UniTask<T> Open<T>(string screenName, bool isPopup = false, bool isAllowedMultiOperation = false, ScreenAnimationBlockData screenAnimationBlockData = null, CancellationToken token = default) where T : Screen
        {
            var screen = Prefabs.FirstOrDefault(s => s.name == screenName);
        
            return Open((T) screen, isPopup, screenAnimationBlockData, isAllowedMultiOperation, token);
        }

        private async UniTask<T> Open<T>(T screen = null, bool isPopup = false, ScreenAnimationBlockData screenAnimationBlockData = null, bool isAllowedMultiOperation = false, CancellationToken token = default) where T : Screen
        {
            if (_isScreenOperationInProgress && !isAllowedMultiOperation)
            {
                return null;
            }

            _isScreenOperationInProgress = true;
            
            screen = Get(screen);

            if (Current == screen)
            {
                return (T)Current;
            }

            screen.IsPopup = isPopup;
            
            var prevScreen = Current;

            AddScreenToOpenedStack(screen);

            if (prevScreen != null)
            {
                await prevScreen.Blur(token);
            }
            
            await screen.Show(token);
            
            await screen.Focus(token);

            SendWaitScreenSignal(WaitScreenSignalType.Open, screen);
            
            _isScreenOperationInProgress = false;

            return screen;
        }

        public async UniTask Close(ScreenAnimationBlockData screenAnimationBlockData = null) => await Close(true, screenAnimationBlockData);

        public async UniTask Close(bool isTryOpenNextScreen, ScreenAnimationBlockData screenAnimationBlockData = null)
        {
            UniTask HideWithSendWait(Screen screen)
            {
                var hideTask = screen.Hide();
                SendWaitScreenSignal(WaitScreenSignalType.Close, screen);

                return hideTask;
            }

            _isScreenOperationInProgress = true;
            
            if (!_opened.TryPop(out var screen))
            {
                return;
            }

            await screen.Blur();

            if (isTryOpenNextScreen && !screen.IsPopup && _next.TryDequeue(out var nextScreen))
            {
                if (screen.IsPermissionOverlapOnHide && nextScreen.IsPermissionOverlapOnShow)
                {
                    await UniTask.WhenAll(HideWithSendWait(screen), Open(nextScreen));
                }
                else
                {
                    await HideWithSendWait(screen);
                    await Open(nextScreen);
                }
                
                return;
            }

            await HideWithSendWait(screen);

            if(Current != null)
                await Current.Focus();
            
            _isScreenOperationInProgress = false;
        }

        public async UniTask<T> Replace<T>(T screen = null, bool isPopup = false, ScreenAnimationBlockData replaceable = null, ScreenAnimationBlockData replacing = null) where T : Screen
        {
            await Close(false, replaceable);
            
            return await Open(screen, isPopup, replacing);
        }

        public void AddedToNextScreen<T>(T screen = null) where T : Screen
        {
            screen = Get<T>(screen);
            
            if (Current == null)
            {
                _ = Open(screen);
                return;
            }
                
            _next.Enqueue(screen);
        }

        private void SendWaitScreenSignal<T>(WaitScreenSignalType waitType, T screen = null) where T : Screen
        {
            screen = Get(screen);
            
            var waitScreenItem = _waitScreen.Find(item => item.screen == screen && item.type == waitType);

            if (waitScreenItem == null)
            {
                return;
            }

            waitScreenItem.waitCompletionSource.TrySetResult();

            _waitScreen.Remove(waitScreenItem);
        }

        public UniTask WaitScreenSignal<T>(T screen = null, WaitScreenSignalType waitType = WaitScreenSignalType.Open) where T : Screen
        {
            screen = Get(screen);

            var waitScreenItem = _waitScreen.Find(item => item.screen == screen && item.type == waitType);

            if (waitScreenItem == null)
            {
                var source = new UniTaskCompletionSource();
                
                _waitScreen.Add(new WaitScreenData()
                {
                    waitCompletionSource = source,
                    screen = screen,
                });

                return source.Task;
            }

            return waitScreenItem.waitCompletionSource.Task;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Pragma.ScreenNavigator
{
    public class ScreenNavigator : MonoBehaviour
    {
        [SerializeField] private ScreenNavigatorConfig config;
        
        private readonly List<Screen> _screenInstances = new();
        private readonly Stack<Screen> _openedScreens = new();
        
        private readonly Queue<Screen> _nextScreens = new();
        private readonly List<WaitScreenData> _waitScreen = new();

        private IScreenFactory _screenFactory;
        
        private Screen Current => _openedScreens.Count > 0 ? _openedScreens.Peek() : null;
        private List<Screen> Screens => config.Screens;
        private int Count => _openedScreens.Count;

        public string CurrentScreenName => Current.name;

        private bool _isScreenOperationInProgress;

        private void Awake()
        {
            _screenFactory ??= new UnityScreenFactory();
            
            Screens.ForEach(s =>
            {
                CreateScreen(s);
            });

            _screenInstances.ForEach(i => i.gameObject.SetActive(false));
        }

        public void SetScreenFactory(IScreenFactory screenFactory)
        {
            _screenFactory = screenFactory;
        }

        private T CreateScreen<T>(T prefab) where T : Screen
        {
            var screen = _screenFactory.Create(prefab, transform);
            screen.ScreenNavigator = this;
            _screenInstances.Add(screen);

            return (T)screen;
        }

        private T GetScreen<T>(T screen = null) where T : Screen
        {
            if (screen != null)
            {
                return (T)GetScreenByInstance(screen);
            }

            return GetScreenByType<T>();
        }

        private Screen GetScreenByInstance(Screen screen)
        {
            if (_screenInstances.Contains(screen))
            {
                return screen;
            }

            var type = screen.GetType();

            var instance = _screenInstances.Find(s => s.GetType() == type);

            return instance != null ? instance : CreateScreen(screen);
        }

        private T GetScreenByType<T>() where T : Screen
        {
            var screenInstance = _screenInstances.Find(s => s is T);

            if (screenInstance == null)
            {
                var screenPrefab = Screens.Find(s => s is T);

                if (screenPrefab != null)
                {
                    screenInstance = CreateScreen(screenPrefab);
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

            _openedScreens.Push(screen);
        }

        public UniTask<T> Open<T>(string screenName, bool isPopup = false, bool isAllowedMultiOperation = false, ScreenAnimationBlockData screenAnimationBlockData = null, CancellationToken token = default) where T : Screen
        {
            var screen = Screens.Find(s => s.name == screenName);
        
            return Open((T) screen, isPopup, screenAnimationBlockData, isAllowedMultiOperation, token);
        }

        private async UniTask<T> Open<T>(T screen = null, bool isPopup = false, ScreenAnimationBlockData screenAnimationBlockData = null, bool isAllowedMultiOperation = false, CancellationToken token = default) where T : Screen
        {
            if (_isScreenOperationInProgress && !isAllowedMultiOperation)
            {
                return null;
            }

            _isScreenOperationInProgress = true;
            
            screen = GetScreen(screen);

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
            
            await screen.Show(token, screenAnimationBlockData : screenAnimationBlockData);
            
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
                var hideTask = screen.Hide(screenAnimationBlockData : screenAnimationBlockData);
                SendWaitScreenSignal(WaitScreenSignalType.Close, screen);

                return hideTask;
            }

            _isScreenOperationInProgress = true;
            
            if (!_openedScreens.TryPop(out var screen))
            {
                return;
            }

            await screen.Blur();

            if (isTryOpenNextScreen && !screen.IsPopup && _nextScreens.TryDequeue(out var nextScreen))
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

        public UniTask<T> OpenIsNeeded<T>(T screen = null, bool isPopup = false) where T : Screen
        {
            screen = GetScreen(screen);

            screen.IsPopup = isPopup;

            return screen.IsNeedToOpen() ? Open(screen) : UniTask.FromResult<T>(null);
        }
        
        public UniTask<T> ReplaceIsNeeded<T>(T screen = null) where T : Screen
        {
            screen = GetScreen<T>(screen);
            
            return screen.IsNeedToOpen() ? Replace<T>(screen) : UniTask.FromResult<T>(null);
        }
        
        public void AddedToNextScreenIsNeeded<T>(T screen = null) where T : Screen
        {
            screen = GetScreen<T>(screen);

            if (screen.IsNeedToOpen())
            {
                AddedToNextScreen(screen);
            }
        }
        
        public void AddedToNextScreen<T>(T screen = null) where T : Screen
        {
            screen = GetScreen<T>(screen);
            
            if (Current == null)
            {
                _ = Open(screen);
                return;
            }
                
            _nextScreens.Enqueue(screen);
        }

        private void SendWaitScreenSignal<T>(WaitScreenSignalType waitType, T screen = null) where T : Screen
        {
            screen = GetScreen(screen);
            
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
            screen = GetScreen(screen);

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
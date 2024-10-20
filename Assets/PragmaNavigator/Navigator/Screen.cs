using System.Threading;
using Cysharp.Threading.Tasks;
using Pragma.SignalBus;
using UnityEngine;

namespace Pragma.Navigator
{
    public class Screen : MonoBehaviour, IScreen
    {
        [SerializeField] private ScreenAnimationTurntable _screenShowAnimationTurntable = new();
        [SerializeField] private ScreenAnimationTurntable _screenHideAnimationTurntable = new();
        [SerializeField] private ScreenAnimationTurntable _screenFocusAnimationTurntable = new();
        [SerializeField] private ScreenAnimationTurntable _screenBlurAnimationTurntable = new();

        private ISignalBus _signalBus;
        
        public bool IsPermissionOverlapOnShow => _screenShowAnimationTurntable.IsPermissionOverlap;
        public bool IsPermissionOverlapOnHide => _screenHideAnimationTurntable.IsPermissionOverlap;

        public bool IsPopup { get; set; }

        public ISignalRegistrar Registrar => _signalBus;
        public INavigator Navigator { get; private set; }

        public void Initialize(INavigator navigator)
        {
            Navigator = navigator;

            var visuals = GetComponentsInChildren<IVisual>();

            foreach (var visual in visuals)
            {
                visual.Initialize(this);
            }
        }
        
        private async UniTask DoAnimation(
            ScreenAnimationTurntable screenAnimationTurntable, 
            ScreenAnimationBlockData screenAnimationBlockData = null, 
            CancellationToken cancellationToken = default)
        {
            await screenAnimationTurntable.PlayAnimations(cancellationToken, screenAnimationBlockData);
        }

        protected virtual void Awake()
        {
            _signalBus = new SignalBus.SignalBus();
            
            _screenHideAnimationTurntable.SetAnimationObject(transform);
            _screenShowAnimationTurntable.SetAnimationObject(transform);
            _screenFocusAnimationTurntable.SetAnimationObject(transform);
            _screenBlurAnimationTurntable.SetAnimationObject(transform);
        }

        public virtual async UniTask Show(CancellationToken token = default)
        {
            gameObject.SetActive(true);
            _signalBus.Send<ShowSignal>();
            await DoAnimation(_screenShowAnimationTurntable, null, token);
            _signalBus.Send<ShowCompletedSignal>();
        }

        public virtual async UniTask Hide(CancellationToken token = default)
        {
            _signalBus.Send<HideSignal>();
            await DoAnimation(_screenHideAnimationTurntable, null, token);
            _signalBus.Send<HideCompletedSignal>();
            gameObject.SetActive(false);
        }
        
        public async UniTask Focus(CancellationToken token = default)
        {
            _signalBus.Send<FocusSignal>();
            await DoAnimation(_screenFocusAnimationTurntable, cancellationToken: token);
            _signalBus.Send<FocusCompletedSignal>();
        }

        public async UniTask Blur(CancellationToken token = default)
        {
            _signalBus.Send<BlurSignal>();
            await DoAnimation(_screenFocusAnimationTurntable, cancellationToken: token);
            _signalBus.Send<BlurCompletedSignal>();
        }

        // protected virtual UniTask OnFocus(CancellationToken token){return UniTask.CompletedTask;}
        // protected virtual UniTask OnBlur(CancellationToken token){return UniTask.CompletedTask;}
        // protected virtual UniTask OnShow(CancellationToken token){return UniTask.CompletedTask;}
        // protected virtual UniTask OnShowCompleted(CancellationToken token){return UniTask.CompletedTask;}
        // protected virtual UniTask OnHide(CancellationToken token){return UniTask.CompletedTask;}
        // protected virtual UniTask OnHideCompleted(CancellationToken token){return UniTask.CompletedTask;}
    }
}
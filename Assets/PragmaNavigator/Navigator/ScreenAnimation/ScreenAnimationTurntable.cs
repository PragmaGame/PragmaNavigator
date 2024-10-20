using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Pragma.Navigator
{
    [Serializable]
    public class ScreenAnimationTurntable
    {
        [SerializeField] private bool _isPermissionOverlap;
        
        [SerializeReference] private IShowAnimationProcessor _showScreenAnimation = new ParallelAnimationProcessor();
        [SerializeReference] private IScreenAnimation[] _animations = new IScreenAnimation[0];
        
        public bool IsPermissionOverlap => _isPermissionOverlap;

        public IScreenAnimation[] Animations => _animations;

        private Transform _animationObject;

        public void SetAnimationObject(Transform animationObject)
        {
            _animationObject = animationObject;

            foreach (var animation in _animations)
            {
                animation.ScreenTransformInject = _animationObject;
            }
        }

        public async UniTask PlayAnimations(
            CancellationToken token, 
            IShowAnimationProcessor showScreenAnimation = null, 
            string[] idAnimations = null, 
            IScreenAnimation[] customAnimations = null)
        {
            showScreenAnimation ??= _showScreenAnimation;

            var selection = idAnimations == null ? _animations : _animations.Where(animation => idAnimations.Contains(animation.Id));

            var showList = new List<UniTask<bool>>();
            
            showList.AddRange(Enumerable.Select(selection, animation => animation.DoAnimation(token)));

            if (customAnimations != null && customAnimations.Length != 0)
            {
                foreach (var animation in _animations)
                {
                    animation.ScreenTransformInject = _animationObject;
                }
                
                showList.AddRange(Enumerable.Select(customAnimations, animation => animation.DoAnimation(token)));
            }
            
            await showScreenAnimation.Show(showList);
        }

        public async UniTask PlayAnimations(CancellationToken token, ScreenAnimationBlockData screenAnimationBlockData = null)
        {
            if (screenAnimationBlockData == null)
            {
                await PlayAnimations(token, _showScreenAnimation);
            }
            else
            {
                await PlayAnimations(token, screenAnimationBlockData.ShowScreenAnimationProcessor, screenAnimationBlockData.IdAnimations,
                    screenAnimationBlockData.CustomAnimations);
            }
        }
    }
}
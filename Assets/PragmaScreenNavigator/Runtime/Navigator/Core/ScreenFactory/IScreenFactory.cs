using UnityEngine;

namespace Pragma.ScreenNavigator
{
    public interface IScreenFactory
    {
        public Screen Create(Screen prefab, Transform parent);
    }
}
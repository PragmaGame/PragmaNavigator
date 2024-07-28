using UnityEngine;

namespace Pragma.ScreenNavigator
{
    public class UnityScreenFactory : IScreenFactory
    {
        public Screen Create(Screen prefab, Transform parent)
        {
            return Object.Instantiate(prefab, parent);
        }
    }
}
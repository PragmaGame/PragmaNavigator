using UnityEngine;

namespace Pragma.Navigator
{
    public interface IScreenFactory
    {
        public Screen Create(Screen prefab, Transform parent);
    }
}
using System.Collections.Generic;
using UnityEngine;

namespace Pragma.Navigator
{
    [CreateAssetMenu(fileName = nameof(NavigatorConfig), menuName = "Game/Configs/" + nameof(NavigatorConfig))]
    public class NavigatorConfig : ScriptableObject
    {
        [SerializeField] private Screen[] _prefabs;

        public IReadOnlyCollection<Screen> Prefabs => _prefabs;
    }
}
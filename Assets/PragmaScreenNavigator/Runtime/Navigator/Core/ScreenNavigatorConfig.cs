using System.Collections.Generic;
using UnityEngine;

namespace Pragma.ScreenNavigator
{
    [CreateAssetMenu(fileName = nameof(ScreenNavigatorConfig), menuName = "Game/Configs/" + nameof(ScreenNavigatorConfig))]
    public class ScreenNavigatorConfig : ScriptableObject
    {
        [SerializeField] private List<Screen> _screens;

        public List<Screen> Screens => _screens;
    }
}
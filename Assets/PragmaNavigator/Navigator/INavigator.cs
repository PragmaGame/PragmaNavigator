using Cysharp.Threading.Tasks;

namespace Pragma.Navigator
{
    public interface INavigator
    {
        public UniTask WaitScreenSignal<T>(T screen = null, WaitScreenSignalType waitType = WaitScreenSignalType.Open)
            where T : Screen;
    }
}
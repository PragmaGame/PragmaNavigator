using Cysharp.Threading.Tasks;

namespace Pragma.Navigator
{
    public static class NavigatorExtensions
    {
        public static UniTask WaitScreenSignal(this Screen screen, WaitScreenSignalType signalType)
        {
            return screen.Navigator.WaitScreenSignal(screen, signalType);
        }
    }
}
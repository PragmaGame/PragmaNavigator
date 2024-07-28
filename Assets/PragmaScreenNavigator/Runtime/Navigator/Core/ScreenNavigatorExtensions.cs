using Cysharp.Threading.Tasks;

namespace Pragma.ScreenNavigator
{
    public static class ScreenNavigatorExtensions
    {
        public static UniTask WaitScreenEvent(this Screen screen, WaitScreenSignalType signalType)
        {
            return screen.ScreenNavigator.WaitScreenSignal(screen, signalType);
        }
    }
}
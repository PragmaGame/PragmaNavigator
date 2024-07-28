using Cysharp.Threading.Tasks;

namespace Pragma.ScreenNavigator
{
    public class WaitScreenData
    {
        public WaitScreenSignalType type;
        public UniTaskCompletionSource waitCompletionSource;
        public Screen screen;
    }
}
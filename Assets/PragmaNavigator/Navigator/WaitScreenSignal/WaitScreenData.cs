using Cysharp.Threading.Tasks;

namespace Pragma.Navigator
{
    public class WaitScreenData
    {
        public WaitScreenSignalType type;
        public UniTaskCompletionSource waitCompletionSource;
        public Screen screen;
    }
}
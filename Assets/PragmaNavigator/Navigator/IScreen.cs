using Pragma.SignalBus;

namespace Pragma.Navigator
{
    public interface IScreen
    {
        public ISignalRegistrar Registrar { get;}
        public INavigator Navigator { get;}
    }
}
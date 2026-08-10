using LowDefMustard.Control;

namespace Frankie.Utils.UI
{
    public interface IUIMoveInterceptor
    {
        public bool TryMove(ControllerInputType controllerInputType);
    }
}

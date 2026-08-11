using LowDefMustard.Control;

namespace LowDefMustard.UIBox
{
    public interface IUIMoveInterceptor
    {
        public bool TryMove(ControllerInputType controllerInputType);
    }
}

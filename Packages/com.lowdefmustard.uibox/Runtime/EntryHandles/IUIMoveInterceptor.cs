using LowDefMustard.Control;

namespace LowDefMustard.UIBox
{
    public interface IUIMoveInterceptor
    {
        public bool TryMove(ControllerInputType controllerInputType, out bool isHighlightMove); // isHighlightMove:  moved an inner highlight (vs. e.g. adjusting a value)
    }
}

using Frankie.Control;

namespace Frankie.Utils.UI
{
    public interface IUIMoveInterceptor
    {
        public bool TryMove(PlayerInputType playerInputType);
    }
}

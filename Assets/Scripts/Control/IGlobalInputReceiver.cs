namespace Frankie.Control
{
    public interface IGlobalInputReceiver
    {
        bool HandleGlobalInput(PlayerInputType playerInputType);
    }
}

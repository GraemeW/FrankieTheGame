namespace Frankie.Control
{
    public interface IGlobalInputReceiver
    {
        bool HandleGlobalInput(ControllerInputType controllerInputType);
    }
}

namespace Frankie.Control
{
    public class ReceiverModifiedData
    {
        public readonly IInputReceiver inputReceiver;
        public readonly bool writingState;

        public ReceiverModifiedData(IInputReceiver inputReceiver, bool writingState = false)
        {
            this.inputReceiver = inputReceiver;
            this.writingState = writingState;
        }
    }
}

namespace LowDefMustard.Utils.Editor
{
    public class ActionClassification
    {
        public readonly string resolvedAction;
        public readonly bool isIdleSource;
        public readonly bool isStandStillSource;
        public readonly bool isRecognized;

        public ActionClassification(string resolvedAction, bool isIdleSource, bool isStandStillSource, bool isRecognized)
        {
            this.resolvedAction = resolvedAction;
            this.isIdleSource = isIdleSource;
            this.isStandStillSource = isStandStillSource;
            this.isRecognized = isRecognized;
        }
    }
}

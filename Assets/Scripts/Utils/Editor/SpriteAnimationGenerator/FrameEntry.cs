namespace Frankie.Utils.Editor
{
    public class FrameEntry
    {
        public readonly string character;
        public readonly string action;
        public readonly int frame;
        public readonly string assetPath;
        public readonly bool isIdleSource;
        public readonly bool isStandStillSource;

        public FrameEntry(string character, string action, int frame, string assetPath, bool isIdleSource, bool isStandStillSource)
        {
            this.character = character;
            this.action = action;
            this.frame = frame;
            this.assetPath = assetPath;
            this.isIdleSource = isIdleSource;
            this.isStandStillSource = isStandStillSource;
        }
    }
}

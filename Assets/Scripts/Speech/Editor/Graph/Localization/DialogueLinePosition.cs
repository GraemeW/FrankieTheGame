namespace Frankie.Speech.Editor
{
    internal readonly struct DialogueLinePosition
    {
        public readonly SpeechGraphNode line;
        public readonly int depth;
        public readonly int breadth;

        public DialogueLinePosition(SpeechGraphNode line, int depth, int breadth)
        {
            this.line = line;
            this.depth = depth;
            this.breadth = breadth;
        }
    }
}

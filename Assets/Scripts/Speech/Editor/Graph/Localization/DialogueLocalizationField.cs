namespace Frankie.Speech.Editor
{
    internal readonly struct DialogueLocalizationField
    {
        // Note:
        //  - baseID: stable per line + field (node ID based) ~ used to track the last-synced value
        //  - key: the table entry key the value lives under

        public readonly string baseID;
        public readonly string key;
        public readonly string graphValue;

        public DialogueLocalizationField(string baseID, string key, string graphValue)
        {
            this.baseID = baseID;
            this.key = key;
            this.graphValue = graphValue ?? "";
        }
    }
}

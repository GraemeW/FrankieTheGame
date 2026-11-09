namespace Frankie.Speech.Editor
{
    internal static class DialogueLineKeys
    {
        // Speech table key format: {dialogueName}.{depth}.{breadth}.{field}

        // Const Tunables
        public const string speakerField = "Speaker";
        public const string textField = "Text";

        public static string Get(string dialogueName, int depth, int breadth, string field) => $"{GetPrefix(dialogueName)}{depth}.{breadth}.{field}";
        public static string GetPrefix(string dialogueName) => dialogueName + ".";
    }
}

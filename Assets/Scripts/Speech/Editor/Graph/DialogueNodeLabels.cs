using System.Linq;

namespace Frankie.Speech.Editor
{
    internal static class DialogueNodeLabels
    {
        // Const Tunables
        private const int _maxLabelLength = 48;
        private const string _unplacedPosition = "-.-";

        public static string Line(int depth, int breadth, string speaker, string text)
        {
            string position = depth < 0 || breadth < 0 ? _unplacedPosition : $"{depth}.{breadth}";
            string body = string.IsNullOrWhiteSpace(text) ? "(no text)" : text;
            return Clean(string.IsNullOrWhiteSpace(speaker) ? $"{position} {body}" : $"{position} {speaker}: {body}");
        }

        public static string Condition(string nodeID) => $"Condition - {nodeID}";

        private static string Clean(string value)
        {
            // One line, no '/' (menu paths split on it), truncated
            string singleLine = string.Join(" ", (value ?? "").Split('\n', '\r', '\t').Where(part => part.Length > 0)).Trim().Replace('/', '∕');
            return singleLine.Length <= _maxLabelLength ? singleLine : singleLine[..(_maxLabelLength - 1)].TrimEnd() + "…";
        }
    }
}

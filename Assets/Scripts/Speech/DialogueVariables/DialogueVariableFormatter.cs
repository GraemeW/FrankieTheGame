using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using Unity.Scripting.LifecycleManagement;

namespace Frankie.Speech
{
    public static class DialogueVariableFormatter
    {
        [NoAutoStaticsCleanup] private static readonly Regex _placeholderRegex = new(@"\{(\d+)\}", RegexOptions.Compiled);

        #region PublicMethods
        public static string Format(string text, IReadOnlyList<ScriptableObject> variables, DialogueVariableContext context)
        {
            if (string.IsNullOrEmpty(text) || text.IndexOf('{') < 0) { return text; }
            var resolvedValues = new string[variables?.Count ?? 0]; // Per-call cache ~ a repeated placeholder resolves its variable once
            return _placeholderRegex.Replace(text, match => Resolve(match, variables, context, resolvedValues));
        }

        public static SortedSet<int> GetPlaceholderIndices(string text)
        {
            var indices = new SortedSet<int>();
            if (string.IsNullOrEmpty(text) || text.IndexOf('{') < 0) { return indices; }
            foreach (Match match in _placeholderRegex.Matches(text))
            {
                if (int.TryParse(match.Groups[1].Value, out int index)) { indices.Add(index); }
            }
            return indices;
        }

        public static int GetContiguousCount(string text)
        {
            // Placeholders count from {0} up to the first gap ({0}, {2} -> only 1 placeholder)
            SortedSet<int> indices = GetPlaceholderIndices(text);
            int count = 0;
            while (indices.Contains(count)) { count++; }
            return count;
        }
        #endregion

        #region PrivateMethods
        private static string Resolve(Match match, IReadOnlyList<ScriptableObject> variables, DialogueVariableContext context, string[] resolvedValues)
        {
            if (variables == null || !int.TryParse(match.Groups[1].Value, out int index) || index >= variables.Count) { return ""; }
            return resolvedValues[index] ??= ResolveVariable(variables[index], context);
        }

        private static string ResolveVariable(ScriptableObject variable, DialogueVariableContext context)
        {
            if (variable == null || variable is not IDialogueVariable dialogueVariable) { return ""; }
            return dialogueVariable.GetDialogueVariable(context) ?? "";
        }
        #endregion
    }
}

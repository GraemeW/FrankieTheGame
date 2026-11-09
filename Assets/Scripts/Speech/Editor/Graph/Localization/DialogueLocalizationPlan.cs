using System.Collections.Generic;
using System.Linq;

namespace Frankie.Speech.Editor
{
    internal sealed class DialogueLocalizationPlan
    {
        //  - Init/Re-Init:
        //      - graph == table -> in sync (base settles)
        //      - no table entry -> push
        //      - no base (i.e. first sync since load) -> pull (tables win)
        //  - Update:
        //      - graph changed since base, table not -> graph push to table
        //      - table changed, graph not -> pull table to graph
        //      - both -> graph push to table (graph wins conflict)
        //  - Remove:  table entries under the dialogue that no field claims -> remove

        // State
        public Dictionary<string, string> pushes { get; } = new();
        public Dictionary<string, string> pulls { get; } = new();
        public List<string> removals { get; } = new();
        public List<string> conflicts { get; } = new();
        public Dictionary<string, string> settledBases { get; } = new();

        public bool hasTableWrites => pushes.Count > 0 || removals.Count > 0;
        public bool hasGraphWrites => pulls.Count > 0;

        public DialogueLocalizationPlan(IEnumerable<DialogueLocalizationField> fields, IReadOnlyDictionary<string, string> tableEntries, IReadOnlyDictionary<string, string> bases)
        {
            var claimedKeys = new HashSet<string>();
            foreach (DialogueLocalizationField field in fields)
            {
                claimedKeys.Add(field.key);
                if (!tableEntries.TryGetValue(field.key, out string tableValue) || tableValue == null)
                {
                    pushes[field.key] = field.graphValue;
                    continue;
                }
                if (tableValue == field.graphValue) { settledBases[field.baseID] = field.graphValue; continue; }
                if (!bases.TryGetValue(field.baseID, out string baseValue)) { pulls[field.baseID] = tableValue; continue; }

                bool graphChanged = field.graphValue != baseValue;
                bool tableChanged = tableValue != baseValue;
                if (graphChanged)
                {
                    pushes[field.key] = field.graphValue;
                    if (tableChanged) { conflicts.Add(field.key); }
                }
                else { pulls[field.baseID] = tableValue; }
            }

            removals.AddRange(tableEntries.Keys.Where(key => !claimedKeys.Contains(key)).OrderBy(key => key));
        }
    }
}

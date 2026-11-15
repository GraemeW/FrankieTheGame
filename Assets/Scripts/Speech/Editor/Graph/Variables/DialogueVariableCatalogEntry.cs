using UnityEngine;

namespace Frankie.Speech.Editor
{
    internal readonly struct DialogueVariableCatalogEntry
    {
        public readonly ScriptableObject variable;
        public readonly string groupLabel; // Highest implementing type (e.g. Inventory Item)
        public readonly string typeLabel; // Concrete type (e.g. Action Item)

        public DialogueVariableCatalogEntry(ScriptableObject variable, string groupLabel, string typeLabel)
        {
            this.variable = variable;
            this.groupLabel = groupLabel;
            this.typeLabel = typeLabel;
        }
    }
}

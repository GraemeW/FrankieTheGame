using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Frankie.Speech.Editor
{
    internal static class DialogueVariableCatalog
    {
        #region PublicMethods
        public static List<DialogueVariableCatalogEntry> Collect()
        {
            var entries = new List<DialogueVariableCatalogEntry>();
            var seen = new HashSet<ScriptableObject>();
            foreach (Type groupType in GetGroupTypes())
            {
                string groupLabel = GetLabel(groupType);
                foreach (string guid in AssetDatabase.FindAssets($"t:{groupType.Name}"))
                {
                    var variable = AssetDatabase.LoadAssetAtPath<ScriptableObject>(AssetDatabase.GUIDToAssetPath(guid));
                    if (variable == null || variable is not IDialogueVariable || !seen.Add(variable)) { continue; }
                    entries.Add(new DialogueVariableCatalogEntry(variable, groupLabel, GetLabel(variable.GetType())));
                }
            }

            return entries
                .OrderBy(entry => entry.groupLabel, StringComparer.OrdinalIgnoreCase)
                .ThenBy(entry => entry.typeLabel, StringComparer.OrdinalIgnoreCase)
                .ThenBy(entry => entry.variable.name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public static List<Type> GetGroupTypes()
        {
            return TypeCache.GetTypesDerivedFrom<IDialogueVariable>()
                .Where(type => typeof(ScriptableObject).IsAssignableFrom(type))
                .Select(GetGroupType)
                .Distinct()
                .ToList();
        }

        public static Type GetGroupType(Type type)
        {
            // Walk up while the base type still implements the interface (e.g. ActionItem -> InventoryItem)
            Type groupType = type;
            while (groupType.BaseType != null && typeof(IDialogueVariable).IsAssignableFrom(groupType.BaseType)) { groupType = groupType.BaseType; }
            return groupType;
        }
        #endregion
        
        #region PrivateMethods
        private static string GetLabel(Type type) => ObjectNames.NicifyVariableName(type.Name);
        #endregion
    }
}

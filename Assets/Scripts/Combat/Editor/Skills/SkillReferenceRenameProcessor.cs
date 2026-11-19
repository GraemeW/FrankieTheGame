using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Unity.Scripting.LifecycleManagement;
using LowDefMustard.Core;
using LowDefMustard.Utils.Editor;
using Object = UnityEngine.Object;

namespace Frankie.Combat.Editor
{
    internal sealed class SkillReferenceRenameProcessor : AssetPostprocessor
    {
        // Note:  Skill references are stored by name ~ a rename silently unsets them
        // So ~ here we ensure that renaming a skill re-points every [SkillReference] name that used the old one (skill trees, skill pools, BattleAI priorities)

        // Const Tunables
        private const string _searchRoot = "Assets";
        private const string _prefabFilter = "t:Prefab";

        // State
        [NoAutoStaticsCleanup] private static HashSet<Type> _cachedOwnerTypes; // Types only change on a domain reload ~ which clears this

        #region UnityMethods
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) { return; }

            for (int i = 0; i < movedAssets.Length; i++)
            {
                string oldName = Path.GetFileNameWithoutExtension(movedFromAssetPaths[i]);
                string newName = Path.GetFileNameWithoutExtension(movedAssets[i]);
                if (oldName == newName) { continue; } // Moved, not renamed
                if (!typeof(Skill).IsAssignableFrom(AssetDatabase.GetMainAssetTypeAtPath(movedAssets[i]))) { continue; }

                EditorApplication.delayCall += () => RenameSkillReferences(oldName, newName); // After the import ~ cannot do asset writes mid-import
            }
        }
        #endregion

        #region InternalMethods
        // Note:  Internal methods below for test visibility
        private static void RenameSkillReferences(string oldName, string newName)
        {
            var updatedPaths = new List<string>();
            if (string.IsNullOrWhiteSpace(oldName) || string.IsNullOrWhiteSpace(newName) || oldName == newName) { return; }
            
            if (IsSkillNameInUse(oldName))
            {
                LDMLog.Info(LDMLogCategory.Combat, $"Skill '{oldName}' was renamed to '{newName}', but another skill is still named '{oldName}' ~ left references to '{oldName}' as they are.");
                return;
            }

            HashSet<Type> ownerTypes = GetOwnerTypes();
            foreach (string path in FindOwnerAssetPaths(ownerTypes))
            {
                int updatedCount = AssetDatabase.LoadAllAssetsAtPath(path)
                    .Where(asset => asset != null && ownerTypes.Contains(asset.GetType()))
                    .Sum(asset => RenameSkillReferences(asset, oldName, newName));
                if (updatedCount == 0) { continue; }

                AssetDatabase.SaveAssetIfDirty(AssetDatabase.LoadMainAssetAtPath(path));
                updatedPaths.Add(path);
            }

            if (updatedPaths.Count > 0) { LDMLog.Info(LDMLogCategory.Combat, $"Skill '{oldName}' renamed to '{newName}' ~ updated its references in:  {string.Join(", ", updatedPaths)}"); }
        }

        internal static int RenameSkillReferences(Object target, string oldName, string newName)
        {
            if (target == null) { return 0; }

            HashSet<string> skillReferenceFields = GetSkillReferenceFieldNames();
            using var serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.GetIterator();
            int updatedCount = 0;
            while (property.Next(true))
            {
                if (!skillReferenceFields.Contains(property.name)) { continue; } // e.g. skillReference, upSkillReference, skillReferences
                if (property.propertyType == SerializedPropertyType.String) { updatedCount += RenameIfMatch(property, oldName, newName); }
                else if (property.IsStringArray())
                {
                    for (int i = 0; i < property.arraySize; i++) { updatedCount += RenameIfMatch(property.GetArrayElementAtIndex(i), oldName, newName); }
                }
            }

            if (updatedCount == 0) { return 0; }
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
            return updatedCount;
        }

        internal static HashSet<Type> GetOwnerTypes()
        {
            // Asset / component types that can hold a [SkillReference] field ~ directly, or nested in serializable classes, lists, arrays
            if (_cachedOwnerTypes != null) { return _cachedOwnerTypes; }

            var containsCache = new Dictionary<Type, bool>();
            _cachedOwnerTypes = TypeCache.GetTypesDerivedFrom<ScriptableObject>()
                .Concat(TypeCache.GetTypesDerivedFrom<MonoBehaviour>())
                .Where(type => !type.IsAbstract && !type.IsGenericTypeDefinition && ContainsSkillReference(type, containsCache))
                .ToHashSet();
            return _cachedOwnerTypes;
        }
        #endregion

        #region PrivateMethods
        private static HashSet<string> GetSkillReferenceFieldNames() => TypeCache.GetFieldsWithAttribute<SkillReferenceAttribute>().Select(field => field.Name).ToHashSet();
        private static bool IsSkillNameInUse(string skillName) => AssetDatabase.FindAssets($"t:{nameof(Skill)}").Select(AssetDatabase.GUIDToAssetPath).Any(path => Path.GetFileNameWithoutExtension(path) == skillName);
        
        private static int RenameIfMatch(SerializedProperty property, string oldName, string newName)
        {
            if (property.stringValue != oldName) { return 0; }
            property.stringValue = newName;
            return 1;
        }
        
        private static IEnumerable<string> FindOwnerAssetPaths(HashSet<Type> ownerTypes)
        {
            var searchTerms = ownerTypes.Where(type => typeof(ScriptableObject).IsAssignableFrom(type)).Select(type => $"t:{type.Name}").ToList();
            if (ownerTypes.Any(type => typeof(MonoBehaviour).IsAssignableFrom(type))) { searchTerms.Add(_prefabFilter); } // Components can't be found by type ~ load prefabs
            if (searchTerms.Count == 0) { return Enumerable.Empty<string>(); } // An empty filter finds every asset

            return AssetDatabase.FindAssets(string.Join(" ", searchTerms), new[] { _searchRoot }).Select(AssetDatabase.GUIDToAssetPath).Distinct();
        }

        private static bool ContainsSkillReference(Type type, Dictionary<Type, bool> containsCache)
        {
            if (containsCache.TryGetValue(type, out bool contains)) { return contains; }
            containsCache[type] = false; // Cycle guard

            foreach (FieldInfo field in SerializedFieldUtility.GetSerializedFields(type))
            {
                if (field.IsDefined(typeof(SkillReferenceAttribute), false)) { return containsCache[type] = true; }
                if (field.IsDefined(typeof(SerializeReference), false)) { continue; } // Held type only known per instance ~ not covered (none in use)

                Type valueType = SerializedFieldUtility.GetCollectionElementType(field.FieldType);
                if (!SerializedFieldUtility.IsInlineSerializable(valueType)) { continue; } // Object fields point at other assets ~ checked on their own
                if (ContainsSkillReference(valueType, containsCache)) { return containsCache[type] = true; }
            }
            return false;
        }
        #endregion
    }
}

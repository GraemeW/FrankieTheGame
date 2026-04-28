#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization.Tables;

namespace Frankie.Utils.Editor
{
    [InitializeOnLoad]
    public class LocalizationDeletionHandler : AssetModificationProcessor
    {
        private static AssetDeleteResult OnWillDeleteAsset(string assetPath, RemoveAssetOptions options)
        {
            var asset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
            if (asset is ScriptableObject scriptableObject and ILocalizable localizable)
            {
                HandleDeletion(localizable.localizationTableType, scriptableObject, localizable);
            }
            else if (asset is GameObject gameObject && gameObject != null && gameObject.TryGetComponent(out localizable))
            {
                HandleDeletion(localizable.localizationTableType, gameObject,  localizable);
            }
            
            // Pass back to Unity to continue deletion
            return AssetDeleteResult.DidNotDelete;
        }

        static LocalizationDeletionHandler()
        {
            ILocalizable.onBeforeDestroyedInEditor -= HandleDeletion;
            ILocalizable.onBeforeDestroyedInEditor += HandleDeletion;
        }

        private static void HandleDeletion(LocalizationTableType localizationTableType, Object targetObject, ILocalizable localizable)
        {
            Debug.Log($"{targetObject.name} is being deleted.  Deleting localization entries.");
            int deletionCount = 0;
            foreach (TableEntryReference tableEntryReference in FilterLocalizationEntries(localizationTableType, targetObject, localizable))
            {
                //LocalizationTool.RemoveEntry(localizationTableType, tableEntryReference);
                Debug.Log($"Removing entry {tableEntryReference.KeyId}");
                // TODO:  Uncomment, actually remove
                deletionCount++;
            }
            Debug.Log($"{deletionCount} localization entries deleted.");
        }
        
        private static List<TableEntryReference> FilterLocalizationEntries(LocalizationTableType localizationTableType, Object targetObject, ILocalizable targetLocalizable)
        {
            var deletableEntries = new List<TableEntryReference>();
            if (targetLocalizable == null) { return deletableEntries; }
            
            List<TableEntryReference> tableEntryReferences = targetLocalizable.GetLocalizationEntries().ToList();
            if (targetObject is ScriptableObject || targetLocalizable is not MonoBehaviour targetMonoBehaviour) { return tableEntryReferences; }
            if (!IsPrefabLocalizable(targetMonoBehaviour, out ILocalizable prefabLocalizable)) { return tableEntryReferences; }

            // Note:  We cannot use standard prefab utility methods (e.g. GetPropertyModifications()) to check
            //        --> for reasons? these are not valid if this function is called from OnDestroy() or OnDisable()
            //        Approach is thus to manually compare GUID entries
            HashSet<long> uniqueTargetKeyIDs = GetUniqueKeyIDs(localizationTableType, tableEntryReferences);
            HashSet<long> uniquePrefabKeyIDs = GetUniqueKeyIDs(localizationTableType, prefabLocalizable.GetLocalizationEntries());

            deletableEntries.AddRange(uniqueTargetKeyIDs.Where(keyID => !uniquePrefabKeyIDs.Contains(keyID)).Select(deletableTableEntryReference => (TableEntryReference)deletableTableEntryReference));

            return deletableEntries;
        }
        
        private static HashSet<long> GetUniqueKeyIDs(LocalizationTableType localizationTableType, IList<TableEntryReference> tableEntryReferences)
        {
            var keyIDs = new HashSet<long>();
            foreach (TableEntryReference ambiguousTableEntryReference in tableEntryReferences)
            {
                TableEntryReference tableEntryReference = LocalizationTool.GetTableEntryReferencedByID(localizationTableType, ambiguousTableEntryReference);
                if (tableEntryReference.ReferenceType != TableEntryReference.Type.Id) { continue; }
                keyIDs.Add(tableEntryReference.KeyId);
            }
            return keyIDs;
        }
        
        private static bool IsPrefabLocalizable(MonoBehaviour targetMonoBehaviour, out ILocalizable prefabLocalizable)
        {
            prefabLocalizable = null;
            if (PrefabUtility.GetPrefabInstanceStatus(targetMonoBehaviour) != PrefabInstanceStatus.Connected) { return false; }
            Component prefabComponent = PrefabUtility.GetCorrespondingObjectFromSource(targetMonoBehaviour);
            return prefabComponent != null && prefabComponent.TryGetComponent(out prefabLocalizable);
        }
    }
}
#endif

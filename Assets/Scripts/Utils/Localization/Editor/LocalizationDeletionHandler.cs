#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization.Tables;

namespace Frankie.Utils.Localization.Editor
{
    [InitializeOnLoad]
    public class LocalizationDeletionHandler : AssetModificationProcessor
    {
        static LocalizationDeletionHandler()
        {
            ILocalizable.onBeforeDestroyedInEditor -= HandleDeletion;
            ILocalizable.onBeforeDestroyedInEditor += HandleDeletion;
        }
        
        #region UnityMethods
        private static AssetDeleteResult OnWillDeleteAsset(string assetPath, RemoveAssetOptions options)
        {
            var asset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
            if (asset is ScriptableObject scriptableObject and ILocalizable localizable)
            {
                HandleDeletion(localizable.localizationTableType, scriptableObject, localizable, false);
            }
            else if (asset is GameObject gameObject && gameObject != null && gameObject.TryGetComponent(out localizable))
            {
                HandleDeletion(localizable.localizationTableType, gameObject,  localizable, false);
            }
            
            // Pass back to Unity to continue deletion
            return AssetDeleteResult.DidNotDelete;
        }
        #endregion

        #region PrivateMethods
        private static void HandleDeletion(LocalizationTableType localizationTableType, Object targetObject, ILocalizable localizable)
        {
            HandleDeletion(localizationTableType, targetObject, localizable, true);
        }

        private static void HandleDeletion(LocalizationTableType localizationTableType, Object targetObject, ILocalizable localizable, bool isSceneInstance)
        {
            if (localizable.GetLocalizationEntries().Count == 0) { return; }
            
            Debug.Log($"{targetObject.name} is being deleted.  Deleting unique localization entries.");
            int deletionCount = 0;
            foreach (TableEntryReference tableEntryReference in FilterLocalizationEntries(localizationTableType, targetObject, localizable, isSceneInstance))
            {
                Debug.Log($"Removing entry:  {tableEntryReference.KeyId}");
                LocalizationTool.RemoveEntry(localizationTableType, tableEntryReference);
                deletionCount++;
            }
            Debug.Log($"{deletionCount} localization entries deleted.");
        }
        
        private static List<TableEntryReference> FilterLocalizationEntries(LocalizationTableType localizationTableType, Object targetObject, ILocalizable targetLocalizable, bool isSceneInstance = true)
        {
            var deletableEntries = new List<TableEntryReference>();
            if (targetLocalizable == null) { return deletableEntries; }

            List<TableEntryReference> tableEntryReferences = LocalizationTool.GetStandardTableEntryReferences(localizationTableType, targetLocalizable);
            
            if (targetObject is ScriptableObject || targetLocalizable is not MonoBehaviour targetMonoBehaviour) { return tableEntryReferences; }
            if (!IsPrefabLocalizable(targetMonoBehaviour, out ILocalizable prefabLocalizable, isSceneInstance)) { return tableEntryReferences; }

            // Note:  We cannot use standard prefab utility methods (e.g. GetPropertyModifications()) to check
            //        --> for reasons? these are not valid if this function is called from OnDestroy() or OnDisable()
            //        Approach is thus to manually compare keyID entries ~ don't expect conflicts given the use case
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
        
        private static bool IsPrefabLocalizable(MonoBehaviour targetMonoBehaviour, out ILocalizable prefabLocalizable, bool isSceneInstance)
        {
            prefabLocalizable = null;
            if (!isSceneInstance && !PrefabUtility.IsPartOfVariantPrefab(targetMonoBehaviour)) { return false; }
            if (isSceneInstance && !PrefabUtility.IsPartOfPrefabInstance(targetMonoBehaviour)) { return false; }
            
            Component prefabComponent = PrefabUtility.GetCorrespondingObjectFromSource(targetMonoBehaviour);
            return prefabComponent != null && prefabComponent.TryGetComponent(out prefabLocalizable);
        }
        #endregion
    }
}
#endif

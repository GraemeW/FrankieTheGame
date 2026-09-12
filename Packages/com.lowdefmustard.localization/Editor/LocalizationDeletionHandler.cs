using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization.Tables;
using Object = UnityEngine.Object;

namespace LowDefMustard.Localization.Editor
{
    [InitializeOnLoad]
    public class LocalizationDeletionHandler : AssetModificationProcessor
    {
        static LocalizationDeletionHandler()
        {
            ILocalizableCore.onBeforeDestroyedInEditor -= HandleDeletion;
            ILocalizableCore.onBeforeDestroyedInEditor += HandleDeletion;
        }

        #region UnityMethods
        private static AssetDeleteResult OnWillDeleteAsset(string assetPath, RemoveAssetOptions options)
        {
            var asset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
            if (asset is ScriptableObject scriptableObject and ILocalizableCore localizable)
            {
                HandleDeletion(localizable.localizationTableTypeValue, scriptableObject, localizable, false);
            }
            else if (asset is GameObject gameObject && gameObject != null && gameObject.TryGetComponent(out localizable))
            {
                HandleDeletion(localizable.localizationTableTypeValue, gameObject, localizable, false);
            }

            // Pass back to Unity to continue deletion
            return AssetDeleteResult.DidNotDelete;
        }
        #endregion

        #region PrivateMethods
        private static void HandleDeletion(Enum tableType, Object targetObject, ILocalizableCore localizable)
        {
            HandleDeletion(tableType, targetObject, localizable, true);
        }

        private static void HandleDeletion(Enum tableType, Object targetObject, ILocalizableCore localizable, bool isSceneInstance)
        {
            if (localizable.GetLocalizationEntries().Count == 0) { return; }

            if (tableType == null || !LocalizationToolBridgeRegistry.TryGetBridge(tableType.GetType(), out ILocalizationToolBridge localizationToolBridge))
            {
                Debug.LogWarning("TableType or LocalizationToolBridge could not be not found");
                return;
            }
            
            Debug.Log($"{targetObject.name} is being deleted.  Deleting unique localization entries.");
            int deletionCount = 0;
            foreach (TableEntryReference tableEntryReference in FilterLocalizationEntries(localizationToolBridge, tableType, targetObject, localizable, isSceneInstance))
            {
                Debug.Log($"Removing entry:  {tableEntryReference.KeyId}");
                localizationToolBridge.RemoveEntry(tableType, tableEntryReference);
                deletionCount++;
            }
            Debug.Log($"{deletionCount} localization entries deleted.");
        }

        private static List<TableEntryReference> FilterLocalizationEntries(ILocalizationToolBridge localizationToolBridge, Enum tableType, Object targetObject, ILocalizableCore targetLocalizable, bool isSceneInstance = true)
        {
            var deletableEntries = new List<TableEntryReference>();
            if (localizationToolBridge == null || targetLocalizable == null) { return deletableEntries; }

            List<TableEntryReference> tableEntryReferences = localizationToolBridge.GetStandardTableEntryReferences(tableType, targetLocalizable);

            if (targetObject is ScriptableObject || targetLocalizable is not MonoBehaviour targetMonoBehaviour) { return tableEntryReferences; }
            if (!IsPrefabLocalizable(targetMonoBehaviour, out ILocalizableCore prefabLocalizable, isSceneInstance)) { return tableEntryReferences; }

            // Note:  We cannot use standard prefab utility methods (e.g. GetPropertyModifications()) to check
            //        --> for reasons? these are not valid if this function is called from OnDestroy() or OnDisable()
            //        Approach is thus to manually compare keyID entries ~ don't expect conflicts given the use case
            HashSet<long> uniqueTargetKeyIDs = GetUniqueKeyIDs(localizationToolBridge, tableType, tableEntryReferences);
            HashSet<long> uniquePrefabKeyIDs = GetUniqueKeyIDs(localizationToolBridge, tableType, prefabLocalizable.GetLocalizationEntries());

            deletableEntries.AddRange(uniqueTargetKeyIDs.Where(keyID => !uniquePrefabKeyIDs.Contains(keyID)).Select(deletableTableEntryReference => (TableEntryReference)deletableTableEntryReference));

            return deletableEntries;
        }

        private static HashSet<long> GetUniqueKeyIDs(ILocalizationToolBridge bridge, Enum tableType, IList<TableEntryReference> tableEntryReferences)
        {
            var keyIDs = new HashSet<long>();
            foreach (TableEntryReference ambiguousTableEntryReference in tableEntryReferences)
            {
                TableEntryReference tableEntryReference = bridge.GetTableEntryReferencedByID(tableType, ambiguousTableEntryReference);
                if (tableEntryReference.ReferenceType != TableEntryReference.Type.Id) { continue; }
                keyIDs.Add(tableEntryReference.KeyId);
            }
            return keyIDs;
        }

        private static bool IsPrefabLocalizable(MonoBehaviour targetMonoBehaviour, out ILocalizableCore prefabLocalizable, bool isSceneInstance)
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

using System;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;

namespace LowDefMustard.Utils.Editor
{
    public sealed class DeletedAssetTypes : AssetModificationProcessor
    {
        // Note: Types are only held until the next editor update (allowing AssetPostProcessor to fire meanwhile)
        
        // Const Tunables
        private const string _assetExtension = ".asset";

        // State
        [NoAutoStaticsCleanup] private static readonly Dictionary<string, Type> _typesByDeletedPath = new();
        [NoAutoStaticsCleanup] private static bool _isClearScheduled;

        #region UnityMethods
        private static AssetDeleteResult OnWillDeleteAsset(string assetPath, RemoveAssetOptions options)
        {
            RecordBeforeDeletion(assetPath);
            return AssetDeleteResult.DidNotDelete;
        }
        #endregion
        
        #region PublicMethods
        public static List<Type> GetTypes(IEnumerable<string> deletedAssetPaths, out bool hasUnrecordedAssetDeletion)
        {
            var deletedAssetTypes = new List<Type>();
            hasUnrecordedAssetDeletion = false;
            foreach (string deletedAssetPath in deletedAssetPaths)
            {
                if (_typesByDeletedPath.TryGetValue(deletedAssetPath, out Type deletedAssetType)) { deletedAssetTypes.Add(deletedAssetType); }
                else if (deletedAssetPath.EndsWith(_assetExtension, StringComparison.OrdinalIgnoreCase)) { hasUnrecordedAssetDeletion = true; }
            }
            return deletedAssetTypes;
        }
        #endregion

        #region PrivateMethods
        private static void RecordBeforeDeletion(string assetPath)
        {
            ScheduleClear();
            if (!AssetDatabase.IsValidFolder(assetPath))
            {
                RecordPath(assetPath);
                return;
            }
            
            // Folder deletions report every contained path as deleted ~ need to record each one
            foreach (string assetGUID in AssetDatabase.FindAssets("", new[] { assetPath })) { RecordPath(AssetDatabase.GUIDToAssetPath(assetGUID)); }
        }

        private static void ScheduleClear()
        {
            if (_isClearScheduled) { return; }
            _isClearScheduled = true;
            EditorApplication.delayCall += ClearRecords;
        }

        private static void ClearRecords()
        {
            _typesByDeletedPath.Clear();
            _isClearScheduled = false;
        }

        private static void RecordPath(string assetPath)
        {
            Type mainAssetType = AssetDatabase.GetMainAssetTypeAtPath(assetPath);
            if (mainAssetType != null) { _typesByDeletedPath[assetPath] = mainAssetType; }
        }
        #endregion
    }
}

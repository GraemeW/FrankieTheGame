#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;

namespace LowDefMustard.Utils.Tests
{
    public sealed class TempAssetFolder : IDisposable
    {
        // Disposable Assets/_TEMP_<Name>_SafeToDelete folder for edit mode tests
        //  - deletes via file operations + Refresh by default:  AssetDatabase.DeleteAsset fires asset deletion handlers
        //      - e.g. LocalizationDeletionHandler would remove an ILocalizable asset's localization entries
        //  - opt into deleteViaAssetDatabase only when a test means to exercise those handlers

        // State
        private readonly bool deleteViaAssetDatabase;
        public string folderPath { get; }

        // Constructor
        public TempAssetFolder(string name, bool deleteViaAssetDatabase = false)
        {
            this.deleteViaAssetDatabase = deleteViaAssetDatabase;
            string folderName = $"_TEMP_{name}_SafeToDelete";
            folderPath = $"Assets/{folderName}";

            DeleteWithFileOperations();
            AssetDatabase.CreateFolder("Assets", folderName);
        }

        // Public Methods
        public string Path(string fileName) => $"{folderPath}/{fileName}";

        public string CreateSubfolder(string subfolderName)
        {
            AssetDatabase.CreateFolder(folderPath, subfolderName);
            return Path(subfolderName);
        }

        public void Dispose()
        {
            if (deleteViaAssetDatabase && AssetDatabase.IsValidFolder(folderPath)) { DeleteWithAssetDatabase(); }
            DeleteWithFileOperations();
        }

        private void DeleteWithAssetDatabase()
        {
            // Asset by asset:  a folder's DeleteAsset only reports the folder to deletion handlers
            IEnumerable<string> assetPaths = AssetDatabase.FindAssets(string.Empty, new[] { folderPath })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(assetPath => !AssetDatabase.IsValidFolder(assetPath))
                .Distinct()
                .ToList();
            foreach (string assetPath in assetPaths) { AssetDatabase.DeleteAsset(assetPath); }
            AssetDatabase.DeleteAsset(folderPath);
        }

        private void DeleteWithFileOperations()
        {
            string metaPath = folderPath + ".meta";
            if (!Directory.Exists(folderPath) && !File.Exists(metaPath)) { return; }

            if (Directory.Exists(folderPath)) { Directory.Delete(folderPath, true); }
            File.Delete(metaPath);
            AssetDatabase.Refresh();
        }
    }
}
#endif

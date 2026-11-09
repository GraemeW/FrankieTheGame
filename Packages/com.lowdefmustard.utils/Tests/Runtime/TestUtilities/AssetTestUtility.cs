#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using Object = UnityEngine.Object;

namespace LowDefMustard.Utils.Tests
{
    public static class AssetTestUtility
    {
        // Local file IDs of every object at a path (main + sub-assets), keyed by object name (or a given key)
        //  - keys are assumed unique within the asset
        
        public static Dictionary<string, long> GetSubAssetFileIDs(string assetPath) => GetSubAssetFileIDs(assetPath, asset => asset.name);

        public static Dictionary<string, long> GetSubAssetFileIDs(string assetPath, Func<Object, string> keyOf)
        {
            var fileIDs = new Dictionary<string, long>();
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(assetPath))
            {
                if (asset == null || !AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out string _, out long fileID)) { continue; }
                fileIDs[keyOf(asset)] = fileID;
            }
            return fileIDs;
        }
    }
}
#endif

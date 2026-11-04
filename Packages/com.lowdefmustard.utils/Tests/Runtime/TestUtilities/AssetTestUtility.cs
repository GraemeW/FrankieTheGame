#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using Object = UnityEngine.Object;

namespace LowDefMustard.Utils.Tests
{
    public static class AssetTestUtility
    {
        // Local file IDs of every object at a path (main + sub-assets), keyed by object name
        //  - e.g. compare before / after a reimport to check references stay bound
        //  - names are assumed unique within the asset
        public static Dictionary<string, long> GetSubAssetFileIDs(string assetPath)
        {
            var fileIDs = new Dictionary<string, long>();
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(assetPath))
            {
                if (asset == null || !AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out string _, out long fileID)) { continue; }
                fileIDs[asset.name] = fileID;
            }
            return fileIDs;
        }
    }
}
#endif

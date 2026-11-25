using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace LowDefMustard.Utils.Editor
{
    internal sealed class AddressablesCacheInvalidator : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) { return; }

            IEnumerable<Type> changedAssetTypes = importedAssets.Concat(movedAssets).Select(AssetDatabase.GetMainAssetTypeAtPath).Where(assetType => assetType != null);
            List<Type> deletedAssetTypes = DeletedAssetTypes.GetTypes(deletedAssets, out bool hasUnrecordedAssetDeletion);

            foreach (Type cacheOwnerType in GetCacheOwnersToReset(AddressablesCacheOwners.GetOwnerTypes(), changedAssetTypes.Concat(deletedAssetTypes), hasUnrecordedAssetDeletion))
            {
                AddressablesCacheOwners.GetCache(cacheOwnerType)?.Reset();
            }
        }

        // Note:  Internal for test visibility
        internal static List<Type> GetCacheOwnersToReset(IEnumerable<Type> cacheOwnerTypes, IEnumerable<Type> changedAssetTypes, bool resetAll)
        {
            List<Type> owners = cacheOwnerTypes.ToList();
            if (resetAll) { return owners; }

            List<Type> changedTypes = changedAssetTypes.Distinct().ToList();
            return owners.Where(owner => changedTypes.Any(owner.IsAssignableFrom)).ToList();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

            foreach (Type cacheOwnerType in GetCacheOwnersToReset(GetCacheOwnerTypes(), changedAssetTypes.Concat(deletedAssetTypes), hasUnrecordedAssetDeletion))
            {
                GetResetMethod(cacheOwnerType)?.Invoke(null, null);
            }
        }

        // Note:  Internal methods below for test visibility
        internal static IEnumerable<Type> GetCacheOwnerTypes() => TypeCache.GetTypesDerivedFrom<IAddressablesCache>().Where(IsCacheOwner);
        internal static MethodInfo GetResetMethod(Type cacheOwnerType) => cacheOwnerType.GetMethod(nameof(IAddressablesCache.ResetCache), BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly, null, Type.EmptyTypes, null);
        
        private static bool IsCacheOwner(Type type)
        {
            // The class that introduces IAddressablesCache owns the cache ~ subclasses (e.g. KeyItem : InventoryItem) share it
            bool isClass = !type.IsInterface;
            bool doesBaseImplementCache = typeof(IAddressablesCache).IsAssignableFrom(type.BaseType); // e.g. KeyItem:true, InventoryItem base implements;  InventoryItem:false, GameStateModifier base does not implement
            return isClass && !doesBaseImplementCache;
        }

        internal static List<Type> GetCacheOwnersToReset(IEnumerable<Type> cacheOwnerTypes, IEnumerable<Type> changedAssetTypes, bool resetAll)
        {
            List<Type> owners = cacheOwnerTypes.ToList();
            if (resetAll) { return owners; }

            List<Type> changedTypes = changedAssetTypes.Distinct().ToList();
            return owners.Where(owner => changedTypes.Any(owner.IsAssignableFrom)).ToList();
        }
    }
}

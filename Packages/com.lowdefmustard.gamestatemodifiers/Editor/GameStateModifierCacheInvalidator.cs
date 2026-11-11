using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using LowDefMustard.Utils.Editor;

namespace LowDefMustard.GameStateModifiers.Editor
{
    internal sealed class GameStateModifierCacheInvalidator : AssetPostprocessor
    {
        // Methods
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            IEnumerable<Type> changedAssetTypes = importedAssets.Concat(movedAssets).Select(AssetDatabase.GetMainAssetTypeAtPath);
            List<Type> deletedAssetTypes = DeletedAssetTypes.GetTypes(deletedAssets, out bool hasUnrecordedAssetDeletion);
            if (ShouldResetCache(changedAssetTypes.Concat(deletedAssetTypes), hasUnrecordedAssetDeletion)) { GameStateModifier.ResetCache(); }
        }

        // Note:  Internal for test visibility
        internal static bool ShouldResetCache(IEnumerable<Type> changedOrDeletedAssetTypes, bool hasUnrecordedAssetDeletion)
        {
            return hasUnrecordedAssetDeletion || changedOrDeletedAssetTypes.Any(assetType => assetType != null && typeof(GameStateModifier).IsAssignableFrom(assetType));
        }
    }
}

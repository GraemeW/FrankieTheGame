using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;

namespace LowDefMustard.Zones.Editor
{
    public static class AddressableSceneEntries
    {
        #region PublicMethods
        public static bool IsLoadableScene(string sceneGuid)
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null || string.IsNullOrWhiteSpace(sceneGuid)) { return false; }

            AddressableAssetEntry entry = settings.FindAssetEntry(sceneGuid);
            return entry is { IsScene: true } && IsGuidInCatalog(entry.parentGroup);
        }
        #endregion

        #region PrivateMethods
        private static bool IsGuidInCatalog(AddressableAssetGroup group)
        {
            BundledAssetGroupSchema schema = group != null ? group.GetSchema<BundledAssetGroupSchema>() : null;
            return schema != null && schema.IncludeGUIDInCatalog;
        }
        #endregion
    }
}

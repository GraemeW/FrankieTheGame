#if UNITY_EDITOR
using System.Linq;
using NUnit.Framework;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build.DataBuilders;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine.AddressableAssets;

namespace LowDefMustard.Utils.Tests
{
    public static class AddressablesTestCatalogue
    {
        public static void IgnoreUnlessAssetDatabaseCatalog()
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            string[] locatorIds = Addressables.ResourceLocators.Select(locator => locator.LocatorId).ToArray();
            if (IsAssetDatabaseCatalogue(settings, locatorIds)) { return; }

            string playModeScript = settings != null ? settings.ActivePlayModeDataBuilder?.Name : "no Addressables settings";
            string catalog = locatorIds.Length > 0 ? string.Join(", ", locatorIds) : "not loaded";
            Assert.Ignore($"Addressables isn't on the Asset Database catalog (Play Mode Script: {playModeScript} | loaded catalog: {catalog}), so it loads bundle copies rather than the project assets this test compares. Switch to Use Asset Database, then enter + exit Play mode once.");
        }

        private static bool IsAssetDatabaseCatalogue(AddressableAssetSettings settings, string[] locatorIds)
        {
            // Note:  Asset Database catalogue's locator ID == the settings asset's name ~ package internal
            
            if (settings == null) { return false; }
            if (locatorIds.Length > 0) { return locatorIds.Contains(settings.name); }
            return settings.ActivePlayModeDataBuilder is BuildScriptFastMode;
        }
    }
}
#endif

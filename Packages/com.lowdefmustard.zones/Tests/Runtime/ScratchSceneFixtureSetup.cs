#if UNITY_EDITOR
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.SceneManagement;
using LowDefMustard.Utils.Tests;

namespace LowDefMustard.Zones.Tests
{
    // Test Notes:
    //  - Using a separate fixture w/ IPrebuildSetup/Cleanup instead of traditional [OneTimeSetup/Teardown]
    //      - this is required because EditorSceneManager methods can ONLY be called in Editor mode
    //  - Scenes load through Addressables by scene GUID:  the scratch scene gets its own entry in a scratch group (removed on cleanup)
    //      - left on its default (path) address ~ i.e. loading by GUID doesn't depend on the address
    
    public class ScratchSceneFixtureSetup : IPrebuildSetup, IPostBuildCleanup
    {
        // Const Tunables
        public const string scratchScenePath = "Assets/ScratchTest_SafeToDelete.unity";
        public const string scratchSceneName = "ScratchTest_SafeToDelete";
        private const string _scratchGroupName = "ScratchTest_SafeToDelete";
        
        public void Setup()
        {
            Scene scratchScene = SceneTestUtility.CreateSavedScene(scratchScenePath, NewSceneMode.Additive);
            EditorSceneManager.CloseScene(scratchScene, true);

            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            AddressableAssetGroup group = settings.FindGroup(_scratchGroupName);
            if (group == null) { group = settings.CreateGroup(_scratchGroupName, false, false, false, null, typeof(BundledAssetGroupSchema)); }
            settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(scratchScenePath), group, false, false);
        }

        public void Cleanup()
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            AddressableAssetGroup group = settings.FindGroup(_scratchGroupName);
            if (group != null) { settings.RemoveGroup(group); }

            AssetDatabase.DeleteAsset(scratchScenePath);
            AssetDatabase.Refresh();
        }
    }
}
#endif

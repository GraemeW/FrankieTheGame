using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Build.Profile;
using UnityEditor.Localization;
using LowDefMustard.Zones;
using LowDefMustard.Zones.Editor;

namespace Frankie.Tests.Editor
{
    public class AddressableScenesValidationTests
    {
        // Tested:
        //  - Every Zone's scene + every project scene loads by GUID (how AddressableSceneLoader loads it):  a scene entry in a group w/ Include GUIDs in Catalogue
        //  - Every Zone's stored scene GUID matches its scene asset (builds only have the stored GUID)
        //  - Every Build Profile (+ the global list) builds only the bootstrap scene, which holds a BootstrapSceneLoader with a first scene set
        //  - Every string table is preloaded (an unloaded table read synchronously in a scene's Awake deadlocks the same way)
        // Note:  Validates project data ~ a failure means a scene / profile needs fixing, not the code
        // Note:  A scene left in a build profile is built in, so it + everything it references duplicates the Addressables copies

        // Const (note that paths/names are necessarily string ref and brittle, must be updated w/ project layout/re-configuration)
        private const string _scenesFolder = "Assets/Scenes";
        private const string _templatesFolder = "Assets/Scenes/_Templates/";
        private const string _bootstrapScenePath = "Assets/Scenes/Bootstrap.unity";

        #region Helpers
        private static IEnumerable<string> GetProjectScenePaths()
        {
            return AssetDatabase.FindAssets("t:SceneAsset", new[] { _scenesFolder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => path != _bootstrapScenePath && !path.StartsWith(_templatesFolder));
        }

        private static void AssertOnlyBootstrap(string listName, EditorBuildSettingsScene[] scenes)
        {
            string[] enabledScenes = scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
            CollectionAssert.AreEqual(new[] { _bootstrapScenePath }, enabledScenes, $"{listName} should build only the bootstrap scene");
        }
        #endregion

        #region Tests
        [Test]
        public void Zones_SceneLoadsByStoredGuid()
        {
            var failures = new List<string>();
            foreach (string guid in AssetDatabase.FindAssets("t:Zone"))
            {
                var zone = AssetDatabase.LoadAssetAtPath<Zone>(AssetDatabase.GUIDToAssetPath(guid));
                if (zone == null || !zone.GetSceneReference().IsSet()) { continue; }

                SceneReference sceneReference = zone.GetSceneReference();
                sceneReference.TryGetSceneGuid(out string sceneGuid);
                if (sceneGuid != AssetDatabase.AssetPathToGUID(sceneReference.GetScenePath())) { failures.Add($"{zone.name} -> {sceneReference.SceneName} (stored GUID '{sceneGuid}' doesn't match the scene; re-link it)"); }
                else if (!AddressableSceneEntries.IsLoadableScene(sceneGuid)) { failures.Add($"{zone.name} -> {sceneReference.SceneName}"); }
            }
            Assert.IsEmpty(failures, $"Zone scenes that can't load at runtime:\n{string.Join("\n", failures)}");
        }

        [Test]
        public void ProjectScenes_LoadByGuid()
        {
            List<string> failures = GetProjectScenePaths().Where(path => !AddressableSceneEntries.IsLoadableScene(AssetDatabase.AssetPathToGUID(path))).ToList();
            Assert.IsEmpty(failures, $"Scenes that can't load by GUID (an Addressables scene entry in a group with Include GUIDs in Catalog):\n{string.Join("\n", failures)}");
        }

        [Test]
        public void BuildProfiles_BuildOnlyBootstrapScene()
        {
            AssertOnlyBootstrap("Global scene list", EditorBuildSettings.globalScenes);
            foreach (string guid in AssetDatabase.FindAssets("t:BuildProfile"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var profile = AssetDatabase.LoadAssetAtPath<BuildProfile>(path);
                if (profile == null || !profile.overrideGlobalScenes) { continue; }
                AssertOnlyBootstrap($"Build Profile '{path}'", profile.scenes);
            }
        }

        [Test]
        public void BootstrapScene_HasLoaderWithFirstSceneSet()
        {
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<SceneAsset>(_bootstrapScenePath), $"Missing bootstrap scene at '{_bootstrapScenePath}'");

            var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(_bootstrapScenePath, UnityEditor.SceneManagement.OpenSceneMode.Additive);
            try
            {
                BootstrapSceneLoader[] loaders = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<BootstrapSceneLoader>(true)).ToArray();
                Assert.AreEqual(1, loaders.Length, "The bootstrap scene should hold exactly one BootstrapSceneLoader");
                SceneReference firstScene = loaders[0].GetFirstScene();
                Assert.IsTrue(firstScene.IsSet(), "The bootstrap loader has no first scene set");
                Assert.IsTrue(firstScene.TryGetSceneGuid(out string firstSceneGuid), "The bootstrap loader's first scene has no stored GUID (re-link it)");
                Assert.AreEqual(AssetDatabase.AssetPathToGUID(firstScene.GetScenePath()), firstSceneGuid, "The bootstrap loader's stored first-scene GUID doesn't match its scene (re-link it)");
                Assert.IsTrue(AddressableSceneEntries.IsLoadableScene(firstSceneGuid), "The bootstrap loader's first scene can't load at runtime");
            }
            finally
            {
                UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void StringTables_ArePreloaded()
        {
            List<string> misses = LocalizationEditorSettings.GetStringTableCollections()
                .SelectMany(collection => collection.StringTables)
                .Where(table => !LocalizationEditorSettings.GetPreloadTableFlag(table))
                .Select(table => table.name)
                .ToList();
            Assert.IsEmpty(misses, $"String tables not flagged Preload (a synchronous read in a scene's Awake would deadlock the scene load):\n{string.Join("\n", misses)}");
        }
        #endregion
    }
}

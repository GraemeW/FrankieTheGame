#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace LowDefMustard.Zones.Tests
{
    // Test Notes:
    //  - This test MUST live in runtime-compiled environment because SceneManager.LoadSceneAsync() is a runtime-only method
    //      - since testing requires editor methods (as below), the entire test is wrapped in editor pragma
    //  - Using a separate fixture w/ IPrebuildSetup/Cleanup instead of traditional [OneTimeSetup/Teardown]
    //      - this is required because EditorSceneManager methods can ONLY be called in Editor mode
    
    public class SceneLoadFixtureSetup : IPrebuildSetup, IPostBuildCleanup
    {
        // Const Tunables
        public const string scratchScenePath = "Assets/ScratchTest_SceneLoaderLoad_SafeToDelete.unity";
        public const string scratchSceneName = "ScratchTest_SceneLoaderLoad_SafeToDelete";
        
        public void Setup()
        {
            Scene scratchScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            EditorSceneManager.SaveScene(scratchScene, scratchScenePath);
            EditorSceneManager.CloseScene(scratchScene, true);
            
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (!scenes.Exists(s => s.path == scratchScenePath))
            {
                scenes.Add(new EditorBuildSettingsScene(scratchScenePath, true));
                EditorBuildSettings.scenes = scenes.ToArray();
            }
        }

        public void Cleanup()
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            int removedCount = scenes.RemoveAll(s => s.path == scratchScenePath);
            if (removedCount > 0) { EditorBuildSettings.scenes = scenes.ToArray(); } 
            
            AssetDatabase.DeleteAsset(scratchScenePath);
            AssetDatabase.Refresh();
        }
    }
    
    [PrebuildSetup(typeof(SceneLoadFixtureSetup))]
    [PostBuildCleanup(typeof(SceneLoadFixtureSetup))] 
    public class SceneLoaderBaseLoadNewSceneAsyncTests
    {
        // State
        private EditorBuildSettingsScene[] originalBuildScenes;
        private Scene scratchScene;
        private Scene cleanupScene;
        private Zone zone;
        private bool originalIsCurrentlyLoading;
        private Zone originalCurrentZone;
        private Zone originalLastZone;

        #region Setup
        [SetUp]
        public void SetUp()
        {
            zone = ScriptableObject.CreateInstance<Zone>();
            zone.sceneReference = SceneLoadFixtureSetup.scratchSceneName;
            originalIsCurrentlyLoading = SceneLoaderBase.isCurrentlyLoading;
            originalCurrentZone = SceneLoaderBase.currentZone;
            originalLastZone = SceneLoaderBase.lastZone;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            SceneLoaderBase.isCurrentlyLoading = originalIsCurrentlyLoading;
            SceneLoaderBase.currentZone = originalCurrentZone;
            SceneLoaderBase.lastZone = originalLastZone;
            Object.DestroyImmediate(zone);

            // Leave a clean runtime scene behind for whatever test runs next in this Play Mode session
            if (!cleanupScene.IsValid()) { cleanupScene = SceneManager.CreateScene("PostSceneLoadTestCleanup"); }
            
            Scene checkScratchScene = SceneManager.GetSceneByName(SceneLoadFixtureSetup.scratchSceneName);
            if (checkScratchScene.IsValid()) { yield return SceneManager.UnloadSceneAsync(SceneLoadFixtureSetup.scratchSceneName); }
            else { yield return null; }
            
            SceneManager.SetActiveScene(cleanupScene);
        }
        #endregion

        #region Tests
        [UnityTest]
        public IEnumerator LoadNewSceneAsync_LoadsScratchScene_SetsCurrentZoneAndClearsLoadingFlag()
        {
            yield return SceneLoaderBase.LoadNewSceneAsync(zone);
            
            Assert.AreEqual(SceneLoadFixtureSetup.scratchSceneName, SceneManager.GetActiveScene().name);
            Assert.AreSame(zone, SceneLoaderBase.GetCurrentZone());
            Assert.IsFalse(SceneLoaderBase.isCurrentlyLoading);
        }

        [UnityTest]
        public IEnumerator LoadNewSceneAsync_WhileIsCurrentlyLoading_IsNoOp()
        {
            SceneLoaderBase.isCurrentlyLoading = true;
            string activeSceneBefore = SceneManager.GetActiveScene().name;

            yield return SceneLoaderBase.LoadNewSceneAsync(zone);

            Assert.AreEqual(activeSceneBefore, SceneManager.GetActiveScene().name);
        }
        #endregion
    }
}
#endif

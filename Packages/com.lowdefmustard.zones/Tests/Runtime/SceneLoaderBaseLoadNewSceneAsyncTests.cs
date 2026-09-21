#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace LowDefMustard.Zones.Tests
{
    // Test Notes:
    //  - This test MUST live in runtime-compiled environment because SceneManager.LoadSceneAsync() is a runtime-only method
    //      - since testing requires editor methods (as below), the entire test is wrapped in editor pragma
    
    [PrebuildSetup(typeof(ScratchSceneFixtureSetup))]
    [PostBuildCleanup(typeof(ScratchSceneFixtureSetup))] 
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
            zone.preventLocalizationForTests = true;
            zone.sceneReference = ScratchSceneFixtureSetup.scratchSceneName;
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
            
            Scene checkScratchScene = SceneManager.GetSceneByName(ScratchSceneFixtureSetup.scratchSceneName);
            if (checkScratchScene.IsValid()) { yield return SceneManager.UnloadSceneAsync(ScratchSceneFixtureSetup.scratchSceneName); }
            else { yield return null; }
            
            SceneManager.SetActiveScene(cleanupScene);
        }
        #endregion

        #region Tests
        [UnityTest]
        public IEnumerator LoadNewSceneAsync_LoadsScratchScene_SetsCurrentZoneAndClearsLoadingFlag()
        {
            yield return SceneLoaderBase.LoadNewSceneAsync(zone);
            
            Assert.AreEqual(ScratchSceneFixtureSetup.scratchSceneName, SceneManager.GetActiveScene().name);
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

#if UNITY_EDITOR
using System.Collections;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using LowDefMustard.Utils.Tests;

namespace LowDefMustard.Zones.Tests
{
    // Test Notes:
    //  - This test MUST live in runtime-compiled environment because scene loads (Addressables.LoadSceneAsync) are runtime-only
    //  - Since testing requires editor methods (as below), the entire test is wrapped in editor pragma
    
    [PrebuildSetup(typeof(ScratchSceneFixtureSetup))]
    [PostBuildCleanup(typeof(ScratchSceneFixtureSetup))] 
    public class SceneLoaderBaseLoadNewSceneAsyncTests
    {
        // Const
        private const string _missingSceneName = "SceneLoaderBaseTests_NotAnAddressablesScene";
        private const string _missingSceneGuid = "00000000000000000000000000000001"; // i.e. no asset

        // State
        private readonly TestObjectTracker tracker = new();
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
            zone = tracker.Create<Zone>();
            zone.preventLocalizationForTests = true;
            zone.hideFlags = HideFlags.HideAndDontSave;
            
            zone.sceneReference = new SceneReference(ScratchSceneFixtureSetup.scratchSceneName) { sceneGuid = AssetDatabase.AssetPathToGUID(ScratchSceneFixtureSetup.scratchScenePath) };
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
            tracker.DestroyAll();

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

        [UnityTest]
        public IEnumerator LoadNewSceneAsync_SceneNotAnAddressablesEntry_WarnsAndKeepsActiveSceneAndZone()
        {
            zone.sceneReference = new SceneReference(_missingSceneName) { sceneGuid = _missingSceneGuid };
            string activeSceneBefore = SceneManager.GetActiveScene().name;
            Zone zoneBefore = SceneLoaderBase.currentZone;
            LogAssert.ignoreFailingMessages = true; // Addressables logs its own invalid key exception alongside ours
            LogAssert.Expect(LogType.Warning, new Regex($"Failed to load scene '{_missingSceneGuid}'"));
            LogAssert.Expect(LogType.Warning, new Regex("didn't load - staying in the current scene"));

            yield return SceneLoaderBase.LoadNewSceneAsync(zone);

            Assert.AreEqual(activeSceneBefore, SceneManager.GetActiveScene().name);
            Assert.AreSame(zoneBefore, SceneLoaderBase.currentZone, "A failed load shouldn't switch the current zone");
            Assert.IsFalse(SceneLoaderBase.isCurrentlyLoading);
        }
        #endregion
    }
}
#endif

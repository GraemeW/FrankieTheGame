using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System;
using System.Text.RegularExpressions;
using LowDefMustard.Utils.Tests;

namespace LowDefMustard.Zones.Tests.Editor
{
    public class SceneLoaderBaseGenericTests
    {
        // State
        private readonly TestObjectTracker tracker = new();
        private GameObject loaderGameObject;
        private TestSceneLoader loader;
        private Zone zone;
        private bool originalIsCurrentlyLoading;
        private SceneLoaderBase<TestSceneType> originalActiveSceneLoader;
        private Func<Zone> originalDemoZoneOverrideProvider;

        // Data Structures
        private enum TestSceneType { None, TestScene }

        private class TestSceneLoader : SceneLoaderBase<TestSceneType> { }

        private class GameOverTestSceneLoader : SceneLoaderBase<TestSceneType>
        {
            protected override bool IsGameOverSceneType(TestSceneType sceneType) => true;
            protected override bool ShouldSaveSessionOnGameOver() => false;
        }

        private class NewGameTestSceneLoader : SceneLoaderBase<TestSceneType>
        {
            protected override bool IsNewGameSceneType(TestSceneType sceneType) => true;
        }

        #region Setup
        [SetUp]
        public void SetUp()
        {
            originalIsCurrentlyLoading = SceneLoaderBase.isCurrentlyLoading;
            SceneLoaderBase.isCurrentlyLoading = false;
            originalActiveSceneLoader = SceneLoaderBase<TestSceneType>.activeSceneLoader;
            originalDemoZoneOverrideProvider = SceneLoaderBase.demoZoneOverrideProvider;
            SceneLoaderBase.demoZoneOverrideProvider = null;

            loaderGameObject = tracker.CreateGameObject("TestSceneLoader");
            loader = loaderGameObject.AddComponent<TestSceneLoader>();

            var lookup = new ZoneSceneTypeLookup<TestSceneType>();
            zone = tracker.Create<Zone>();
            zone.preventLocalizationForTests = true;
            lookup.Set(TestSceneType.TestScene, zone);
            loader.zoneSceneTypeLookup = lookup;

            SceneLoaderBase<TestSceneType>.activeSceneLoader = loader;
        }

        [TearDown]
        public void TearDown()
        {
            SceneLoaderBase.isCurrentlyLoading = originalIsCurrentlyLoading;
            SceneLoaderBase<TestSceneType>.activeSceneLoader = originalActiveSceneLoader;
            SceneLoaderBase.demoZoneOverrideProvider = originalDemoZoneOverrideProvider;
            tracker.DestroyAll();
        }
        #endregion

        #region Tests
        [Test]
        public void QueueScene_MatchingSceneType_InvokesProviderWithReconciledZone()
        {
            Zone receivedZone = null;
            bool? receivedSaveSession = null;
            loader.sceneLoadFadeProvider = (invokedZone, saveSession) =>
            {
                receivedZone = invokedZone;
                receivedSaveSession = saveSession;
            };

            SceneLoaderBase<TestSceneType>.QueueScene(TestSceneType.TestScene, new SceneQueueData(true));

            Assert.AreSame(zone, receivedZone);
            bool checkValue = receivedSaveSession ?? false; // Opposite to test expect on null
            Assert.IsTrue(checkValue);
        }

        [Test]
        public void QueueScene_SceneTypeWithNoAssignedZone_ReconciledZoneIsNull_LogsErrorAndDoesNotInvokeProvider()
        {
            // TestSceneType.None was never passed to lookup.Set, so it reconciles to a null Zone
            bool invoked = false;
            loader.sceneLoadFadeProvider = (_, _) => invoked = true;

            LogAssert.Expect(LogType.Error, new Regex(@"\[Zones\]\S* No zone mapped to scene type 'None'\."));
            SceneLoaderBase<TestSceneType>.QueueScene(TestSceneType.None, new SceneQueueData(true));

            Assert.IsFalse(invoked);
        }

        [Test]
        public void QueueScene_WhileIsCurrentlyLoading_IsNoOp()
        {
            SceneLoaderBase.isCurrentlyLoading = true;
            bool invoked = false;
            loader.sceneLoadFadeProvider = (_, _) => invoked = true;

            SceneLoaderBase<TestSceneType>.QueueScene(TestSceneType.TestScene, new SceneQueueData(true));

            Assert.IsFalse(invoked);
        }

        [Test]
        public void QueueScene_GameOverSceneTypeThatShouldNotSaveSession_PassesFalseToProvider()
        {
            var gameOverLoaderGameObject = tracker.CreateGameObject("GameOverTestSceneLoader");
            var gameOverLoader = gameOverLoaderGameObject.AddComponent<GameOverTestSceneLoader>();

            var lookup = new ZoneSceneTypeLookup<TestSceneType>();
            lookup.Set(TestSceneType.TestScene, zone);
            gameOverLoader.zoneSceneTypeLookup = lookup;
            SceneLoaderBase<TestSceneType>.activeSceneLoader = gameOverLoader;

            bool? receivedSaveSession = null;
            gameOverLoader.sceneLoadFadeProvider = (_, saveSession) => receivedSaveSession = saveSession;

            SceneLoaderBase<TestSceneType>.QueueScene(TestSceneType.TestScene, new SceneQueueData(true));

            bool checkValue = receivedSaveSession ?? true; // Opposite to test expect on null
            Assert.IsFalse(checkValue);
        }

        [Test]
        public void QueueScene_NewGameSceneTypeWithProvider_UsesProviderZone_SkipsLookup()
        {
            var providerZone = tracker.Create<Zone>();
            providerZone.preventLocalizationForTests = true;
            var newGameLoaderGameObject = tracker.CreateGameObject("NewGameTestSceneLoader");
            var newGameLoader = newGameLoaderGameObject.AddComponent<NewGameTestSceneLoader>();
            newGameLoader.zoneSceneTypeLookup = loader.zoneSceneTypeLookup; // has TestScene -> zone (the OTHER zone)
            SceneLoaderBase<TestSceneType>.activeSceneLoader = newGameLoader;
            SceneLoaderBase.demoZoneOverrideProvider = () => providerZone;

            Zone receivedZone = null;
            newGameLoader.sceneLoadFadeProvider = (invokedZone, _) => receivedZone = invokedZone;

            SceneLoaderBase<TestSceneType>.QueueScene(TestSceneType.TestScene, new SceneQueueData(true));

            Assert.AreSame(providerZone, receivedZone);
            Assert.AreNotSame(zone, receivedZone);
        }

        [Test]
        public void QueueScene_NewGameSceneTypeWithProviderReturningNull_FallsBackToLookupZone()
        {
            var newGameLoaderGameObject = tracker.CreateGameObject("NewGameTestSceneLoader");
            var newGameLoader = newGameLoaderGameObject.AddComponent<NewGameTestSceneLoader>();
            newGameLoader.zoneSceneTypeLookup = loader.zoneSceneTypeLookup;
            SceneLoaderBase<TestSceneType>.activeSceneLoader = newGameLoader;
            SceneLoaderBase.demoZoneOverrideProvider = () => null;

            Zone receivedZone = null;
            newGameLoader.sceneLoadFadeProvider = (invokedZone, _) => receivedZone = invokedZone;

            SceneLoaderBase<TestSceneType>.QueueScene(TestSceneType.TestScene, new SceneQueueData(true));

            Assert.AreSame(zone, receivedZone);
        }

        [Test]
        public void QueueScene_NotNewGameSceneType_IgnoresProviderEvenIfSet()
        {
            var providerZone = tracker.Create<Zone>();
            providerZone.preventLocalizationForTests = true;
            SceneLoaderBase.demoZoneOverrideProvider = () => providerZone;

            Zone receivedZone = null;
            loader.sceneLoadFadeProvider = (invokedZone, _) => receivedZone = invokedZone;

            SceneLoaderBase<TestSceneType>.QueueScene(TestSceneType.TestScene, new SceneQueueData(true));

            Assert.AreSame(zone, receivedZone);
            Assert.AreNotSame(providerZone, receivedZone);
        }
        #endregion
    }
}

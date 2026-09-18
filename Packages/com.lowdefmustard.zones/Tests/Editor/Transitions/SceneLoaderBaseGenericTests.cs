using NUnit.Framework;
using UnityEngine;

namespace LowDefMustard.Zones.Tests.Editor
{
    public class SceneLoaderBaseGenericTests
    {
        // State
        private GameObject loaderGameObject;
        private TestSceneLoader loader;
        private Zone zone;
        private bool originalIsCurrentlyLoading;
        private SceneLoaderBase<TestSceneType> originalActiveSceneLoader;

        // Data Structures
        private enum TestSceneType { None, TestScene }

        private class TestSceneLoader : SceneLoaderBase<TestSceneType> { }

        private class GameOverTestSceneLoader : SceneLoaderBase<TestSceneType>
        {
            protected override bool IsGameOverSceneType(TestSceneType sceneType) => true;
            protected override bool ShouldSaveSessionOnGameOver() => false;
        }

        #region Setup
        [SetUp]
        public void SetUp()
        {
            originalIsCurrentlyLoading = SceneLoaderBase.isCurrentlyLoading;
            SceneLoaderBase.isCurrentlyLoading = false;
            originalActiveSceneLoader = SceneLoaderBase<TestSceneType>.activeSceneLoader;

            loaderGameObject = new GameObject("TestSceneLoader");
            loader = loaderGameObject.AddComponent<TestSceneLoader>();

            var lookup = new ZoneSceneTypeLookup<TestSceneType>();
            zone = ScriptableObject.CreateInstance<Zone>();
            lookup.Set(TestSceneType.TestScene, zone);
            loader.zoneSceneTypeLookup = lookup;

            SceneLoaderBase<TestSceneType>.activeSceneLoader = loader;
        }

        [TearDown]
        public void TearDown()
        {
            SceneLoaderBase.isCurrentlyLoading = originalIsCurrentlyLoading;
            SceneLoaderBase<TestSceneType>.activeSceneLoader = originalActiveSceneLoader;
            if (loaderGameObject != null) { Object.DestroyImmediate(loaderGameObject); }
            if (zone != null) { Object.DestroyImmediate(zone); }
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
        public void QueueScene_SceneTypeWithNoAssignedZone_ReconciledZoneIsNull_DoesNotInvokeProvider()
        {
            // TestSceneType.None was never passed to lookup.Set, so it reconciles to a null Zone
            bool invoked = false;
            loader.sceneLoadFadeProvider = (_, _) => invoked = true;

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
            var gameOverLoaderGameObject = new GameObject("GameOverTestSceneLoader");
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

            Object.DestroyImmediate(gameOverLoaderGameObject);
        }
        #endregion
    }
}

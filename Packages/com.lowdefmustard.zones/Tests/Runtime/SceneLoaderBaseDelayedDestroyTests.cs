using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace LowDefMustard.Zones.Tests
{
    public class SceneLoaderBaseDelayedDestroyTests
    {
        // State
        private GameObject loaderGameObject;
        private SceneLoaderBase originalActiveSceneLoaderBase;
        
        // Data Structures
        private class TestSceneLoaderBase : SceneLoaderBase { }

        #region Setup
        [SetUp]
        public void SetUp()
        {
            originalActiveSceneLoaderBase = SceneLoaderBase.activeSceneLoaderBase;
            loaderGameObject = new GameObject("TestSceneLoaderBase");
            var loader = loaderGameObject.AddComponent<TestSceneLoaderBase>();
            SceneLoaderBase.activeSceneLoaderBase = loader;
        }

        [TearDown]
        public void TearDown()
        {
            SceneLoaderBase.activeSceneLoaderBase = originalActiveSceneLoaderBase;
            if (loaderGameObject != null) { Object.Destroy(loaderGameObject); }
        }
        #endregion

        #region Tests
        [UnityTest]
        public IEnumerator QueueDelayedDestroy_DestroysEntriesAfterOneFrame()
        {
            var target = new GameObject("DestroyTarget");

            SceneLoaderBase.QueueDelayedDestroy(new List<GameObject> { target });

            Assert.IsTrue(target != null);
            yield return null;
            yield return null;

            Assert.IsTrue(target == null);
        }
        #endregion
    }
}

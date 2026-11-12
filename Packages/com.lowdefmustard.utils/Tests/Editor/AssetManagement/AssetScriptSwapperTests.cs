using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using LowDefMustard.Utils.Editor;
using Object = UnityEngine.Object;

namespace LowDefMustard.Utils.Tests.Editor
{
    public class AssetScriptSwapperTests
    {
        // Not Tested:  ConfirmInPlace (not interesting ~ modal dialogue only)

        // State
        private TempAssetFolder tempFolder;

        #region Setup
        [SetUp]
        public void SetUp() => tempFolder = new TempAssetFolder(nameof(AssetScriptSwapperTests));

        [TearDown]
        public void TearDown() => tempFolder.Dispose();
        #endregion

        #region Tests
        [Test]
        public void SwapScriptInPlace_ScriptableObjectAsset_PreservesGuidAndFields()
        {
            var source = ScriptableObject.CreateInstance<SwapSourceAsset>();
            source.sharedValue = 7;
            string path = tempFolder.Path("Source.asset");
            AssetDatabase.CreateAsset(source, path);
            string guidBefore = AssetDatabase.AssetPathToGUID(path);

            AssetScriptSwapper.SwapScriptInPlace<SwapTargetAsset>(source);
            
            Assert.AreEqual(guidBefore, AssetDatabase.AssetPathToGUID(path));
            var reloaded = AssetDatabase.LoadAssetAtPath<SwapTargetAsset>(path); // The in-memory `source` reference is stale after the reimport - reload from the same path directly
            Assert.IsNotNull(reloaded, "expected the same-path asset to now load as SwapTargetAsset");
            Assert.AreEqual(7, reloaded.sharedValue);
            Assert.AreEqual(0.5f, reloaded.addedValue, "a field missing from the YAML should take its default");
        }

        [Test]
        public void SwapScriptInPlace_PrefabComponent_ReloadsAsTargetWithFields()
        {
            var gameObject = new GameObject("Swap");
            gameObject.AddComponent<SwapSourceBehaviour>().sharedValue = 3;
            string path = tempFolder.Path("Swap.prefab");
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(gameObject, path);
            Object.DestroyImmediate(gameObject);

            AssetScriptSwapper.SwapScriptInPlace<SwapTargetBehaviour>(prefab.GetComponent<SwapSourceBehaviour>());

            var reloaded = AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<SwapTargetBehaviour>();
            Assert.IsNotNull(reloaded, "expected the prefab's component to now load as SwapTargetBehaviour");
            Assert.AreEqual(3, reloaded.sharedValue);
        }

        [Test]
        public void SwapScriptInPlace_ScriptableObjectToMonoBehaviour_Throws()
        {
            var source = ScriptableObject.CreateInstance<SwapSourceAsset>();
            AssetDatabase.CreateAsset(source, tempFolder.Path("Mismatch.asset"));

            Assert.Throws<ArgumentException>(() => AssetScriptSwapper.SwapScriptInPlace<SwapTargetBehaviour>(source));
        }

        [Test]
        public void SwapScriptInPlace_NonPersistentObject_Throws()
        {
            var source = ScriptableObject.CreateInstance<SwapSourceAsset>();
            try { Assert.Throws<ArgumentException>(() => AssetScriptSwapper.SwapScriptInPlace<SwapTargetAsset>(source)); }
            finally { Object.DestroyImmediate(source); }
        }
        #endregion
    }
}

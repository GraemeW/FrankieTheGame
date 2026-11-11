using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using LowDefMustard.Utils.Editor;

namespace LowDefMustard.Utils.Tests.Editor
{
    public class DeletedAssetTypesTests
    {
        // Covers DeletedAssetTypes through real AssetDatabase deletes in a temp folder
        //  - TestPredicate assets have no deletion handlers, so deleting them via AssetDatabase is safe

        // State
        private TempAssetFolder tempFolder;

        #region Setup
        [SetUp]
        public void SetUp() => tempFolder = new TempAssetFolder(nameof(DeletedAssetTypesTests));

        [TearDown]
        public void TearDown() => tempFolder.Dispose();
        #endregion

        #region Helpers
        private static string CreatePredicateAsset(string path)
        {
            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<TestPredicate>(), path);
            return path;
        }
        #endregion

        #region Tests
        [Test]
        public void DeleteAsset_InEditor_RecordsItsMainType()
        {
            string assetPath = CreatePredicateAsset(tempFolder.Path("Deleted.asset"));

            AssetDatabase.DeleteAsset(assetPath);
            List<Type> deletedTypes = DeletedAssetTypes.GetTypes(new[] { assetPath }, out bool hasUnrecordedAssetDeletion);

            CollectionAssert.AreEqual(new[] { typeof(TestPredicate) }, deletedTypes);
            Assert.IsFalse(hasUnrecordedAssetDeletion);
        }

        [Test]
        public void DeleteFolder_InEditor_RecordsContainedAssets()
        {
            string subfolderPath = tempFolder.CreateSubfolder("Sub");
            string assetPath = CreatePredicateAsset($"{subfolderPath}/Nested.asset");

            AssetDatabase.DeleteAsset(subfolderPath);
            List<Type> deletedTypes = DeletedAssetTypes.GetTypes(new[] { subfolderPath, assetPath }, out bool hasUnrecordedAssetDeletion);

            CollectionAssert.AreEqual(new[] { typeof(TestPredicate) }, deletedTypes);
            Assert.IsFalse(hasUnrecordedAssetDeletion);
        }

        [UnityTest]
        public IEnumerator Records_AreClearedOnTheNextEditorUpdate()
        {
            string assetPath = CreatePredicateAsset(tempFolder.Path("Cleared.asset"));
            AssetDatabase.DeleteAsset(assetPath);
            Assert.IsNotEmpty(DeletedAssetTypes.GetTypes(new[] { assetPath }, out _)); // recorded for this deletion's postprocessors

            yield return TestWait.Until(() => DeletedAssetTypes.GetTypes(new[] { assetPath }, out _).Count == 0);

            CollectionAssert.IsEmpty(DeletedAssetTypes.GetTypes(new[] { assetPath }, out bool hasUnrecordedAssetDeletion));
            Assert.IsTrue(hasUnrecordedAssetDeletion);
        }

        [Test]
        public void GetTypes_UnrecordedPaths_OnlyAssetFilesCountAsUnrecorded()
        {
            DeletedAssetTypes.GetTypes(new[] { "Assets/_NeverExisted_/Texture.png" }, out bool hasUnrecordedForTexture);
            DeletedAssetTypes.GetTypes(new[] { "Assets/_NeverExisted_/Thing.asset" }, out bool hasUnrecordedForAsset);

            Assert.IsFalse(hasUnrecordedForTexture);
            Assert.IsTrue(hasUnrecordedForAsset);
        }
        #endregion
    }
}

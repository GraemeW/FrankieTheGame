using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Localization;

namespace LowDefMustard.Localization.Tests.Editor
{
    public class LocalizationToolAssetCreationTests
    {
        // Deliberately disposable and named so a failed cleanup is self-documenting
        private const string _scratchTableName = "ScratchTest_LocTool_SafeToDelete";
        private const string _scratchTablePath = "Assets/Localization/Table_" + _scratchTableName;

        [SetUp]
        public void SetUp()
        {
            TestLocalizationTool.RegisterTableCollectionNames(new Dictionary<TestTableType, string>
            {
                { TestTableType.ScratchAsset, _scratchTableName }
            });
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            // Safety net beyond any per-test cleanup, in case a test throws before it runs
            if (AssetDatabase.IsValidFolder(_scratchTablePath))
            {
                AssetDatabase.DeleteAsset(_scratchTablePath);
            }
            TestLocalizationTool.RefreshTableCache();
        }

        [Test]
        public void GetOrMakeTableCollection_CreatesCollectionUnderLocalizationFolder()
        {
            bool created = TestLocalizationTool.GetOrMakeTableCollection(TestTableType.ScratchAsset, out StringTableCollection collection);

            Assert.IsTrue(created);
            Assert.IsNotNull(collection);
            Assert.IsTrue(AssetDatabase.IsValidFolder(_scratchTablePath));
        }
    }
}

using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using LowDefMustard.Zones.Editor;
using LowDefMustard.Utils.Tests;

namespace LowDefMustard.Zones.Tests.Editor
{
    public class MultiZoneViewCreateNewEntryTests
    {
        // State
        private TempAssetFolder tempFolder;
        private string scratchAssetPath;
        private MultiZoneView multiZoneView;

        #region Setup
        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            tempFolder = new TempAssetFolder(nameof(MultiZoneViewCreateNewEntryTests));
            scratchAssetPath = tempFolder.Path("ScratchTest_MultiZoneViewCreateNew.asset");
            multiZoneView = ScriptableObject.CreateInstance<MultiZoneView>();
            AssetDatabase.CreateAsset(multiZoneView, scratchAssetPath);
            AssetDatabase.SaveAssets();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            tempFolder.Dispose();
        }
        #endregion

        #region Tests
        [Test]
        public void CreateOrUpdateZoneViewData_UnknownZone_CreatesAndEmbedsNewEntry()
        {
            ZoneViewData result = multiZoneView.CreateOrUpdateZoneViewData("ZoneNew", "Assets/ZoneNew.unity", "Snapshot.png", new Vector2(10f, 20f), new Vector2(1f, 2f), false, false);

            Assert.IsNotNull(result);
            Assert.AreEqual("ZoneNew", result.zoneName);
            Assert.Contains(result, multiZoneView.zoneViewDataSet);
            Assert.AreEqual(scratchAssetPath, AssetDatabase.GetAssetPath(result));
        }
        #endregion
    }
}

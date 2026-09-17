using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using LowDefMustard.Zones.Editor;

namespace LowDefMustard.Zones.Tests.Editor
{
    public class MultiZoneViewCreateNewEntryTests
    {
        // Const Tunables
        private const string _scratchAssetPath = "Assets/ScratchTest_MultiZoneViewCreateNew_SafeToDelete.asset";
        
        // State
        private MultiZoneView multiZoneView;

        #region Setup
        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            multiZoneView = ScriptableObject.CreateInstance<MultiZoneView>();
            AssetDatabase.CreateAsset(multiZoneView, _scratchAssetPath);
            AssetDatabase.SaveAssets();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            AssetDatabase.DeleteAsset(_scratchAssetPath);
            AssetDatabase.Refresh();
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
            Assert.AreEqual(_scratchAssetPath, AssetDatabase.GetAssetPath(result));
        }
        #endregion
    }
}

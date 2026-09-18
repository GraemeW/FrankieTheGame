using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using LowDefMustard.Zones.Editor;

namespace LowDefMustard.Zones.Tests.Editor
{
    public class MultiZoneViewTests
    {
        // State
        private MultiZoneView multiZoneView;
        private readonly List<ZoneViewData> createdZoneViewData = new();

        #region Setup
        [SetUp]
        public void SetUp()
        {
            multiZoneView = ScriptableObject.CreateInstance<MultiZoneView>();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (ZoneViewData data in createdZoneViewData) { Object.DestroyImmediate(data); }
            createdZoneViewData.Clear();
            Object.DestroyImmediate(multiZoneView);
        }
        #endregion

        #region PrivateMethods
        private ZoneViewData AddExistingZoneViewData(string zoneName, string scenePath, Vector2 dimensions, Vector2 topLeftPosition)
        {
            var data = ScriptableObject.CreateInstance<ZoneViewData>();
            data.Setup(zoneName, scenePath, "OldSnapshot.png", dimensions, topLeftPosition);
            multiZoneView.zoneViewDataSet.Add(data);
            createdZoneViewData.Add(data);
            return data;
        }
        #endregion

        #region Tests
        [Test]
        public void CreateOrUpdateZoneViewData_ExistingZone_KeepFlagsTrue_PreservesPositionAndDimensions()
        {
            ZoneViewData existing = AddExistingZoneViewData("ZoneA", "Assets/ZoneA.unity", new Vector2(10f, 20f), new Vector2(1f, 2f));
            
            ZoneViewData result = multiZoneView.CreateOrUpdateZoneViewData("ZoneA", "Assets/ZoneA.unity", "NewSnapshot.png", new Vector2(99f, 99f), new Vector2(99f, 99f), true, true);

            Assert.AreSame(existing, result);
            Assert.AreEqual(new Vector2(1f, 2f), result.topLeftPosition);
            Assert.AreEqual(new Vector2(10f, 20f), result.dimensions);
            Assert.AreEqual("NewSnapshot.png", result.snapshotPath);
        }

        [Test]
        public void CreateOrUpdateZoneViewData_ExistingZone_KeepFlagsFalse_UsesProvidedValues()
        {
            AddExistingZoneViewData("ZoneA", "Assets/ZoneA.unity", new Vector2(10f, 20f), new Vector2(1f, 2f));
            
            ZoneViewData result = multiZoneView.CreateOrUpdateZoneViewData("ZoneA", "Assets/ZoneA.unity", "NewSnapshot.png", new Vector2(99f, 99f), new Vector2(5f, 6f), false, false);

            Assert.AreEqual(new Vector2(5f, 6f), result.topLeftPosition);
            Assert.AreEqual(new Vector2(99f, 99f), result.dimensions);
        }

        [Test]
        public void CleanDanglingZoneViewData_RemovesNullEntries()
        {
            AddExistingZoneViewData("ZoneA", "Assets/ZoneA.unity", Vector2.zero, Vector2.zero);
            multiZoneView.zoneViewDataSet.Add(null);

            multiZoneView.CleanDanglingZoneViewData();

            Assert.AreEqual(1, multiZoneView.zoneViewDataSet.Count);
        }

        [Test]
        public void GetScenePaths_ReturnsUniqueNonEmptyPaths()
        {
            AddExistingZoneViewData("ZoneA", "Assets/ZoneA.unity", Vector2.zero, Vector2.zero);
            AddExistingZoneViewData("ZoneB", "Assets/ZoneA.unity", Vector2.zero, Vector2.zero);
            AddExistingZoneViewData("ZoneC", "", Vector2.zero, Vector2.zero);

            HashSet<string> scenePaths = multiZoneView.GetScenePaths();

            Assert.AreEqual(1, scenePaths.Count);
            Assert.IsTrue(scenePaths.Contains("Assets/ZoneA.unity"));
        }

        [Test]
        public void UpdateZoneNodeData_UpdatesMatchingZone_SkipsUnknownZone()
        {
            ZoneViewData zoneA = AddExistingZoneViewData("ZoneA", "Assets/ZoneA.unity", Vector2.zero, Vector2.zero);
            var nodeData = new List<ZoneNodeData> { new("node1", new Vector2(0.5f, 0.5f)) };

            var update = new Dictionary<string, List<ZoneNodeData>>
            {
                { "ZoneA", nodeData },
                { "ZoneUnknown", nodeData },
            };

            multiZoneView.UpdateZoneNodeData(update);

            Assert.AreEqual(1, zoneA.zoneNodeDataSet.Count);
            Assert.AreEqual("node1", zoneA.zoneNodeDataSet[0].zoneNodeID);
        }
        #endregion
    }
}

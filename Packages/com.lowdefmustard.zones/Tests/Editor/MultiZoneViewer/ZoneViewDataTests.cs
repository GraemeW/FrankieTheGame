using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using LowDefMustard.Zones.Editor;

namespace LowDefMustard.Zones.Tests.Editor
{
    public class ZoneViewDataTests
    {
        // State
        private ZoneViewData zoneViewData;

        #region Setup
        [SetUp]
        public void SetUp()
        {
            zoneViewData = ScriptableObject.CreateInstance<ZoneViewData>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(zoneViewData);
        }
        #endregion

        #region Tests
        [Test]
        public void Setup_AssignsAllFields_AndClearsNodeData()
        {
            zoneViewData.Setup("ZoneA", "Assets/ZoneA.unity", "Snapshot.png", new Vector2(10f, 20f), new Vector2(1f, 2f));

            Assert.AreEqual("ZoneA", zoneViewData.zoneName);
            Assert.AreEqual("Assets/ZoneA.unity", zoneViewData.scenePath);
            Assert.AreEqual("Snapshot.png", zoneViewData.snapshotPath);
            Assert.AreEqual(new Vector2(10f, 20f), zoneViewData.dimensions);
            Assert.AreEqual(new Vector2(1f, 2f), zoneViewData.topLeftPosition);
            Assert.AreEqual(0, zoneViewData.zoneNodeDataSet.Count);
        }

        [Test]
        public void TryGetZoneNodeData_KnownID_ReturnsTrueWithData()
        {
            zoneViewData.SetZoneNodeData(new List<ZoneNodeData> { new("node1", new Vector2(0.2f, 0.3f)) });

            bool result = zoneViewData.TryGetZoneNodeData("node1", out ZoneNodeData data);

            Assert.IsTrue(result);
            Assert.AreEqual(new Vector2(0.2f, 0.3f), data.relativePosition);
        }

        [Test]
        public void TryGetZoneNodeData_UnknownID_ReturnsFalse()
        {
            bool result = zoneViewData.TryGetZoneNodeData("missing", out _);

            Assert.IsFalse(result);
        }

        [Test]
        public void TrySetLink_KnownID_UpdatesLinkAndReturnsTrue()
        {
            zoneViewData.SetZoneNodeData(new List<ZoneNodeData> { new("node1", Vector2.zero) });

            bool result = zoneViewData.TrySetLink("node1", "ZoneB", "node2", new Vector2(0.7f, 0.8f));

            Assert.IsTrue(result);
            zoneViewData.TryGetZoneNodeData("node1", out ZoneNodeData data);
            Assert.IsTrue(data.HasLink());
            Assert.AreEqual("ZoneB", data.linkedZoneName);
        }

        [Test]
        public void TrySetLink_UnknownID_ReturnsFalse()
        {
            bool result = zoneViewData.TrySetLink("missing", "ZoneB", "node2", Vector2.zero);

            Assert.IsFalse(result);
        }

        [Test]
        public void TryClearLink_WithExistingLink_ReturnsTrueAndClears()
        {
            zoneViewData.SetZoneNodeData(new List<ZoneNodeData> { new("node1", Vector2.zero) });
            zoneViewData.TrySetLink("node1", "ZoneB", "node2", Vector2.one);

            bool result = zoneViewData.TryClearLink("node1");

            Assert.IsTrue(result);
            zoneViewData.TryGetZoneNodeData("node1", out ZoneNodeData data);
            Assert.IsFalse(data.HasLink());
        }

        [Test]
        public void TryClearLink_NoExistingLink_ReturnsFalse()
        {
            zoneViewData.SetZoneNodeData(new List<ZoneNodeData> { new("node1", Vector2.zero) });

            bool result = zoneViewData.TryClearLink("node1");

            Assert.IsFalse(result);
        }

        [Test]
        public void UpdateZoneNodePositions_UpdatesMatchingIDs_LeavesOthersUnchanged()
        {
            zoneViewData.SetZoneNodeData(new List<ZoneNodeData>
            {
                new("node1", Vector2.zero),
                new("node2", Vector2.one),
            });

            var updates = new Dictionary<string, Vector2> { { "node1", new Vector2(0.9f, 0.9f) } };
            zoneViewData.UpdateZoneNodePositions(updates);

            zoneViewData.TryGetZoneNodeData("node1", out ZoneNodeData node1Data);
            zoneViewData.TryGetZoneNodeData("node2", out ZoneNodeData node2Data);
            Assert.AreEqual(new Vector2(0.9f, 0.9f), node1Data.relativePosition);
            Assert.AreEqual(Vector2.one, node2Data.relativePosition);
        }
        #endregion
    }
}

using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using LowDefMustard.Zones.Editor;

namespace LowDefMustard.Zones.Tests.Editor
{
    public class ZoneHandlerConduitTests
    {
        // State
        private readonly List<ZoneNode> createdNodes = new();
        private bool originalIgnoreFailingMessages;

        #region Setup
        [SetUp]
        public void SetUp()
        {
            originalIgnoreFailingMessages = LogAssert.ignoreFailingMessages;
        }

        [TearDown]
        public void TearDown()
        {
            LogAssert.ignoreFailingMessages = originalIgnoreFailingMessages;
            foreach (ZoneNode node in createdNodes) { Object.DestroyImmediate(node); }
            createdNodes.Clear();
        }
        #endregion
        
        #region PrivateMethods
        private ZoneNode CreateNode(string zoneName, string nodeName)
        {
            var node = ScriptableObject.CreateInstance<ZoneNode>();
            LogAssert.ignoreFailingMessages = true;
            node.SetZoneName(zoneName);
            node.SetNodeID(nodeName); // Need to set ID/name manually when creating instance
            LogAssert.ignoreFailingMessages = false;
            createdNodes.Add(node);
            return node;
        }
        #endregion
        
        #region Tests
        [Test]
        public void GetRelativePosition_AtBoundsCenter_ReturnsHalfHalf()
        {
            var bounds = new Bounds(new Vector3(50f, 50f, 0f), new Vector3(100f, 100f, 0f));

            Vector2 result = ZoneHandlerConduit.GetRelativePosition(new Vector2(50f, 50f), bounds);

            Assert.AreEqual(new Vector2(0.5f, 0.5f), result);
        }

        [Test]
        public void GetRelativePosition_OutsideBounds_ClampsToUnitRange()
        {
            var bounds = new Bounds(new Vector3(50f, 50f, 0f), new Vector3(100f, 100f, 0f));

            Vector2 result = ZoneHandlerConduit.GetRelativePosition(new Vector2(-200f, 500f), bounds);

            Assert.AreEqual(new Vector2(0f, 0f), result);
        }

        [Test]
        public void BuildZoneNodeData_GroupsEntriesByZoneName_WithRelativePositions()
        {
            ZoneNode nodeA1 = CreateNode("ZoneA", "nodeA1");
            ZoneNode nodeA2 = CreateNode("ZoneA", "nodeA2");
            ZoneNode nodeB1 = CreateNode("ZoneB", "nodeB1");

            var handlerData = new List<ZoneHandlerNodeData>
            {
                new(nodeA1, new Vector2(25f, 75f)),
                new(nodeA2, new Vector2(100f, 0f)),
                new(nodeB1, Vector2.zero),
            };

            var zoneDimensionsLookup = new Dictionary<string, Bounds> { { "ZoneA", new Bounds(new Vector3(50f, 50f, 0f), new Vector3(100f, 100f, 0f)) }, };

            Dictionary<string, List<ZoneNodeData>> result = ZoneHandlerConduit.BuildZoneNodeData(handlerData, zoneDimensionsLookup);

            Assert.IsTrue(result.ContainsKey("ZoneA"));
            Assert.IsFalse(result.ContainsKey("ZoneB"));
            Assert.AreEqual(2, result["ZoneA"].Count);

            ZoneNodeData entryA1 = result["ZoneA"].Find(entry => entry.zoneNodeID == nodeA1.GetNodeID());
            Assert.AreEqual(new Vector2(0.25f, 0.25f), entryA1.relativePosition);

            ZoneNodeData entryA2 = result["ZoneA"].Find(entry => entry.zoneNodeID == nodeA2.GetNodeID());
            
            Assert.AreEqual(new Vector2(1f, 1f), entryA2.relativePosition);
        }
        #endregion
    }
}

using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using LowDefMustard.Zones.Editor;

namespace LowDefMustard.Zones.Tests.Editor
{
    public class ZoneEdgesLayerTests
    {
        [Test]
        public void Constructor_SetsNamePickingModeAndPosition()
        {
            var layer = new ZoneEdgesLayer();

            Assert.AreEqual("zone-edges-layer", layer.name);
            Assert.AreEqual(PickingMode.Ignore, layer.pickingMode);
            Assert.AreEqual(Position.Absolute, layer.style.position.value);
            Assert.AreEqual(0f, layer.style.left.value.value);
            Assert.AreEqual(0f, layer.style.top.value.value);
        }

        [Test]
        public void SetEdges_WithList_AssignsEdges()
        {
            var layer = new ZoneEdgesLayer();
            var newEdges = new List<(Rect from, Rect to)> { (new Rect(0, 0, 10, 10), new Rect(20, 20, 10, 10)) };

            layer.SetEdges(newEdges);

            Assert.AreSame(newEdges, layer.edges);
        }

        [Test]
        public void SetEdges_WithNull_AssignsEmptyList()
        {
            var layer = new ZoneEdgesLayer();

            layer.SetEdges(null);

            Assert.IsNotNull(layer.edges);
            Assert.AreEqual(0, layer.edges.Count);
        }
    }
}

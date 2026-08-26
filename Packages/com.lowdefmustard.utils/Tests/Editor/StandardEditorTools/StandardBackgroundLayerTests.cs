using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using LowDefMustard.Utils.Editor;

namespace LowDefMustard.Utils.Tests.Editor
{
    public class StandardBackgroundLayerTests
    {
        // Tested:
        // - constructor's style/setup
        // - geometry math via GenerateGridLines/GenerateDotQuads
        // Not Tested:
        // - drawing execution (no access to Painter2D calls instead OnGenerateVisualContent):

        [Test]
        public void Constructor_SetsNameAndPickingMode()
        {
            var layer = new StandardBackgroundLayer(StandardBackgroundType.Lines);

            Assert.AreEqual("background-layer", layer.name);
            Assert.AreEqual(PickingMode.Ignore, layer.pickingMode);
        }

        [Test]
        public void Constructor_PositionsAbsoluteAndCentersOnOrigin()
        {
            var layer = new StandardBackgroundLayer(StandardBackgroundType.Lines);

            Assert.AreEqual(Position.Absolute, layer.style.position.value);
            // Left/top should be the negative half-extent, so the layer is centered on (0,0)
            Assert.AreEqual(-layer.style.width.value.value / 2f, layer.style.left.value.value, 0.01f);
            Assert.AreEqual(-layer.style.height.value.value / 2f, layer.style.top.value.value, 0.01f);
        }

        [Test]
        public void GenerateGridLines_SmallGrid_ReturnsExpectedLineCount()
        {
            // size=100, cellSize=50 -> x in {0,50,100} = 3 vertical + 3 horizontal
            var lines = StandardBackgroundLayer.GenerateGridLines(size: 100f, cellSize: 50f);

            Assert.AreEqual(6, lines.Count);
        }

        [Test]
        public void GenerateGridLines_VerticalLine_SpansFullHeight()
        {
            var lines = StandardBackgroundLayer.GenerateGridLines(size: 100f, cellSize: 50f);

            Assert.Contains((new Vector2(50, 0), new Vector2(50, 100)), lines);
        }

        [Test]
        public void GenerateGridLines_HorizontalLine_SpansFullWidth()
        {
            var lines = StandardBackgroundLayer.GenerateGridLines(size: 100f, cellSize: 50f);

            Assert.Contains((new Vector2(0, 50), new Vector2(100, 50)), lines);
        }

        [Test]
        public void GenerateGridLines_ZeroSize_ReturnsOneDegenerateLinePerAxis()
        {
            var lines = StandardBackgroundLayer.GenerateGridLines(size: 0f, cellSize: 50f);

            Assert.AreEqual(2, lines.Count); // one vertical, one horizontal, both zero-length
        }

        [Test]
        public void GenerateDotQuads_SmallGrid_ReturnsExpectedDotCount()
        {
            // size=100, cellSize=50 -> 3x3 grid of centers = 9 dots
            var quads = StandardBackgroundLayer.GenerateDotQuads(size: 100f, cellSize: 50f, radius: 1f);

            Assert.AreEqual(9, quads.Count);
        }

        [Test]
        public void GenerateDotQuads_CenterDot_HasCornersOffsetByRadius()
        {
            var quads = StandardBackgroundLayer.GenerateDotQuads(size: 100f, cellSize: 50f, radius: 1f);

            var centerQuad = quads.Find(q => q.bottomLeft == new Vector2(49, 49));
            Assert.AreEqual(new Vector2(51, 49), centerQuad.bottomRight);
            Assert.AreEqual(new Vector2(51, 51), centerQuad.topRight);
            Assert.AreEqual(new Vector2(49, 51), centerQuad.topLeft);
        }
    }
}

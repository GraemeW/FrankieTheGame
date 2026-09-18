using NUnit.Framework;
using UnityEngine;
using LowDefMustard.Zones.Editor;

namespace LowDefMustard.Zones.Tests.Editor
{
    public class ZoneViewTests
    {
        // State
        private ZoneViewData zoneViewData;
        private Texture2D texture;

        // Setup
        [SetUp]
        public void SetUp()
        {
            zoneViewData = ScriptableObject.CreateInstance<ZoneViewData>();
            texture = new Texture2D(1, 1);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(zoneViewData);
            Object.DestroyImmediate(texture);
        }

        // Tests
        [Test]
        public void Constructor_AssignsAllFields()
        {
            var dimensions = new Vector2(100f, 50f);
            var offset = new Vector2(10f, 20f);

            var view = new ZoneView(zoneViewData, texture, dimensions, offset);

            Assert.AreSame(zoneViewData, view.data);
            Assert.AreSame(texture, view.texture2D);
            Assert.AreEqual(dimensions, view.renderedImageDimensions);
            Assert.AreEqual(offset, view.renderedImageOffset);
        }

        [Test]
        public void MutableFields_CanBeReassignedAfterConstruction()
        {
            var view = new ZoneView(zoneViewData, texture, Vector2.zero, Vector2.zero)
            {
                renderedImageDimensions = new Vector2(1f, 1f),
                renderedImageOffset = new Vector2(2f, 2f)
            };

            Assert.AreEqual(new Vector2(1f, 1f), view.renderedImageDimensions);
            Assert.AreEqual(new Vector2(2f, 2f), view.renderedImageOffset);
        }
    }
}

using NUnit.Framework;
using UnityEngine;
using LowDefMustard.Utils.Tests;

namespace LowDefMustard.Zones.Tests.Editor
{
    public class ZoneHandlerBaseTests
    {
        // State
        private readonly TestObjectTracker tracker = new();
        private GameObject handlerGameObject;
        private GameObject warpGameObject;
        private ZoneNode zoneNode;

        #region Setup
        [TearDown]
        public void TearDown()
        {
            tracker.DestroyAll();
        }
        #endregion

        #region Tests
        [Test]
        public void GetWarpPosition_NoWarpTransform_ReturnsOwnTransformPosition()
        {
            handlerGameObject = tracker.Track(new GameObject("Handler") { transform = { position = new Vector3(1f, 2f, 3f) } });
            var handler = handlerGameObject.AddComponent<ZoneHandlerBase>();

            Assert.AreEqual(new Vector3(1f, 2f, 3f), handler.GetWarpPosition());
        }

        [Test]
        public void GetWarpPosition_WithWarpTransform_ReturnsWarpTransformPosition()
        {
            handlerGameObject = tracker.Track(new GameObject("Handler") { transform = { position = new Vector3(1f, 2f, 3f) } });
            var handler = handlerGameObject.AddComponent<ZoneHandlerBase>();

            warpGameObject = tracker.Track(new GameObject("Warp") { transform = { position = new Vector3(9f, 8f, 7f) } });
            handler.warpTransform = warpGameObject.transform;

            Assert.AreEqual(new Vector3(9f, 8f, 7f), handler.GetWarpPosition());
        }

        [Test]
        public void GetZoneNode_ReturnsSeededNode()
        {
            handlerGameObject = tracker.CreateGameObject("Handler");
            var handler = handlerGameObject.AddComponent<ZoneHandlerBase>();
            zoneNode = tracker.Create<ZoneNode>();
            zoneNode.preventLocalizationForTests = true;
            handler.zoneNode = zoneNode;

            Assert.AreSame(zoneNode, handler.GetZoneNode());
        }
        #endregion
    }
}

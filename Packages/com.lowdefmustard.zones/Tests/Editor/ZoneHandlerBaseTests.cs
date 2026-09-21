using NUnit.Framework;
using UnityEngine;

namespace LowDefMustard.Zones.Tests.Editor
{
    public class ZoneHandlerBaseTests
    {
        // State
        private GameObject handlerGameObject;
        private GameObject warpGameObject;
        private ZoneNode zoneNode;

        #region Setup
        [TearDown]
        public void TearDown()
        {
            if (handlerGameObject != null) { Object.DestroyImmediate(handlerGameObject); }
            if (warpGameObject != null) { Object.DestroyImmediate(warpGameObject); }
            if (zoneNode != null) { Object.DestroyImmediate(zoneNode); }
        }
        #endregion

        #region Tests
        [Test]
        public void GetWarpPosition_NoWarpTransform_ReturnsOwnTransformPosition()
        {
            handlerGameObject = new GameObject("Handler") { transform = { position = new Vector3(1f, 2f, 3f) } };
            var handler = handlerGameObject.AddComponent<ZoneHandlerBase>();

            Assert.AreEqual(new Vector3(1f, 2f, 3f), handler.GetWarpPosition());
        }

        [Test]
        public void GetWarpPosition_WithWarpTransform_ReturnsWarpTransformPosition()
        {
            handlerGameObject = new GameObject("Handler") { transform = { position = new Vector3(1f, 2f, 3f) } };
            var handler = handlerGameObject.AddComponent<ZoneHandlerBase>();

            warpGameObject = new GameObject("Warp") { transform = { position = new Vector3(9f, 8f, 7f) } };
            handler.warpTransform = warpGameObject.transform;

            Assert.AreEqual(new Vector3(9f, 8f, 7f), handler.GetWarpPosition());
        }

        [Test]
        public void GetZoneNode_ReturnsSeededNode()
        {
            handlerGameObject = new GameObject("Handler");
            var handler = handlerGameObject.AddComponent<ZoneHandlerBase>();
            zoneNode = ScriptableObject.CreateInstance<ZoneNode>();
            zoneNode.preventLocalizationForTests = true;
            handler.zoneNode = zoneNode;

            Assert.AreSame(zoneNode, handler.GetZoneNode());
        }
        #endregion
    }
}

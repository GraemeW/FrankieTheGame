using System.Collections.Generic;
using System.Linq;
using LowDefMustard.Zones.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace LowDefMustard.Zones.Tests.Editor
{
    // Covers MultiZoneViewer's node-dot drag-to-link flow: OnNodeDotDragStarted/Updated/Ended and the
    // TryLinkZoneNodes/TryClearZoneNodeLink paths they drive, via the internal callbacks the real
    // ZoneNodeLinkManipulator invokes rather than simulated mouse events (matches OnPlayModeStateChanged etc. above)
    public class MultiZoneViewerNodeDotLinkTests
    {
        // State
        private MultiZoneViewer viewer;
        private MultiZoneView multiZoneView;
        private Zone zoneA;
        private Zone zoneB;
        private ZoneNode nodeA;
        private ZoneNode nodeB;
        private ZoneViewData zoneViewDataA;
        private ZoneViewData zoneViewDataB;
        private Object originalSelection;
        private Dictionary<string, Zone> originalZoneLookupCache;
        private Dictionary<string, Zone> originalSceneReferenceCache;

        #region Setup
        [SetUp]
        public void SetUp()
        {
            originalSelection = Selection.activeObject;
            originalZoneLookupCache = Zone.zoneLookupCache;
            originalSceneReferenceCache = Zone.sceneReferenceCache;

            viewer = ScriptableObject.CreateInstance<MultiZoneViewer>();
            viewer.CreateGUI(); // activeMultiZoneView is still null here, so this only builds toolbar/canvas/layers

            zoneA = CreateZoneWithRootNode("ZoneA");
            zoneB = CreateZoneWithRootNode("ZoneB");
            nodeA = zoneA.GetRootNode();
            nodeB = zoneB.GetRootNode();
            Zone.zoneLookupCache = new Dictionary<string, Zone> { { "ZoneA", zoneA }, { "ZoneB", zoneB } };
            Zone.sceneReferenceCache = new Dictionary<string, Zone> { { "ZoneA", zoneA }, { "ZoneB", zoneB } };

            zoneViewDataA = MakeZoneViewData("ZoneA", new Vector2(0f, 0f), nodeA.GetNodeID());
            zoneViewDataB = MakeZoneViewData("ZoneB", new Vector2(500f, 0f), nodeB.GetNodeID());
            viewer.zoneViews.Add(new ZoneView(zoneViewDataA, null, Vector2.zero, Vector2.zero));
            viewer.zoneViews.Add(new ZoneView(zoneViewDataB, null, Vector2.zero, Vector2.zero));
            viewer.zoneViewLookup["ZoneA"] = viewer.zoneViews[0];
            viewer.zoneViewLookup["ZoneB"] = viewer.zoneViews[1];
            viewer.RefreshNodeDots();

            multiZoneView = ScriptableObject.CreateInstance<MultiZoneView>();
            viewer.activeMultiZoneView = multiZoneView;
        }

        [TearDown]
        public void TearDown()
        {
            Selection.activeObject = originalSelection;
            Zone.zoneLookupCache = originalZoneLookupCache;
            Zone.sceneReferenceCache = originalSceneReferenceCache;

            Object.DestroyImmediate(multiZoneView);
            Object.DestroyImmediate(zoneViewDataA);
            Object.DestroyImmediate(zoneViewDataB);
            foreach (ZoneNode node in zoneA.GetAllNodes()) { Object.DestroyImmediate(node); }
            foreach (ZoneNode node in zoneB.GetAllNodes()) { Object.DestroyImmediate(node); }
            Object.DestroyImmediate(zoneA);
            Object.DestroyImmediate(zoneB);
            Object.DestroyImmediate(viewer);
        }
        #endregion

        #region PrivateMethods
        private static Zone CreateZoneWithRootNode(string zoneName)
        {
            var zone = ScriptableObject.CreateInstance<Zone>();
            zone.preventLocalizationForTests = true;
            zone.name = zoneName;
            zone.CreateRootNodeIfMissing();
            zone.GetRootNode().preventLocalizationForTests = true;
            return zone;
        }

        private static ZoneViewData MakeZoneViewData(string zoneName, Vector2 topLeftPosition, string nodeID)
        {
            var zoneViewData = ScriptableObject.CreateInstance<ZoneViewData>();
            zoneViewData.Setup(zoneName, $"Assets/{zoneName}.unity", "snap.png", new Vector2(100f, 100f), topLeftPosition);
            zoneViewData.SetZoneNodeData(new List<ZoneNodeData> { new(nodeID, new Vector2(0.5f, 0.5f)) });
            return zoneViewData;
        }

        private static void AddDot(ZoneViewData zoneViewData, string nodeID, Vector2 relativePosition)
        {
            var zoneNodeDataSet = new List<ZoneNodeData>(zoneViewData.zoneNodeDataSet) { new(nodeID, relativePosition) };
            zoneViewData.SetZoneNodeData(zoneNodeDataSet);
        }

        private Vector2 GetDotCenter(string zoneName, string zoneNodeID) => viewer.nodeDotElements.First(entry => entry.zoneName == zoneName && entry.zoneNodeID == zoneNodeID).canvasRect.center;
        #endregion

        #region Tests
        [Test]
        public void DragBetweenTwoZoneDots_PastThreshold_LinksSourceNodeToTarget()
        {
            Vector2 sourceCentre = GetDotCenter("ZoneA", nodeA.GetNodeID());
            Vector2 targetCentre = GetDotCenter("ZoneB", nodeB.GetNodeID());
            Assert.GreaterOrEqual(Vector2.Distance(sourceCentre, targetCentre), MultiZoneViewer._uiNodeDotMinLinkDragDistance);

            viewer.OnNodeDotDragStarted("ZoneA", nodeA.GetNodeID());
            viewer.OnNodeDotDragUpdated(targetCentre);
            viewer.OnNodeDotDragEnded(targetCentre);
            
            Debug.Log(sourceCentre);
            Debug.Log(targetCentre);
            
            Assert.AreSame(nodeB, nodeA.GetLinkedZoneNode());
            Assert.IsTrue(zoneViewDataA.TryGetZoneNodeData(nodeA.GetNodeID(), out ZoneNodeData resultData));
            Assert.IsTrue(resultData.HasLink());
            Assert.AreEqual("ZoneB", resultData.linkedZoneName);
            Assert.AreEqual(nodeB.GetNodeID(), resultData.linkedZoneNodeID);
        }

        [Test]
        public void DragBetweenTwoZoneDots_PastThreshold_RefreshesDotsToLinkedStyle()
        {
            Vector2 sourceCentre = GetDotCenter("ZoneA", nodeA.GetNodeID());
            Vector2 targetCentre = GetDotCenter("ZoneB", nodeB.GetNodeID());

            viewer.OnNodeDotDragStarted("ZoneA", nodeA.GetNodeID());
            viewer.OnNodeDotDragEnded(targetCentre);

            // RefreshNodeDots is called at the end of a successful link, so the dot list should still describe both zones' dots
            Assert.AreEqual(2, viewer.nodeDotElements.Count);
            Assert.IsTrue(viewer.nodeDotElements.Any(entry => entry.zoneName == "ZoneA" && entry.zoneNodeID == nodeA.GetNodeID()));
            Assert.IsTrue(viewer.nodeDotElements.Any(entry => entry.zoneName == "ZoneB" && entry.zoneNodeID == nodeB.GetNodeID()));
        }

        [Test]
        public void DragToEmptyCanvasArea_PastThreshold_ClearsAnExistingLink()
        {
            Assert.IsTrue(nodeA.TrySetExternalLink(nodeB));
            zoneViewDataA.TrySetLink(nodeA.GetNodeID(), "ZoneB", nodeB.GetNodeID(), new Vector2(0.5f, 0.5f));

            viewer.OnNodeDotDragStarted("ZoneA", nodeA.GetNodeID());
            viewer.OnNodeDotDragEnded(new Vector2(99999f, 99999f)); // Far past the threshold, and nowhere near any dot

            Assert.IsNull(nodeA.GetLinkedZoneNode());
            Assert.IsTrue(zoneViewDataA.TryGetZoneNodeData(nodeA.GetNodeID(), out ZoneNodeData resultData));
            Assert.IsFalse(resultData.HasLink());
        }

        [Test]
        public void DragToEmptyCanvasArea_PastThreshold_WithNoExistingLink_ChangesNothing()
        {
            viewer.OnNodeDotDragStarted("ZoneA", nodeA.GetNodeID());
            viewer.OnNodeDotDragEnded(new Vector2(99999f, 99999f));

            Assert.IsNull(nodeA.GetLinkedZoneNode());
        }

        [Test]
        public void DragEndedAtSameSpotAsStart_BelowThreshold_SelectsTheClickedNodeAndDoesNotLink()
        {
            Vector2 sourceCentre = GetDotCenter("ZoneA", nodeA.GetNodeID());

            viewer.OnNodeDotDragStarted("ZoneA", nodeA.GetNodeID());
            viewer.OnNodeDotDragEnded(sourceCentre);

            Assert.AreSame(nodeA, Selection.activeObject);
            Assert.IsNull(nodeA.GetLinkedZoneNode());
        }

        [Test]
        public void OnNodeDotDragUpdated_BeforeAnyDragStarted_DoesNothing()
        {
            Vector2 targetCentre = GetDotCenter("ZoneB", nodeB.GetNodeID());

            viewer.OnNodeDotDragUpdated(targetCentre); // No matching OnNodeDotDragStarted call first

            Assert.IsFalse(viewer.isDraggingNodeLink);
        }

        [Test]
        public void OnNodeDotDragEnded_WithoutDragStarted_DoesNothing()
        {
            Selection.activeObject = zoneB; // Sentinel - a real selection change would replace this

            viewer.OnNodeDotDragEnded(GetDotCenter("ZoneB", nodeB.GetNodeID()));

            Assert.IsNull(nodeA.GetLinkedZoneNode());
            Assert.AreSame(zoneB, Selection.activeObject);
        }

        [Test]
        public void DragBetweenTwoDotsInTheSameZone_DoesNotLinkAndLeavesAnyExistingLinkUntouched()
        {
            ZoneNode nodeA2 = zoneA.CreateChildNode(nodeA); // A second real node/dot within ZoneA, distinct from nodeA
            zoneViewDataA.Setup("ZoneA", "Assets/ZoneA.unity", "snap.png", new Vector2(400f, 400f), Vector2.zero); // Wide enough that its own two dots clear the drag threshold
            zoneViewDataA.SetZoneNodeData(new List<ZoneNodeData> { new(nodeA.GetNodeID(), new Vector2(0.1f, 0.1f)) });
            AddDot(zoneViewDataA, nodeA2.GetNodeID(), new Vector2(0.9f, 0.9f));
            Assert.IsTrue(nodeA.TrySetExternalLink(nodeB)); // Pre-existing cross-zone link that a same-zone drag should not disturb
            zoneViewDataA.TrySetLink(nodeA.GetNodeID(), "ZoneB", nodeB.GetNodeID(), new Vector2(0.5f, 0.5f));
            viewer.RefreshNodeDots();
            Vector2 targetCentre = GetDotCenter("ZoneA", nodeA2.GetNodeID());

            viewer.OnNodeDotDragStarted("ZoneA", nodeA.GetNodeID());
            viewer.OnNodeDotDragEnded(targetCentre);

            Assert.AreSame(nodeB, nodeA.GetLinkedZoneNode()); // Unchanged - same-zone target was rejected before any save
        }
        #endregion
    }
}

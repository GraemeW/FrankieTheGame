using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEditor.SceneManagement;
using LowDefMustard.Zones.Editor;

namespace LowDefMustard.Zones.Tests.Editor
{
    public class ZoneHandlerConduitTests
    {
        // Const Tunables
        private const float _tolerance = 0.0001f;

        // State
        private readonly List<Zone> createdZones = new();
        private readonly List<ZoneNode> createdNodes = new();
        private readonly List<GameObject> spawnedObjects = new();
        private Dictionary<string, Zone> originalZoneLookupCache;
        private Dictionary<string, Zone> originalSceneReferenceCache;

        #region Setup
        [SetUp]
        public void SetUp()
        {
            originalZoneLookupCache = Zone.zoneLookupCache;
            originalSceneReferenceCache = Zone.sceneReferenceCache;
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); // New scene to avoid stray object pollution across tests
        }

        [TearDown]
        public void TearDown()
        {
            Zone.zoneLookupCache = originalZoneLookupCache;
            Zone.sceneReferenceCache = originalSceneReferenceCache;
            
            foreach (Zone zone in createdZones.Where(zone => zone != null)) { Object.DestroyImmediate(zone); }
            createdZones.Clear();
            foreach (GameObject spawnedObject in spawnedObjects.Where(spawnedObject => spawnedObject != null)) { Object.DestroyImmediate(spawnedObject); }
            spawnedObjects.Clear();
            foreach (ZoneNode node in createdNodes.Where(node => node != null)) { Object.DestroyImmediate(node); }
            createdNodes.Clear();
        }
        #endregion
        
        #region PrivateMethods
        // sceneName null leaves the zone's SceneReference unset
        private Zone CreateZone(string zoneName, string sceneName)
        {
            var zone = ScriptableObject.CreateInstance<Zone>();
            zone.preventLocalizationForTests = true;
            zone.name = zoneName;
            if (sceneName != null) { zone.sceneReference = sceneName; }
            createdZones.Add(zone);
            return zone;
        }
        
        private ZoneNode CreateNode(string zoneName, string nodeName)
        {
            var node = ScriptableObject.CreateInstance<ZoneNode>();
            node.preventLocalizationForTests = true;
            node.hideFlags = HideFlags.HideAndDontSave; // Keeps it alive across any scene transition
            
            LogAssert.ignoreFailingMessages = true;
            node.SetZoneName(zoneName);
            node.SetNodeID(nodeName); // Need to set ID/name manually when creating instance
            LogAssert.ignoreFailingMessages = false;
            createdNodes.Add(node);
            return node;
        }
        
        private ZoneHandlerBase CreateHandler(string objectName, Vector3 position, ZoneNode node, bool active = true)
        {
            var handlerObject = new GameObject(objectName) { transform = { position = position } };
            spawnedObjects.Add(handlerObject);

            var handler = handlerObject.AddComponent<ZoneHandlerBase>();
            handler.zoneNode = node;
            handlerObject.SetActive(active);
            return handler;
        }

        // Seeds both lookups directly (both must be non-empty, or Zone would fall through to a real Addressables load)
        private static void SeedZoneCache(params Zone[] zones)
        {
            Zone.zoneLookupCache = new Dictionary<string, Zone>();
            Zone.sceneReferenceCache = new Dictionary<string, Zone>();
            foreach (Zone zone in zones)
            {
                Zone.zoneLookupCache[zone.name] = zone;
                Zone.sceneReferenceCache[zone.name] = zone; // Keyed by zone name, since a zone with no scene reference has no usable scene key
            }
        }

        private static Bounds MakeBounds() => new(new Vector3(50f, 50f, 0f), new Vector3(100f, 100f, 0f));

        private static void AssertVector2Approximately(Vector2 expected, Vector2 actual)
        {
            Assert.AreEqual(expected.x, actual.x, _tolerance);
            Assert.AreEqual(expected.y, actual.y, _tolerance);
        }
        #endregion
        
        #region StandardTests
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

        [Test]
        public void BuildZoneNodeData_NullOrDestroyedZoneNodes_AreSkipped()
        {
            ZoneNode liveNode = CreateNode("ZoneA", "liveNode");
            ZoneNode destroyedNode = CreateNode("ZoneA", "destroyedNode");
            Object.DestroyImmediate(destroyedNode);
            createdNodes.Remove(destroyedNode);

            var handlerData = new List<ZoneHandlerNodeData>
            {
                new(null, Vector2.zero),
                new(destroyedNode, Vector2.zero),
                new(liveNode, new Vector2(50f, 50f)),
            };
            var zoneDimensionsLookup = new Dictionary<string, Bounds> { { "ZoneA", MakeBounds() } };

            Dictionary<string, List<ZoneNodeData>> result = ZoneHandlerConduit.BuildZoneNodeData(handlerData, zoneDimensionsLookup);

            Assert.AreEqual(1, result["ZoneA"].Count);
            Assert.AreEqual("liveNode", result["ZoneA"][0].zoneNodeID);
        }

        [Test]
        public void BuildZoneNodeData_LinkedNodeWhoseZoneHasASceneReference_RecordsLinkWithTargetRelativePosition()
        {
            ZoneNode source = CreateNode("ZoneA", "sourceNode");
            ZoneNode target = CreateNode("ZoneB", "targetNode");
            Assert.IsTrue(source.TrySetExternalLink(target));
            SeedZoneCache(CreateZone("ZoneA", "SceneA"), CreateZone("ZoneB", "SceneB"));

            var handlerData = new List<ZoneHandlerNodeData> { new(source, new Vector2(25f, 75f)), new(target, new Vector2(75f, 25f)) };
            var zoneDimensionsLookup = new Dictionary<string, Bounds> { { "ZoneA", MakeBounds() }, { "ZoneB", MakeBounds() } };

            Dictionary<string, List<ZoneNodeData>> result = ZoneHandlerConduit.BuildZoneNodeData(handlerData, zoneDimensionsLookup);

            ZoneNodeData sourceEntry = result["ZoneA"].Find(entry => entry.zoneNodeID == "sourceNode");
            Assert.IsTrue(sourceEntry.HasLink());
            Assert.AreEqual("ZoneB", sourceEntry.linkedZoneName);
            Assert.AreEqual("targetNode", sourceEntry.linkedZoneNodeID);
            // Bounds span (0,0)-(100,100) with a top-left origin, so target (75, 25) is 0.75 across and 0.75 down
            AssertVector2Approximately(new Vector2(0.75f, 0.75f), sourceEntry.linkedRelativePosition);
            AssertVector2Approximately(new Vector2(0.25f, 0.25f), sourceEntry.relativePosition);

            ZoneNodeData targetEntry = result["ZoneB"].Find(entry => entry.zoneNodeID == "targetNode");
            Assert.IsFalse(targetEntry.HasLink());
        }

        [Test]
        public void BuildZoneNodeData_TwoWayLinks_ResolvesBothDirections()
        {
            ZoneNode nodeA = CreateNode("ZoneA", "nodeA");
            ZoneNode nodeB = CreateNode("ZoneB", "nodeB");
            Assert.IsTrue(nodeA.TrySetExternalLink(nodeB));
            Assert.IsTrue(nodeB.TrySetExternalLink(nodeA));
            SeedZoneCache(CreateZone("ZoneA", "SceneA"), CreateZone("ZoneB", "SceneB"));

            var handlerData = new List<ZoneHandlerNodeData> { new(nodeA, new Vector2(25f, 75f)), new(nodeB, new Vector2(75f, 25f)) };
            var zoneDimensionsLookup = new Dictionary<string, Bounds> { { "ZoneA", MakeBounds() }, { "ZoneB", MakeBounds() } };

            Dictionary<string, List<ZoneNodeData>> result = ZoneHandlerConduit.BuildZoneNodeData(handlerData, zoneDimensionsLookup);

            ZoneNodeData entryA = result["ZoneA"][0];
            ZoneNodeData entryB = result["ZoneB"][0];
            Assert.AreEqual("nodeB", entryA.linkedZoneNodeID);
            AssertVector2Approximately(entryB.relativePosition, entryA.linkedRelativePosition);
            Assert.AreEqual("nodeA", entryB.linkedZoneNodeID);
            AssertVector2Approximately(entryA.relativePosition, entryB.linkedRelativePosition);
        }

        [Test]
        public void BuildZoneNodeData_LinkTargetHasNoHandlerData_LeavesLinkUnset()
        {
            ZoneNode source = CreateNode("ZoneA", "sourceNode");
            ZoneNode target = CreateNode("ZoneB", "targetNode");
            Assert.IsTrue(source.TrySetExternalLink(target));
            SeedZoneCache(CreateZone("ZoneA", "SceneA"), CreateZone("ZoneB", "SceneB"));

            // Only the source was found in its scene, so the target has no relative position to link to
            var handlerData = new List<ZoneHandlerNodeData> { new(source, new Vector2(25f, 75f)) };
            var zoneDimensionsLookup = new Dictionary<string, Bounds> { { "ZoneA", MakeBounds() }, { "ZoneB", MakeBounds() } };

            Dictionary<string, List<ZoneNodeData>> result = ZoneHandlerConduit.BuildZoneNodeData(handlerData, zoneDimensionsLookup);

            Assert.IsFalse(result["ZoneA"][0].HasLink());
        }

        [Test]
        public void BuildZoneNodeData_LinkTargetZoneHasNoSceneReference_LeavesLinkUnset()
        {
            ZoneNode source = CreateNode("ZoneA", "sourceNode");
            ZoneNode target = CreateNode("ZoneB", "targetNode");
            Assert.IsTrue(source.TrySetExternalLink(target));
            SeedZoneCache(CreateZone("ZoneA", "SceneA"), CreateZone("ZoneB", null));

            var handlerData = new List<ZoneHandlerNodeData> { new(source, new Vector2(25f, 75f)), new(target, new Vector2(75f, 25f)) };
            var zoneDimensionsLookup = new Dictionary<string, Bounds> { { "ZoneA", MakeBounds() }, { "ZoneB", MakeBounds() } };

            Dictionary<string, List<ZoneNodeData>> result = ZoneHandlerConduit.BuildZoneNodeData(handlerData, zoneDimensionsLookup);

            Assert.IsFalse(result["ZoneA"][0].HasLink());
        }

        [Test]
        public void BuildZoneNodeData_LinkTargetZoneNotInCache_LeavesLinkUnset()
        {
            ZoneNode source = CreateNode("ZoneA", "sourceNode");
            ZoneNode target = CreateNode("ZoneB", "targetNode");
            Assert.IsTrue(source.TrySetExternalLink(target));
            SeedZoneCache(CreateZone("ZoneA", "SceneA")); // "ZoneB" deliberately absent

            var handlerData = new List<ZoneHandlerNodeData> { new(source, new Vector2(25f, 75f)), new(target, new Vector2(75f, 25f)) };
            var zoneDimensionsLookup = new Dictionary<string, Bounds> { { "ZoneA", MakeBounds() }, { "ZoneB", MakeBounds() } };

            Dictionary<string, List<ZoneNodeData>> result = ZoneHandlerConduit.BuildZoneNodeData(handlerData, zoneDimensionsLookup);

            Assert.IsFalse(result["ZoneA"][0].HasLink());
        }

        [Test]
        public void BuildZoneNodeData_LinkedSourceZoneMissingFromDimensionsLookup_SkipsItsEntryAndLink()
        {
            ZoneNode source = CreateNode("ZoneA", "sourceNode");
            ZoneNode target = CreateNode("ZoneB", "targetNode");
            Assert.IsTrue(source.TrySetExternalLink(target));
            SeedZoneCache(CreateZone("ZoneA", "SceneA"), CreateZone("ZoneB", "SceneB"));

            var handlerData = new List<ZoneHandlerNodeData> { new(source, new Vector2(25f, 75f)), new(target, new Vector2(75f, 25f)) };
            var zoneDimensionsLookup = new Dictionary<string, Bounds> { { "ZoneB", MakeBounds() } }; // No bounds for ZoneA

            Dictionary<string, List<ZoneNodeData>> result = ZoneHandlerConduit.BuildZoneNodeData(handlerData, zoneDimensionsLookup);

            Assert.IsFalse(result.ContainsKey("ZoneA"));
            Assert.AreEqual(1, result["ZoneB"].Count);
            Assert.IsFalse(result["ZoneB"][0].HasLink());
        }
        #endregion
        
        #region SceneScanTests
        [Test]
        public void BuildZoneHandlerNodeData_SceneWithNoHandlers_ReturnsEmptyList()
        {
            List<ZoneHandlerNodeData> result = ZoneHandlerConduit.BuildZoneHandlerNodeData();

            CollectionAssert.IsEmpty(result);
        }

        [Test]
        public void BuildZoneHandlerNodeData_HandlerWithNode_ReturnsNodeAtOwnPosition()
        {
            ZoneNode node = CreateNode("ZoneA", "nodeA");
            CreateHandler("HandlerA", new Vector3(3f, 4f, 5f), node);

            List<ZoneHandlerNodeData> result = ZoneHandlerConduit.BuildZoneHandlerNodeData();

            Assert.AreEqual(1, result.Count);
            Assert.AreSame(node, result[0].zoneNode);
            Assert.AreEqual(new Vector2(3f, 4f), result[0].position);
        }

        [Test]
        public void BuildZoneHandlerNodeData_HandlerWithWarpTransform_UsesWarpPosition()
        {
            ZoneNode node = CreateNode("ZoneA", "nodeA");
            ZoneHandlerBase handler = CreateHandler("HandlerA", new Vector3(3f, 4f, 5f), node);
            var warpObject = new GameObject("Warp") { transform = { position = new Vector3(9f, 8f, 7f) } };
            spawnedObjects.Add(warpObject);
            handler.warpTransform = warpObject.transform;

            List<ZoneHandlerNodeData> result = ZoneHandlerConduit.BuildZoneHandlerNodeData();

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(new Vector2(9f, 8f), result[0].position);
        }

        [Test]
        public void BuildZoneHandlerNodeData_HandlerWithoutNode_IsSkipped()
        {
            ZoneNode node = CreateNode("ZoneA", "nodeA");
            CreateHandler("HandlerWithNode", Vector3.zero, node);
            CreateHandler("HandlerWithoutNode", Vector3.one, null);

            List<ZoneHandlerNodeData> result = ZoneHandlerConduit.BuildZoneHandlerNodeData();

            Assert.AreEqual(1, result.Count);
            Assert.AreSame(node, result[0].zoneNode);
        }

        [Test]
        public void BuildZoneHandlerNodeData_InactiveHandler_IsStillIncluded()
        {
            ZoneNode node = CreateNode("ZoneA", "nodeA");
            CreateHandler("InactiveHandler", new Vector3(1f, 2f, 0f), node, active: false);

            List<ZoneHandlerNodeData> result = ZoneHandlerConduit.BuildZoneHandlerNodeData();

            Assert.AreEqual(1, result.Count);
            Assert.AreSame(node, result[0].zoneNode);
        }

        [Test]
        public void BuildZoneHandlerNodeData_MultipleHandlers_ReturnsOneEntryPerHandlerWithANode()
        {
            ZoneNode nodeOne = CreateNode("ZoneA", "nodeOne");
            ZoneNode nodeTwo = CreateNode("ZoneA", "nodeTwo");
            ZoneNode nodeThree = CreateNode("ZoneB", "nodeThree");
            CreateHandler("HandlerOne", new Vector3(1f, 0f, 0f), nodeOne);
            CreateHandler("HandlerTwo", new Vector3(2f, 0f, 0f), nodeTwo);
            CreateHandler("HandlerThree", new Vector3(3f, 0f, 0f), nodeThree, active: false);
            CreateHandler("HandlerNone", new Vector3(4f, 0f, 0f), null);

            List<ZoneHandlerNodeData> result = ZoneHandlerConduit.BuildZoneHandlerNodeData();

            // Scan order isn't guaranteed, so compare as sets
            CollectionAssert.AreEquivalent(new[] { nodeOne, nodeTwo, nodeThree }, result.Select(entry => entry.zoneNode).ToList());
        }
        #endregion
    }
}

using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using LowDefMustard.Zones.Editor;

namespace LowDefMustard.Zones.Tests.Editor
{
    public class ZoneHandlerConduitLinkedScenePathsTests
    {
        // Const Tunables
        private const string _scratchFolderName = "ScratchTest_ConduitLinkFollowing_SafeToDelete";
        private const string _scratchFolder = "Assets/" + _scratchFolderName;
        private const string _scratchScenePath = "Assets/ScratchTest_OpenLinkedScenePaths_SafeToDelete.unity";
        private const string _scratchSceneName = "ScratchTest_OpenLinkedScenePaths_SafeToDelete";
        private const string _noScenePathWarning = "Scene path is not configured!  Please re-link the scene reference in editor";

        // State
        private Zone rootZone;
        private readonly List<Zone> createdZones = new();
        private Dictionary<string, Zone> originalZoneLookupCache;
        private Dictionary<string, Zone> originalSceneReferenceCache;

        #region Setup
        [SetUp]
        public void SetUp()
        {
            originalZoneLookupCache = Zone.zoneLookupCache;
            originalSceneReferenceCache = Zone.sceneReferenceCache;
            
            Scene scratchScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scratchScene, _scratchScenePath);

            rootZone = ScriptableObject.CreateInstance<Zone>();
            rootZone.preventLocalizationForTests = true;
            rootZone.hideFlags = HideFlags.HideAndDontSave;
            rootZone.sceneReference = _scratchSceneName;
            rootZone.sceneReference.scenePath = _scratchScenePath;
            
            if (AssetDatabase.IsValidFolder(_scratchFolder)) { AssetDatabase.DeleteAsset(_scratchFolder); }
            AssetDatabase.CreateFolder("Assets", _scratchFolderName);
        }

        [TearDown]
        public void TearDown()
        {
            Zone.zoneLookupCache = originalZoneLookupCache;
            Zone.sceneReferenceCache = originalSceneReferenceCache;
            
            Object.DestroyImmediate(rootZone);
            foreach (Zone zone in createdZones) { Object.DestroyImmediate(zone); }
            createdZones.Clear();
            
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); // Nothing scratch left open when deleting
            AssetDatabase.DeleteAsset(_scratchScenePath);
            AssetDatabase.DeleteAsset(_scratchFolder);
            AssetDatabase.Refresh();
        }
        #endregion

        #region PrivateMethods
        private static string ScenePath(string sceneSuffix) => $"{_scratchFolder}/Scene{sceneSuffix}.unity";

        private static ZoneNode CreateNodeAsset(string zoneName, string nodeID)
        {
            var node = ScriptableObject.CreateInstance<ZoneNode>();
            node.preventLocalizationForTests = true;

            LogAssert.ignoreFailingMessages = true;
            node.SetZoneName(zoneName);
            node.SetNodeID(nodeID);
            LogAssert.ignoreFailingMessages = false;

            string path = $"{_scratchFolder}/{nodeID}.asset";
            AssetDatabase.CreateAsset(node, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(path); // Save/import (step 1 for persistence across scene loads)

            ZoneNode loadedNode = AssetDatabase.LoadAssetAtPath<ZoneNode>(path);
            loadedNode.hideFlags |= HideFlags.DontUnloadUnusedAsset; // Adjust flags (step 2 for persistence across scene loads)
            return loadedNode;
        }

        private static void Link(ZoneNode source, ZoneNode target)
        {
            Assert.IsTrue(source.TrySetExternalLink(target));
        }

        // Saves a scene with one ZoneHandlerBase per node (call after the nodes' links are set and SaveAssets has run)
        private static void CreateSceneWithHandlers(string scenePath, params ZoneNode[] handlerNodes)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            foreach (ZoneNode node in handlerNodes)
            {
                var handlerObject = new GameObject($"Handler_{node.GetNodeID()}");
                handlerObject.AddComponent<ZoneHandlerBase>().zoneNode = node;
            }
            EditorSceneManager.SaveScene(scene, scenePath);
        }
        
        private Zone CreateZone(string zoneName, string scenePath) // Note: scenePath null leaves the zone's SceneReference unset
        {
            var zone = ScriptableObject.CreateInstance<Zone>();
            zone.preventLocalizationForTests = true;
            zone.hideFlags = HideFlags.HideAndDontSave; // Opening scenes can unload loose unreferenced objects
            zone.name = zoneName;
            if (scenePath != null)
            {
                zone.sceneReference = Path.GetFileNameWithoutExtension(scenePath);
                zone.sceneReference.scenePath = scenePath;
            }
            createdZones.Add(zone);
            return zone;
        }

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

        private static List<string> Traverse(Zone rootZone, HashSet<string> existingViewScenePaths = null)
        {
            return ZoneHandlerConduit.OpenLinkedScenePaths(rootZone, maxZoneCount: 10, existingViewScenePaths ?? new HashSet<string>(), showProgressBar: false).ToList();
        }
        #endregion
        
        #region OpenLinkedScenePathsTests
        [Test]
        public void OpenLinkedScenePaths_NoLinkedZones_YieldsOnlyRootScenePath()
        {
            List<string> result = ZoneHandlerConduit.OpenLinkedScenePaths(rootZone, maxZoneCount: 1, existingViewScenePaths: new HashSet<string>(), showProgressBar: false).ToList();

            CollectionAssert.AreEqual(new[] { _scratchScenePath }, result);
        }

        [Test]
        public void OpenLinkedScenePaths_RootAlreadyInExistingViews_StillYieldsItExactlyOnce()
        {
            var existingViewScenePaths = new HashSet<string> { _scratchScenePath };

            List<string> result = ZoneHandlerConduit.OpenLinkedScenePaths(rootZone, maxZoneCount: 1, existingViewScenePaths, showProgressBar: false).ToList();

            CollectionAssert.AreEqual(new[] { _scratchScenePath }, result);
        }

        [Test]
        public void OpenLinkedScenePaths_NullRootZone_YieldsNothing()
        {
            List<string> result = ZoneHandlerConduit.OpenLinkedScenePaths(null, maxZoneCount: 1, new HashSet<string>(), showProgressBar: false).ToList();

            CollectionAssert.IsEmpty(result);
        }

        [Test]
        public void OpenLinkedScenePaths_RootZoneWithoutSceneName_YieldsNothing()
        {
            rootZone.sceneReference = default;

            List<string> result = ZoneHandlerConduit.OpenLinkedScenePaths(rootZone, maxZoneCount: 1, new HashSet<string>(), showProgressBar: false).ToList();

            CollectionAssert.IsEmpty(result);
        }

        [Test]
        public void OpenLinkedScenePaths_RootZoneWithNameButNoScenePath_WarnsAndYieldsNothing()
        {
            rootZone.sceneReference = _scratchSceneName; // Name only - no cached path or scene asset to resolve one from

            LogAssert.Expect(LogType.Warning, _noScenePathWarning);
            List<string> result = ZoneHandlerConduit.OpenLinkedScenePaths(rootZone, maxZoneCount: 1, new HashSet<string>(), showProgressBar: false).ToList();

            CollectionAssert.IsEmpty(result);
        }

        [Test]
        public void OpenLinkedScenePaths_ExistingViewWithEmptyPath_IsSkippedWithoutOpeningAScene()
        {
            var existingViewScenePaths = new HashSet<string> { string.Empty };

            List<string> result = ZoneHandlerConduit.OpenLinkedScenePaths(rootZone, maxZoneCount: 1, existingViewScenePaths, showProgressBar: false).ToList();

            CollectionAssert.AreEqual(new[] { _scratchScenePath }, result);
        }
        #endregion
        
        #region LinkFollowingTests
        [Test]
        public void OpenLinkedScenePaths_LinkedZone_IsFollowedAndYieldedAfterRoot()
        {
            ZoneNode nodeA = CreateNodeAsset("ZoneA", "NodeA");
            ZoneNode nodeB = CreateNodeAsset("ZoneB", "NodeB");
            Link(nodeA, nodeB);
            AssetDatabase.SaveAssets();
            CreateSceneWithHandlers(ScenePath("A"), nodeA);
            CreateSceneWithHandlers(ScenePath("B"), nodeB);
            Zone zoneA = CreateZone("ZoneA", ScenePath("A"));
            SeedZoneCache(zoneA, CreateZone("ZoneB", ScenePath("B")));

            List<string> result = Traverse(zoneA);

            CollectionAssert.AreEqual(new[] { ScenePath("A"), ScenePath("B") }, result);
        }

        [Test]
        public void OpenLinkedScenePaths_LinkChain_IsFollowedTransitively()
        {
            ZoneNode nodeA = CreateNodeAsset("ZoneA", "NodeA");
            ZoneNode nodeB = CreateNodeAsset("ZoneB", "NodeB");
            ZoneNode nodeC = CreateNodeAsset("ZoneC", "NodeC");
            Link(nodeA, nodeB);
            Link(nodeB, nodeC);
            AssetDatabase.SaveAssets();
            CreateSceneWithHandlers(ScenePath("A"), nodeA);
            CreateSceneWithHandlers(ScenePath("B"), nodeB);
            CreateSceneWithHandlers(ScenePath("C"), nodeC);
            Zone zoneA = CreateZone("ZoneA", ScenePath("A"));
            SeedZoneCache(zoneA, CreateZone("ZoneB", ScenePath("B")), CreateZone("ZoneC", ScenePath("C")));

            List<string> result = Traverse(zoneA);

            CollectionAssert.AreEqual(new[] { ScenePath("A"), ScenePath("B"), ScenePath("C") }, result);
        }

        [Test]
        public void OpenLinkedScenePaths_LinkBackToAlreadyVisitedScene_DoesNotRevisitIt()
        {
            ZoneNode nodeA = CreateNodeAsset("ZoneA", "NodeA");
            ZoneNode nodeB = CreateNodeAsset("ZoneB", "NodeB");
            Link(nodeA, nodeB);
            Link(nodeB, nodeA);
            AssetDatabase.SaveAssets();
            CreateSceneWithHandlers(ScenePath("A"), nodeA);
            CreateSceneWithHandlers(ScenePath("B"), nodeB);
            Zone zoneA = CreateZone("ZoneA", ScenePath("A"));
            SeedZoneCache(zoneA, CreateZone("ZoneB", ScenePath("B")));

            List<string> result = Traverse(zoneA);

            CollectionAssert.AreEqual(new[] { ScenePath("A"), ScenePath("B") }, result);
        }

        [Test]
        public void OpenLinkedScenePaths_SeveralLinksIncludingDuplicates_YieldsEachSceneExactlyOnce()
        {
            ZoneNode nodeA1 = CreateNodeAsset("ZoneA", "NodeA1");
            ZoneNode nodeA2 = CreateNodeAsset("ZoneA", "NodeA2");
            ZoneNode nodeA3 = CreateNodeAsset("ZoneA", "NodeA3");
            ZoneNode nodeB = CreateNodeAsset("ZoneB", "NodeB");
            ZoneNode nodeC = CreateNodeAsset("ZoneC", "NodeC");
            Link(nodeA1, nodeB);
            Link(nodeA2, nodeB); // Second door into the same scene
            Link(nodeA3, nodeC);
            Link(nodeB, nodeA1);
            Link(nodeC, nodeA1);
            AssetDatabase.SaveAssets();
            CreateSceneWithHandlers(ScenePath("A"), nodeA1, nodeA2, nodeA3);
            CreateSceneWithHandlers(ScenePath("B"), nodeB);
            CreateSceneWithHandlers(ScenePath("C"), nodeC);
            Zone zoneA = CreateZone("ZoneA", ScenePath("A"));
            SeedZoneCache(zoneA, CreateZone("ZoneB", ScenePath("B")), CreateZone("ZoneC", ScenePath("C")));

            List<string> result = Traverse(zoneA);

            // Handler scan order isn't guaranteed, so only the root's position is fixed
            Assert.AreEqual(ScenePath("A"), result[0]);
            CollectionAssert.AreEquivalent(new[] { ScenePath("A"), ScenePath("B"), ScenePath("C") }, result);
        }

        [Test]
        public void OpenLinkedScenePaths_ExistingViewScenePaths_AreYieldedBeforeRootAndLinkedScenes()
        {
            ZoneNode nodeA = CreateNodeAsset("ZoneA", "NodeA");
            ZoneNode nodeB = CreateNodeAsset("ZoneB", "NodeB");
            ZoneNode nodeC = CreateNodeAsset("ZoneC", "NodeC");
            Link(nodeA, nodeB);
            Link(nodeC, nodeA);
            AssetDatabase.SaveAssets();
            CreateSceneWithHandlers(ScenePath("A"), nodeA);
            CreateSceneWithHandlers(ScenePath("B"), nodeB);
            CreateSceneWithHandlers(ScenePath("C"), nodeC);
            Zone zoneA = CreateZone("ZoneA", ScenePath("A"));
            SeedZoneCache(zoneA, CreateZone("ZoneB", ScenePath("B")), CreateZone("ZoneC", ScenePath("C")));

            // C is already on the map, so it goes first; its link into A and the root itself then both resolve to a single visit of A
            List<string> result = Traverse(zoneA, new HashSet<string> { ScenePath("C") });

            CollectionAssert.AreEqual(new[] { ScenePath("C"), ScenePath("A"), ScenePath("B") }, result);
        }

        [Test]
        public void OpenLinkedScenePaths_LinkedNodeWhoseZoneHasNoSceneReference_IsNotFollowed()
        {
            ZoneNode nodeA = CreateNodeAsset("ZoneA", "NodeA");
            ZoneNode nodeNoScene = CreateNodeAsset("ZoneNoScene", "NodeNoScene");
            Link(nodeA, nodeNoScene);
            AssetDatabase.SaveAssets();
            CreateSceneWithHandlers(ScenePath("A"), nodeA);
            Zone zoneA = CreateZone("ZoneA", ScenePath("A"));
            SeedZoneCache(zoneA, CreateZone("ZoneNoScene", null));

            List<string> result = Traverse(zoneA);

            CollectionAssert.AreEqual(new[] { ScenePath("A") }, result);
        }

        [Test]
        public void OpenLinkedScenePaths_LinkedNodeWhoseZoneIsNotInTheCache_IsNotFollowed()
        {
            ZoneNode nodeA = CreateNodeAsset("ZoneA", "NodeA");
            ZoneNode nodeUnknown = CreateNodeAsset("ZoneUnknown", "NodeUnknown");
            Link(nodeA, nodeUnknown);
            AssetDatabase.SaveAssets();
            CreateSceneWithHandlers(ScenePath("A"), nodeA);
            Zone zoneA = CreateZone("ZoneA", ScenePath("A"));
            SeedZoneCache(zoneA); // "ZoneUnknown" deliberately absent

            List<string> result = Traverse(zoneA);

            CollectionAssert.AreEqual(new[] { ScenePath("A") }, result);
        }
        #endregion
    }
}

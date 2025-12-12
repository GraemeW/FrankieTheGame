using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Frankie.Core;

namespace Frankie.ZoneManagement
{
    [CreateAssetMenu(fileName = "New Zone", menuName = "Zone/New Zone")]
    public class Zone : ScriptableObject, ISerializationCallbackReceiver, IAddressablesCache
    {
        // Tunables
        [Header("Editor Settings")]
        [SerializeField] private Vector2 newNodeOffset = new(100f, 25f);
        [SerializeField] private int nodeWidth = 430;
        [SerializeField] private int nodeHeight = 150;
        [Header("Zone Properties")]
        [SerializeField] private SceneReference sceneReference;
        [SerializeField] private bool updateMap = false;
        [SerializeField] private AudioClip zoneAudio;
        [SerializeField] private bool isZoneAudioLooping = true;

        // State
        [HideInInspector] [SerializeField] private List<ZoneNode> zoneNodes = new();
        [HideInInspector] [SerializeField] private Dictionary<string, ZoneNode> nodeLookup = new();

        private static AsyncOperationHandle<IList<Zone>> _addressablesLoadHandle;
        private static Dictionary<string, Zone> _zoneLookupCache;
        private static Dictionary<string, Zone> _sceneReferenceCache;

        #region AddressablesCaching
        public static Zone GetFromName(string zoneName)
        {
            if (string.IsNullOrWhiteSpace(zoneName)) { return null; }
            BuildCacheIfEmpty();
            return _zoneLookupCache.GetValueOrDefault(zoneName);
        }

        public static Zone GetFromSceneReference(string sceneReference)
        {
            if (string.IsNullOrWhiteSpace(sceneReference)) { return null; }

            Debug.Log($"Attempting to load zone from scene reference {sceneReference}");
            BuildCacheIfEmpty();
            return _sceneReferenceCache.GetValueOrDefault(sceneReference);
        }

        public static void BuildCacheIfEmpty()
        {
            if (_sceneReferenceCache != null) { return; }
            BuildZoneCache();
        }

        private static void BuildZoneCache()
        {
            _zoneLookupCache = new Dictionary<string, Zone>();
            _sceneReferenceCache = new Dictionary<string, Zone>();
            Debug.Log("Building zone cache");
            _addressablesLoadHandle = Addressables.LoadAssetsAsync(nameof(Zone), (Zone zone) =>
            {
                if (_zoneLookupCache.ContainsKey(zone.name) || _sceneReferenceCache.ContainsKey(zone.GetSceneReference().SceneName))
                {
                    Debug.LogError($"Looks like there's a duplicate ID for objects: {_zoneLookupCache[zone.name]} and {zone}");
                }

                _zoneLookupCache[zone.name] = zone;
                _sceneReferenceCache[zone.GetSceneReference().SceneName] = zone;
            }
            );
            _addressablesLoadHandle.WaitForCompletion();
        }

        public static void ReleaseCache()
        {
            Addressables.Release(_addressablesLoadHandle);
        }
        #endregion

        #region UnityMethods
        private void Awake()
        {
#if UNITY_EDITOR
            // Deprecated Approach
            // Note:  We now make in ZoneEditor due to some intricacies in how Unity calls Awake on ScriptableObjects in Editor vs. the serialization callback
            // For (unknown) reasons, the root node gets made and then killed by the time serialization occurs
            //CreateRootNodeIfMissing();
#endif
        }

        private void OnValidate()
        {
            nodeLookup = new Dictionary<string, ZoneNode>();
            foreach (ZoneNode zoneNode in zoneNodes)
            {
                nodeLookup.Add(zoneNode.name, zoneNode);
            }
        }
        #endregion

        #region PublicMethods
        public SceneReference GetSceneReference() => sceneReference;
        public AudioClip GetZoneAudio() => zoneAudio;
        public bool IsZoneAudioLooping() => isZoneAudioLooping;
        public bool ShouldUpdateMap() => updateMap;
        public IEnumerable<ZoneNode> GetAllNodes() => zoneNodes;
        public ZoneNode GetRootNode() => zoneNodes[0];
        public bool IsRelated(ZoneNode parentNode, ZoneNode childNode) => parentNode.GetChildren() != null && parentNode.GetChildren().Contains(childNode.name);
        
        public ZoneNode GetNodeFromID(string zoneNodeName) => zoneNodes.FirstOrDefault(zoneNode => zoneNode.name == zoneNodeName);

        public IEnumerable<ZoneNode> GetAllChildren(ZoneNode parentNode)
        {
            if (parentNode == null || parentNode.GetChildren() == null || parentNode.GetChildren().Count == 0) { yield break; }
            foreach (var childUniqueID in parentNode.GetChildren().Where(childUniqueID => nodeLookup.ContainsKey(childUniqueID)))
            {
                yield return nodeLookup[childUniqueID];
            }
        }
        #endregion
        
        #region EditorMethods
#if UNITY_EDITOR
        private ZoneNode CreateNode()
        {
            var zoneNode = CreateInstance<ZoneNode>();
            Undo.RegisterCreatedObjectUndo(zoneNode, "Created Zone Node Object");
            zoneNode.Initialize(nodeWidth, nodeHeight);
            zoneNode.SetNodeID(System.Guid.NewGuid().ToString());
            zoneNode.SetZoneName(name);

            Undo.RecordObject(this, "Add Zone Node");
            zoneNodes.Add(zoneNode);

            return zoneNode;
        }

        public void CreateChildNode(ZoneNode parentNode)
        {
            if (parentNode == null) { return; }

            ZoneNode childNode = CreateNode();
            parentNode.AddChild(childNode.name);

            Vector2 offsetPosition = new Vector2(parentNode.GetRect().xMax + newNodeOffset.x,
                parentNode.GetRect().yMin + (parentNode.GetRect().height + newNodeOffset.y) * (parentNode.GetChildren().Count - 1)); // Offset position by 1 since child just added
            childNode.SetPosition(offsetPosition);

            OnValidate();
        }

        public void CreateRootNodeIfMissing()
        {
            if (zoneNodes.Count > 0) return;
            CreateNode();
            OnValidate();
        }

        public void DeleteNode(ZoneNode nodeToDelete)
        {
            if (nodeToDelete == null) { return; }

            Undo.RecordObject(this, "Delete Zone Node");
            zoneNodes.Remove(nodeToDelete);
            CleanDanglingChildren(nodeToDelete);
            OnValidate();

            Undo.DestroyObjectImmediate(nodeToDelete);
        }

        private void CleanDanglingChildren(ZoneNode nodeToDelete)
        {
            foreach (ZoneNode zoneNode in zoneNodes)
            {
                zoneNode.RemoveChild(nodeToDelete.name);
            }
        }

        public void UpdateNodeID(string oldNodeID, string newNodeID)
        {
            foreach (ZoneNode zoneNode in zoneNodes.Where(zoneNode => zoneNode.GetChildren() != null))
            {
                zoneNode.UpdateChildNodeID(oldNodeID, newNodeID);
            }
            OnValidate();
        }

        public void ToggleRelation(ZoneNode parentNode, ZoneNode childNode)
        {
            if (IsRelated(parentNode, childNode))
            {
                parentNode.RemoveChild(childNode.name);
            }
            else
            {
                parentNode.AddChild(childNode.name);
            }
            OnValidate();
        }
#endif
        #endregion

        #region Interfaces
        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
#if UNITY_EDITOR
            if (AssetDatabase.GetAssetPath(this) == "") return;
            foreach (ZoneNode zoneNode in GetAllNodes())
            {
                zoneNode.SetZoneName(name);
                if (AssetDatabase.GetAssetPath(zoneNode) == "")
                {
                    AssetDatabase.AddObjectToAsset(zoneNode, this);
                }
            }
#endif
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            // Unused, required for interface
        }
        #endregion
    }

}

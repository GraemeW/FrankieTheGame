using Frankie.Core;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Frankie.ZoneManagement
{
    [CreateAssetMenu(fileName = "New Zone", menuName = "Zone/New Zone")]
    public class Zone : ScriptableObject, ISerializationCallbackReceiver, IAddressablesCache
    {
        // Tunables
        [Header("Editor Settings")]
        [SerializeField] Vector2 newNodeOffset = new Vector2(100f, 25f);
        [SerializeField] int nodeWidth = 430;
        [SerializeField] int nodeHeight = 150;
        [Header("Zone Properties")]
        [SerializeField] SceneReference sceneReference = null;
        [SerializeField] bool updateMap = false;
        [SerializeField] AudioClip zoneAudio = null;
        [SerializeField] bool isZoneAudioLooping = true;

        // State
        [HideInInspector] [SerializeField] List<ZoneNode> zoneNodes = new List<ZoneNode>();
        [HideInInspector] [SerializeField] Dictionary<string, ZoneNode> nodeLookup = new Dictionary<string, ZoneNode>();

        static AsyncOperationHandle<IList<Zone>> addressablesLoadHandle;
        static Dictionary<string, Zone> zoneLookupCache;
        static Dictionary<string, Zone> sceneReferenceCache;

        #region AddressablesCaching
        public static Zone GetFromName(string zoneName)
        {
            if (string.IsNullOrWhiteSpace(zoneName)) { return null; }

            BuildCacheIfEmpty();
            if (zoneName == null || !zoneLookupCache.ContainsKey(zoneName)) return null;
            return zoneLookupCache[zoneName];
        }

        public static Zone GetFromSceneReference(string sceneReference)
        {
            if (string.IsNullOrWhiteSpace(sceneReference)) { return null; }

            UnityEngine.Debug.Log($"Attempting to load zone from scene reference {sceneReference}");
            BuildCacheIfEmpty();

            if (sceneReference == null || !sceneReferenceCache.ContainsKey(sceneReference)) return null;
            return sceneReferenceCache[sceneReference];
        }

        public static void BuildCacheIfEmpty()
        {
            if (sceneReferenceCache == null)
            {
                // Debug:  UnityEngine.Debug.Log("Scene reference cache empty -- building");
                BuildZoneCache();
            }
        }

        private static void BuildZoneCache()
        {
            zoneLookupCache = new Dictionary<string, Zone>();
            sceneReferenceCache = new Dictionary<string, Zone>();
            UnityEngine.Debug.Log("Building zone cache");
            addressablesLoadHandle = Addressables.LoadAssetsAsync(typeof(Zone).Name, (Zone zone) =>
            {
                if (zoneLookupCache.ContainsKey(zone.name) || sceneReferenceCache.ContainsKey(zone.GetSceneReference().SceneName))
                {
                    Debug.LogError(string.Format("Looks like there's a duplicate ID for objects: {0} and {1}", zoneLookupCache[zone.name], zone));
                }

                zoneLookupCache[zone.name] = zone;
                sceneReferenceCache[zone.GetSceneReference().SceneName] = zone;
                //UnityEngine.Debug.Log($"Found zone:  {zone.name}");
            }
            );
            addressablesLoadHandle.WaitForCompletion();
        }

        public static void ReleaseCache()
        {
            Addressables.Release(addressablesLoadHandle);
        }
        #endregion

        #region UnityMethods
        private void Awake()
        {
#if UNITY_EDITOR
            //CreateRootNodeIfMissing();
            // Note:  Making in ZoneEditor due to some intricacies in how Unity calls Awake on ScriptableObjects in Editor vs. the serialization callback
            // For (unknown) reasons, the root node gets made and then killed by the time serialization occurs
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
        public SceneReference GetSceneReference()
        {
            return sceneReference;
        }

        public AudioClip GetZoneAudio()
        {
            return zoneAudio;
        }

        public bool IsZoneAudioLooping()
        {
            return isZoneAudioLooping;
        }

        public IEnumerable<ZoneNode> GetAllNodes()
        {
            return zoneNodes;
        }

        public ZoneNode GetRootNode()
        {
            return zoneNodes[0];
        }

        public ZoneNode GetNodeFromID(string name)
        {
            foreach (ZoneNode zoneNode in zoneNodes)
            {
                if (zoneNode.name == name)
                {
                    return zoneNode;
                }
            }
            return null;
        }

        public IEnumerable<ZoneNode> GetAllChildren(ZoneNode parentNode)
        {
            if (parentNode == null || parentNode.GetChildren() == null || parentNode.GetChildren().Count == 0) { yield break; }
            foreach (string childUniqueID in parentNode.GetChildren())
            {
                if (nodeLookup.ContainsKey(childUniqueID))
                {
                    yield return nodeLookup[childUniqueID];
                }
            }
        }

        public bool IsRelated(ZoneNode parentNode, ZoneNode childNode)
        {
            if (parentNode.GetChildren() == null) { return false; }

            if (parentNode.GetChildren().Contains(childNode.name))
            {
                return true;
            }
            return false;
        }

        public bool ShouldUpdateMap()
        {
            return updateMap;
        }
        #endregion


        #region EditorMethods
#if UNITY_EDITOR
        private ZoneNode CreateNode()
        {
            ZoneNode zoneNode = CreateInstance<ZoneNode>();
            Undo.RegisterCreatedObjectUndo(zoneNode, "Created Zone Node Object");
            zoneNode.Initialize(nodeWidth, nodeHeight);
            zoneNode.SetNodeID(System.Guid.NewGuid().ToString());
            zoneNode.SetZoneName(this.name);

            Undo.RecordObject(this, "Add Zone Node");
            zoneNodes.Add(zoneNode);

            return zoneNode;
        }

        public ZoneNode CreateChildNode(ZoneNode parentNode)
        {
            if (parentNode == null) { return null; }

            ZoneNode childNode = CreateNode();
            parentNode.AddChild(childNode.name);

            Vector2 offsetPosition = new Vector2(parentNode.GetRect().xMax + newNodeOffset.x,
                parentNode.GetRect().yMin + (parentNode.GetRect().height + newNodeOffset.y) * (parentNode.GetChildren().Count - 1)); // Offset position by 1 since child just added
            childNode.SetPosition(offsetPosition);

            OnValidate();

            return childNode;
        }

        public ZoneNode CreateRootNodeIfMissing()
        {
            if (zoneNodes.Count == 0)
            {
                ZoneNode rootNode = CreateNode();

                OnValidate();
                return rootNode;
            }

            return null;
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
            foreach (ZoneNode zoneNode in zoneNodes)
            {
                if (zoneNode.GetChildren() == null) { continue; }

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
            if (AssetDatabase.GetAssetPath(this) != "")
            {
                foreach (ZoneNode zoneNode in GetAllNodes())
                {
                    zoneNode.SetZoneName(this.name);
                    if (AssetDatabase.GetAssetPath(zoneNode) == "")
                    {
                        AssetDatabase.AddObjectToAsset(zoneNode, this);
                    }
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

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Frankie.ZoneManagement
{
    [CreateAssetMenu(fileName = "New Zone", menuName = "Zone/New Zone")]
    public class Zone : ScriptableObject, ISerializationCallbackReceiver
    {
        // Tunables
        [Header("Editor Settings")]
        [SerializeField] Vector2 newNodeOffset = new Vector2(100f, 25f);
        [SerializeField] int nodeWidth = 430;
        [SerializeField] int nodeHeight = 150;
        [Header("Zone Properties")]
        [SerializeField] SceneReference sceneReference = null;
        [SerializeField] AudioClip zoneAudio = null;
        [SerializeField] bool isZoneAudioLooping = true;

        // State
        [HideInInspector] [SerializeField] List<ZoneNode> zoneNodes = new List<ZoneNode>();
        [HideInInspector] [SerializeField] Dictionary<string, ZoneNode> nodeLookup = new Dictionary<string, ZoneNode>();
        static Dictionary<string, Zone> zoneLookupCache;
        static Dictionary<string, Zone> sceneReferenceCache;

        private void Awake()
        {
#if UNITY_EDITOR
            CreateRootNodeIfMissing();
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

        public static Zone GetFromName(string zoneName)
        {
            if (zoneLookupCache == null)
            {
                BuildCaches();
            }

            if (zoneName == null || !zoneLookupCache.ContainsKey(zoneName)) return null;
            return zoneLookupCache[zoneName];
        }

        public static Zone GetFromSceneReference(string sceneReference)
        {
            if (sceneReferenceCache == null)
            {
                BuildCaches();
            }

            if (sceneReference == null || !sceneReferenceCache.ContainsKey(sceneReference)) return null;
            return sceneReferenceCache[sceneReference];
        }

        private static void BuildCaches()
        {
            zoneLookupCache = new Dictionary<string, Zone>();
            sceneReferenceCache = new Dictionary<string, Zone>();
            Zone[] zoneList = Resources.LoadAll<Zone>("");
            foreach (Zone zone in zoneList)
            {
                if (zoneLookupCache.ContainsKey(zone.name) || sceneReferenceCache.ContainsKey(zone.GetSceneReference().SceneName))
                {
                    Debug.LogError(string.Format("Looks like there's a duplicate ID for objects: {0} and {1}", zoneLookupCache[zone.name], zone));
                    continue;
                }

                zoneLookupCache[zone.name] = zone;
                sceneReferenceCache[zone.GetSceneReference().SceneName] = zone;
            }
        }

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

        // Dialogue editing functionality
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

        private ZoneNode CreateRootNodeIfMissing()
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

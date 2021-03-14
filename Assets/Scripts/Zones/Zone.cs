using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Frankie.SceneManagement;

namespace Frankie.Zone
{
    [CreateAssetMenu(fileName = "New Zone", menuName = "Zone/New Zone")]
    public class Zone : ScriptableObject, ISerializationCallbackReceiver
    {
        // Tunables
        [Header("Editor Settings")]
        [SerializeField] Vector2 newNodeOffset = new Vector2(100f, 25f);
        [SerializeField] int nodeWidth = 430;
        [SerializeField] int nodeHeight = 150;
        [SerializeField] SceneReference sceneReference = null;

        // State
        [HideInInspector] [SerializeField] List<ZoneNode> zoneNodes = new List<ZoneNode>();
        [HideInInspector] [SerializeField] Dictionary<string, ZoneNode> nodeLookup = new Dictionary<string, ZoneNode>();
        static Dictionary<string, Zone> zoneLookupCache;

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
                zoneLookupCache = new Dictionary<string, Zone>();
                Zone[] zoneList = Resources.LoadAll<Zone>("");
                foreach (Zone zone in zoneList)
                {
                    if (zoneLookupCache.ContainsKey(zone.name))
                    {
                        Debug.LogError(string.Format("Looks like there's a duplicate ID for objects: {0} and {1}", zoneLookupCache[zone.name], zone));
                        continue;
                    }

                    zoneLookupCache[zone.name] = zone;
                }
            }

            if (zoneName == null || !zoneLookupCache.ContainsKey(zoneName)) return null;
            return zoneLookupCache[zoneName];
        }

        public SceneReference GetSceneReference()
        {
            return sceneReference;
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
            zoneNode.name = System.Guid.NewGuid().ToString();
            zoneNode.SetZoneName(sceneReference.SceneName);

            Undo.RecordObject(this, "Add Zone Node");
            zoneNodes.Add(zoneNode);

            return zoneNode;
        }

        public ZoneNode CreateChildNode(ZoneNode parentNode)
        {
            if (parentNode == null) { return null; }

            ZoneNode childNode = CreateNode();
            Vector2 offsetPosition = new Vector2(parentNode.GetRect().xMax + newNodeOffset.x,
                parentNode.GetRect().yMin + (parentNode.GetRect().height + newNodeOffset.y) * parentNode.GetChildren().Count);
            childNode.SetPosition(offsetPosition);

            parentNode.AddChild(childNode.name);
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

        public void OnAfterDeserialize()// Unused, required for interface
        {
        }

        public void OnBeforeSerialize() 
        {
#if UNITY_EDITOR
            if (AssetDatabase.GetAssetPath(this) != "")
            {
                foreach (ZoneNode zoneNode in GetAllNodes())
                {
                    zoneNode.SetZoneName(sceneReference.SceneName);
                    if (AssetDatabase.GetAssetPath(zoneNode) == "")
                    {
                        AssetDatabase.AddObjectToAsset(zoneNode, this);
                    }
                }
            }
#endif
        }
    }

}

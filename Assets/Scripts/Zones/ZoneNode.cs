using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System;

namespace Frankie.ZoneManagement
{
    [System.Serializable]
    public class ZoneNode : ScriptableObject
    {
        [Header("Zone Node Properties")]
        [SerializeField] List<string> children = new List<string>();
        [SerializeField] Zone linkedZone = null;
        [SerializeField] ZoneNode linkedZoneNode = null;
        [SerializeField] Rect rect = new Rect(30, 30, 430, 150);
        [HideInInspector] [SerializeField] string zoneName = null;
        [HideInInspector] [SerializeField] Rect draggingRect = new Rect(0, 0, 430, 45);

        public string GetZoneName()
        {
            return zoneName;
        }

        public string GetNodeID()
        {
            return name;
        }

        public bool HasSceneReference()
        {
            if (linkedZone == null || linkedZoneNode == null) { return false; }

            return (linkedZone.GetSceneReference() != null);
        }

        public ZoneNodePair GetZoneReferenceNodeReferencePair()
        {
            ZoneNodePair zoneNodePair = new ZoneNodePair(linkedZone, linkedZoneNode);
            return zoneNodePair;
        }

        public List<string> GetChildren()
        {
            if (children.Count == 0) { return null; }
            return children;
        }

        public void UpdateChildNodeID(string oldID, string newID)
        {
            if (!children.Contains(oldID)) { return; }

            children.Remove(oldID);
            children.Add(newID);
        }

        public Vector2 GetPosition()
        {
            return rect.position;
        }

        public Rect GetRect()
        {
            return rect;
        }

        public Rect GetDraggingRect()
        {
            return draggingRect;
        }


#if UNITY_EDITOR
        public void Initialize(int width, int height)
        {
            rect.width = width;
            rect.height = height;
            EditorUtility.SetDirty(this);
        }

        public void SetZoneName(string zoneName)
        {
            if (zoneName != this.zoneName)
            {
                Undo.RecordObject(this, "Update Zone");
                this.zoneName = zoneName;
                EditorUtility.SetDirty(this);
            }
        }

        public bool SetNodeID(string id)
        {
            if (id != name)
            {
                Undo.RecordObject(this, "Update Detail");
                name = id;
                EditorUtility.SetDirty(this);
                return true;
            }
            return false;
        }

        public void AddChild(string childID)
        {
            Undo.RecordObject(this, "Add Node Relation");
            children.Add(childID);
            EditorUtility.SetDirty(this);
        }

        public void RemoveChild(string childID)
        {
            Undo.RecordObject(this, "Remove Node Relation");
            children.Remove(childID);
            EditorUtility.SetDirty(this);
        }

        public void SetPosition(Vector2 position)
        {
            Undo.RecordObject(this, "Move Zone Node");
            rect.position = position;
            EditorUtility.SetDirty(this);
        }

        public void SetDraggingRect(Rect draggingRect)
        {
            if (draggingRect != this.draggingRect)
            {
                this.draggingRect = draggingRect;
                EditorUtility.SetDirty(this);
            }
        }
#endif
    }

}
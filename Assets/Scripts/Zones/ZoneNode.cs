using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Frankie.SceneManagement;
using System;

namespace Frankie.Zone
{
    [System.Serializable]
    public class ZoneNode : ScriptableObject
    {
        [Header("Zone Node Properties")]
        [SerializeField] List<string> children = new List<string>();
        [SerializeField] string detail = null;
        [SerializeField] string linkedZoneID = null;
        [SerializeField] string linkedNodeID = null;
        [SerializeField] Rect rect = new Rect(30, 30, 430, 150);
        [HideInInspector] [SerializeField] string zoneName = null;
        [HideInInspector] [SerializeField] Rect draggingRect = new Rect(0, 0, 430, 45);

        public string GetZoneName()
        {
            return zoneName;
        }

        public string GetDetail()
        {
            return detail;
        }

        public bool HasSceneReference()
        {
            // Level 1:  Variables set
            bool variableCheck = (linkedZoneID != null && !string.IsNullOrWhiteSpace(linkedNodeID));
            if (!variableCheck) { return variableCheck; }

            // Level 2:  Zone existence
            Zone linkedZone = Zone.GetFromName(linkedZoneID);
            bool zoneCheck = (linkedZone != null);
            if (!zoneCheck) { return zoneCheck; }

            // Level 3:  Scene existence
            bool sceneExistenceCheck = (linkedZone.GetSceneReference() != null);
            return (variableCheck && zoneCheck && sceneExistenceCheck);
        }

        public Tuple<string, string> GetSceneReferenceNodePair()
        {
            Tuple<string, string> zoneIDNodeIDPair = new Tuple<string, string>(linkedZoneID, linkedNodeID);
            return zoneIDNodeIDPair;
        }

        public List<string> GetChildren()
        {
            if (children.Count == 0) { return null; }
            return children;
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

        public void SetDetail(string detail)
        {
            if (detail != this.detail)
            {
                Undo.RecordObject(this, "Update Detail");
                this.detail = detail;
                EditorUtility.SetDirty(this);
            }
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
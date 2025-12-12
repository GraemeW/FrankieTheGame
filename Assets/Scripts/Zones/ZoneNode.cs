using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Frankie.Core;

namespace Frankie.ZoneManagement
{
    [System.Serializable]
    public class ZoneNode : ScriptableObject
    {
        // Tunables
        [Header("Zone Node Properties")]
        [SerializeField] private List<string> children = new();
        [SerializeField] private ZoneNode linkedZoneNode;
        [SerializeField] private Rect rect = new(30, 30, 430, 150);
        [HideInInspector][SerializeField] private string zoneName;
        [HideInInspector][SerializeField] private Rect draggingRect = new(0, 0, 430, 45);
        [Header("Additional Properties")]
        [SerializeField] private Condition condition;

        #region Getters
        public string GetZoneName() => zoneName;
        public Zone GetZone() => Zone.GetFromName(zoneName);
        public string GetNodeID() => name;
        public List<string> GetChildren() => children.Count == 0 ? null : children;
        public ZoneNode GetLinkedZoneNode() => linkedZoneNode;
        public bool HasLinkedSceneReference()
        {
            if (linkedZoneNode == null) { return false; }
            Zone linkedZone = linkedZoneNode.GetZone();
            return linkedZone != null && linkedZone.GetSceneReference().IsSet();
        }
        #endregion

        #region PublicMethods
        public void UpdateChildNodeID(string oldID, string newID)
        {
            if (!children.Contains(oldID)) { return; }

            children.Remove(oldID);
            children.Add(newID);
        }

        public bool CheckCondition(IEnumerable<IPredicateEvaluator> evaluators)
        {
            return condition.Check(evaluators);
        }
        #endregion

        #region ZoneEditorMethods
        public Vector2 GetPosition() => rect.position;
        public Rect GetRect() => rect;
        public Rect GetDraggingRect() => draggingRect;

#if UNITY_EDITOR
        public void Initialize(int width, int height)
        {
            rect.width = width;
            rect.height = height;
            EditorUtility.SetDirty(this);
        }

        public void SetZoneName(string setZoneName)
        {
            if (setZoneName == zoneName) return;
            Undo.RecordObject(this, "Update Zone");
            zoneName = setZoneName;
            EditorUtility.SetDirty(this);
        }

        public bool SetNodeID(string id)
        {
            if (id == name) return false;
            Undo.RecordObject(this, "Update Detail");
            name = id;
            EditorUtility.SetDirty(this);
            return true;
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

        public void SetDraggingRect(Rect setDraggingRect)
        {
            if (setDraggingRect == draggingRect) return;
            draggingRect = setDraggingRect;
            EditorUtility.SetDirty(this);
        }
#endif
        #endregion
    }
}

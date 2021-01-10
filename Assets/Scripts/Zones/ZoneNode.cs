using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Frankie.Zone
{
    [System.Serializable]
    public class ZoneNode : ScriptableObject
    {
        [Header("Dialogue Properties")]
        [SerializeField] List<string> children = new List<string>();
        [SerializeField] string detail = null;
        [SerializeField] string sceneReference = null;
        [SerializeField] Rect rect = new Rect(30, 30, 430, 150);
        [HideInInspector] [SerializeField] Rect draggingRect = new Rect(0, 0, 430, 45);

        public bool IsSceneReference()
        {
            return !string.IsNullOrWhiteSpace(sceneReference);
        }

        public string GetSceneReference()
        {
            return sceneReference;
        }

        public string GetDetail()
        {
            return detail;
        }

        public List<string> GetChildren()
        {
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

        public void SetSceneReference(string sceneReference)
        {
            if (sceneReference != this.sceneReference)
            {
                Undo.RecordObject(this, "Update Scene Reference");
                this.sceneReference = sceneReference;
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
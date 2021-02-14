using Frankie.Core;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Frankie.Dialogue
{
    [System.Serializable]
    public class DialogueNode : ScriptableObject
    {
        [Header("Dialogue Properties")]
        [SerializeField] SpeakerType speakerType = SpeakerType.aiSpeaker;
        [SerializeField] AIConversant aiSpeaker = null;
        [SerializeField] string speakerName = ""; // value gets over-written at runtime w/ value defined by aiConversant
        [SerializeField] string text = "";
        [SerializeField] List<string> children = new List<string>();
        [SerializeField] Rect rect = new Rect(30, 30, 400, 200);
        [HideInInspector] [SerializeField] Rect draggingRect = new Rect(0, 0, 400, 45);
        [Header("Additional Properties")]
        [SerializeField] string onEnterAction = "";
        [SerializeField] string onExitAction = "";
        [SerializeField] Condition condition = null;

        public AIConversant GetSpeaker()
        {
            return aiSpeaker;
        }

        public SpeakerType GetSpeakerType()
        {
            return speakerType;
        }

        public string GetSpeakerName()
        {
            return speakerName;
        }

        public string GetText()
        {
            return text;
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

        public string GetOnEnterAction()
        {
            return onEnterAction;
        }

        public string GetOnExitAction()
        {
            return onExitAction;
        }

        public bool SetSpeakerName(string speakerName)
        {
            if (speakerName != this.speakerName)
            {
#if UNITY_EDITOR
                Undo.RecordObject(this, "Update Dialogue Speaker Name");
#endif
                this.speakerName = speakerName;
#if UNITY_EDITOR
                EditorUtility.SetDirty(this);
#endif
                return true;
            }
            return false;
        }

        public bool CheckCondition(IEnumerable<IPredicateEvaluator> evaluators)
        {
            return condition.Check(evaluators);
        }


#if UNITY_EDITOR
        public void Initialize(int width, int height)
        {
            rect.width = width;
            rect.height = height;
            EditorUtility.SetDirty(this);
        }

        public void SetSpeakerType(SpeakerType speakerType)
        {
            if (speakerType != this.speakerType)
            {
                Undo.RecordObject(this, "Update Dialogue Speaker");
                this.speakerType = speakerType;
                EditorUtility.SetDirty(this);
            }
        }

        public void SetText(string text)
        {
            if (text != this.text)
            {
                Undo.RecordObject(this, "Update Dialogue");
                this.text = text;
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
            Undo.RecordObject(this, "Move Dialogue Node");
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
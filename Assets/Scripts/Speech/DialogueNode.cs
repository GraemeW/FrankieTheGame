using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Frankie.Core;
using Frankie.Stats;

namespace Frankie.Speech
{
    [System.Serializable]
    public class DialogueNode : ScriptableObject
    {
        [Header("Dialogue Properties")]
        [SerializeField] private SpeakerType speakerType = SpeakerType.AISpeaker;
        [SerializeField] private CharacterProperties characterProperties;
        [SerializeField] private string speakerName = ""; // value gets over-written at runtime w/ value defined by aiConversant
        [SerializeField] private string text = "";
        [SerializeField] private List<string> children = new();
        [SerializeField] private Rect rect = new(30, 30, 400, 200);
        [HideInInspector][SerializeField] private Rect draggingRect = new(0, 0, 400, 45);
        [Header("Additional Properties")]
        [SerializeField] private Condition condition;

        #region Getters
        public string GetCharacterName() => characterProperties != null ? characterProperties.name : GetSpeakerName();
        public CharacterProperties GetCharacterProperties() => characterProperties;
        public SpeakerType GetSpeakerType() => speakerType;
        public string GetSpeakerName() => speakerName;
        public string GetText() => text;
        public List<string> GetChildren() => children;
        public Vector2 GetPosition() => rect.position; 
        public Rect GetRect() => rect;
        public Rect GetDraggingRect() => draggingRect;
        #endregion
        
        #region Setters
        public bool SetSpeakerName(string setSpeakerName)
        {
            if (setSpeakerName == speakerName) return false;
            
#if UNITY_EDITOR
            Undo.RecordObject(this, "Update Dialogue Speaker Name");
#endif
            speakerName = setSpeakerName;
#if UNITY_EDITOR
            if (CharacterProperties.GetCharacterPropertiesFromName(setSpeakerName) != null) { characterProperties = CharacterProperties.GetCharacterPropertiesFromName(setSpeakerName); }
            EditorUtility.SetDirty(this);
#endif
            return true;
        }
        #endregion

        #region Checks
        public bool CheckCondition(IEnumerable<IPredicateEvaluator> evaluators)
        {
            return condition.Check(evaluators);
        }
        #endregion

        #region EditorMethods
#if UNITY_EDITOR
        public void Initialize(int width, int height)
        {
            rect.width = width;
            rect.height = height;
            EditorUtility.SetDirty(this);
        }

        public void SetSpeakerType(SpeakerType setSpeakerType)
        {
            if (setSpeakerType == speakerType) return;
            Undo.RecordObject(this, "Update Dialogue Speaker");
            speakerType = setSpeakerType;
            EditorUtility.SetDirty(this);
        }

        public void SetText(string setText)
        {
            if (setText == text) return;
            Undo.RecordObject(this, "Update Dialogue");
            text = setText;
            EditorUtility.SetDirty(this);
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

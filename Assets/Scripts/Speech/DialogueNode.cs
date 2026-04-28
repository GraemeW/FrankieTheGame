using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using UnityEditor;
using Frankie.Core;
using Frankie.Stats;
using Frankie.Utils;

namespace Frankie.Speech
{
    [System.Serializable]
    public class DialogueNode : ScriptableObject
    {
        [Header("Dialogue Properties")]
        [SerializeField, ReadOnly] private string dialogueName = "";
        [SerializeField] private SpeakerType speakerType = SpeakerType.AISpeaker;
        [SerializeField] private CharacterProperties characterProperties;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.Speech, false)] private LocalizedString localizedSpeakerName;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.Speech, false)] private LocalizedString localizedText;
        [SerializeField, ReadOnly] private int nodeDepth = 0;
        [SerializeField, ReadOnly] private int nodeBreadth = 0;
        [SerializeField] private List<string> children = new();
        [SerializeField] private Rect rect = new(30, 30, 400, 200);
        [HideInInspector][SerializeField] private Rect draggingRect = new(0, 0, 400, 45);
        [Header("Additional Properties")]
        [SerializeField] private Condition condition;
        
        // State
        private bool isSpeakerOverridden = false;
        private string overriddenSpeakerName = "";
        
#if UNITY_EDITOR
        // Edit State
        [HideInInspector][SerializeField] private string cachedSpeakerName = "";
        [HideInInspector][SerializeField] private string cachedText = "";
#endif

        #region Getters

        public bool HasValidCharacterProperties() => characterProperties != null && !string.IsNullOrWhiteSpace(characterProperties.GetCharacterNameID()); 
        public CharacterProperties GetCharacterProperties() => characterProperties;
        public SpeakerType GetSpeakerType() => speakerType;

        public string GetSpeakerName(bool useOverriden = true)
        {
            if (useOverriden && isSpeakerOverridden) { return overriddenSpeakerName; }
            return !localizedSpeakerName.IsEmpty ? localizedSpeakerName.GetLocalizedString() : "Speaker";
        }
        public string GetText() => !localizedText.IsEmpty ? localizedText.GetLocalizedString() : "Text";
        public int GetNodeDepth() => nodeDepth;
        public int GetNodeBreadth() => nodeBreadth;
        public List<string> GetChildren() => children;
        public int GetChildrenCount() => children?.Count ?? 0;
        public Vector2 GetPosition() => rect.position; 
        public Rect GetRect() => rect;
        public Rect GetDraggingRect() => draggingRect;

        private string GetSpeakerNameLocalizationKey() => $"{dialogueName}.{nodeDepth}.{nodeBreadth}.Speaker";
        private string GetTextLocalizationKey() => $"{dialogueName}.{nodeDepth}.{nodeBreadth}.Text";
        #endregion
        
        #region Setters
        public void OverrideSpeakerName(string speakerNameOverride)
        {
            isSpeakerOverridden = true;
            overriddenSpeakerName = speakerNameOverride;
        }

        public void OverrideSpeakerName()
        {
            if (characterProperties == null) { return; }
            isSpeakerOverridden = true;
            overriddenSpeakerName = characterProperties.GetCharacterNameID();
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
        
        public void SetDialogueName(string setDialogueName)
        {
            Undo.RecordObject(this, "Update Dialogue Name");            
            dialogueName = setDialogueName;
            EditorUtility.SetDirty(this);
        }

        public void SetSpeakerType(SpeakerType setSpeakerType)
        {
            if (setSpeakerType == speakerType) { return; }
            Undo.RecordObject(this, "Update Dialogue Speaker");
            speakerType = setSpeakerType;
            EditorUtility.SetDirty(this);
        }

        public bool SetSpeakerName(string setSpeakerName)
        {
            if (setSpeakerName == cachedSpeakerName) { return false; }
            
            Undo.RecordObject(this, "Update Dialogue Speaker Name");
            LocalizationTool.AddUpdateEnglishEntry(LocalizationTableType.Speech, GetSpeakerNameLocalizationKey(), setSpeakerName);
            if (localizedSpeakerName.IsEmpty) { LocalizationTool.SafelyUpdateReference(LocalizationTableType.Speech, localizedSpeakerName, GetSpeakerNameLocalizationKey()); }
            
            if (CharacterProperties.GetCharacterPropertiesFromName(setSpeakerName) != null) { characterProperties = CharacterProperties.GetCharacterPropertiesFromName(setSpeakerName); }
            cachedSpeakerName = setSpeakerName;
            EditorUtility.SetDirty(this);
            
            return true;
        }
        
        public void SetText(string setText)
        {
            if (setText == cachedText) { return; }
            
            Undo.RecordObject(this, "Update Dialogue");
            LocalizationTool.AddUpdateEnglishEntry(LocalizationTableType.Speech, GetTextLocalizationKey(), setText);
            if (localizedText.IsEmpty) { LocalizationTool.SafelyUpdateReference(LocalizationTableType.Speech, localizedText, GetTextLocalizationKey()); }
            
            cachedText = setText;
            EditorUtility.SetDirty(this);
        }

        public List<TableEntryReference> GetLocalizationEntries()
        {
            return new List<TableEntryReference>
            {
                localizedSpeakerName.TableEntryReference,
                localizedText.TableEntryReference
            };
        }

        public void DeleteLocalizationEntries()
        {
            Undo.RecordObject(this, "Delete Localization Entries");
            LocalizationTool.RemoveEntry(LocalizationTableType.Speech, GetSpeakerNameLocalizationKey());
            LocalizationTool.RemoveEntry(LocalizationTableType.Speech, GetTextLocalizationKey());
            localizedSpeakerName.SetReference("", "");
            localizedText.SetReference("", "");
            EditorUtility.SetDirty(this);
        }
        
        public void SetNodeDepthBreadth(int setNodeDepth, int setNodeBreadth)
        {
            Undo.RecordObject(this, "Update Dialogue Node Depth/Breadth");            
            nodeDepth = setNodeDepth;
            nodeBreadth = setNodeBreadth;
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

        public void SwapChild(string childID1, string childID2, bool recordUndoHistory = true)
        {
            if (recordUndoHistory) { Undo.RecordObject(this, "Swap Node Relation"); }
            children.Remove(childID1);
            children.Add(childID2);
            if (recordUndoHistory) { EditorUtility.SetDirty(this); }
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

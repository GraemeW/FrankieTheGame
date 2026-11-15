using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using LowDefMustard.Utils;
using LowDefMustard.Localization;
using Frankie.Stats;
using Frankie.Utils.Localization;

namespace Frankie.Speech
{
    [Serializable]
    public class DialogueNode : ScriptableObject
    {
        // Note:  Imported from a .dialoguegraph speech node (DialogueGraphImporter) ~ author in the graph

        [Header("Dialogue Properties")]
        [SerializeField, ReadOnly] private string nodeID = "";
        [SerializeField] private SpeakerType speakerType = SpeakerType.AISpeaker;
        [SerializeField] private CharacterProperties characterProperties;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.Speech, false)] private LocalizedString localizedSpeakerName;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.Speech, false)] private LocalizedString localizedText;
        [SerializeField, ReadOnly] private int nodeDepth = 0;
        [SerializeField, ReadOnly] private int nodeBreadth = 0;
        [SerializeField] private List<string> children = new();
        [SerializeField] private List<ScriptableObject> dialogueVariables = new();

        // State
        private bool isSpeakerOverridden = false;
        private string overriddenSpeakerName = "";

        #region Getters
        public string GetNodeID() => nodeID;
        public List<string> GetChildren() => children;
        public int GetNodeDepth() => nodeDepth;
        public int GetNodeBreadth() => nodeBreadth;
        
        public SpeakerType GetSpeakerType() => speakerType;
        public bool HasValidCharacterProperties() => characterProperties != null && !string.IsNullOrWhiteSpace(characterProperties.GetCharacterID());
        public CharacterProperties GetCharacterProperties() => characterProperties;
        public string GetSpeakerName(bool useOverriden = true)
        {
            if (useOverriden && isSpeakerOverridden) { return overriddenSpeakerName; }
            string speakerName = localizedSpeakerName.GetSafeLocalizedString();
            return !string.IsNullOrWhiteSpace(speakerName) ? speakerName : "Speaker";
        }

        public IReadOnlyList<ScriptableObject> GetDialogueVariables() => dialogueVariables;
        public string GetText(DialogueVariableContext context) => DialogueVariableFormatter.Format(GetRawText(), dialogueVariables, context);

        private string GetRawText() // Never public, external consumers should only ever get text with its variables & context
        {
            string text = localizedText.GetSafeLocalizedString();
            return !string.IsNullOrWhiteSpace(text) ? text : "Text";
        }
        #endregion
        
        #region Localization
        public LocalizationTableType localizationTableType { get; } = LocalizationTableType.Speech;
        public List<TableEntryReference> GetLocalizationEntries()
        {
            return new List<TableEntryReference>
            {
                localizedSpeakerName.TableEntryReference,
                localizedText.TableEntryReference
            };
        }
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
            overriddenSpeakerName = CharacterProperties.GetCharacterDisplayName(characterProperties);
        }
        #endregion

#if UNITY_EDITOR
        #region EditorMethods
        internal void Initialize(string setNodeID, SpeakerType setSpeakerType, CharacterProperties setCharacterProperties, int setNodeDepth, int setNodeBreadth, LocalizedString setLocalizedSpeakerName, LocalizedString setLocalizedText, IEnumerable<string> setChildren, IEnumerable<ScriptableObject> setDialogueVariables)
        {
            nodeID = setNodeID;
            speakerType = setSpeakerType;
            characterProperties = setCharacterProperties;
            nodeDepth = setNodeDepth;
            nodeBreadth = setNodeBreadth;
            localizedSpeakerName = setLocalizedSpeakerName;
            localizedText = setLocalizedText;
            children = new List<string>(setChildren);
            dialogueVariables = setDialogueVariables != null ? new List<ScriptableObject>(setDialogueVariables) : new List<ScriptableObject>();
        }
        #endregion
#endif
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.GraphToolkit.Editor;
using Frankie.Stats;

namespace Frankie.Speech.Editor
{
    [Serializable]
    [UseWithGraph(typeof(DialogueGraph))]
    [Node("Dialogue", "", "Line")]
    internal sealed class SpeechGraphNode : Node
    {
        // Const Tunables
        public const string inPort = "In";
        public const string outPort = "Out";
        public const string speakerTypeOption = "SpeakerType";
        public const string characterOption = "Character";
        public const string speakerNameOption = "SpeakerName";
        public const string textOption = "Text";
        public const string depthOption = "Depth";
        public const string breadthOption = "Breadth";
        public const int unassignedPosition = -1;
        private const string _variablePortPrefix = "Variable";

        private const string _characterTooltip = "For AI SpeakerType - speaker shown is this character's display name (overrides Speaker)";
        private const string _speakerNameTooltip = "Fallback speaker name - @ SpeakerType: Player, follows party leader name; @ : AI, follows character name (if set); otherwise: this";
        private const string _variableTooltip = "Dialogue variable (must implement IDialogueVariable)";
        private const string _positionTooltip = "Localization key position (dialogue.depth.breadth) - assigned once the line is reachable from Entry";

        // State
        [SerializeField] private List<ScriptableObject> stashedVariables = new(); // Port values outlive a placeholder briefly removed from the text
        [NonSerialized] private bool hasAppliedColour; // Unset after a domain reload / window open ~ colour then applies directly
        [NonSerialized] private Color appliedColour;

        #region UnityMethods
        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            context.AddInputPort<DialogueFlow>(inPort).WithCapacity(PortCapacity.Multi).WithConnectorUI(PortConnectorUI.Arrowhead).Build();
            context.AddOutputPort<DialogueFlow>(outPort).WithCapacity(PortCapacity.Multi).WithConnectorUI(PortConnectorUI.Arrowhead).Build();
            
            int variableCount = DialogueVariableFormatter.GetContiguousCount(GetText()); // One field per placeholder ~ re-run by Graph Toolkit whenever the text option changes
            for (int index = 0; index < variableCount; index++)
            {
                var variablePort = context.AddInputPort<ScriptableObject>(GetVariablePortName(index)).WithDisplayName($"{{{index}}}").WithTooltip(_variableTooltip).WithCapacity(PortCapacity.None);
                if (index < stashedVariables.Count && stashedVariables[index] != null) { variablePort = variablePort.WithDefaultValue(stashedVariables[index]); }
                variablePort.Build();
            }
        }

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            context.AddOption<SpeakerType>(speakerTypeOption).WithDisplayName("Speaker Type").WithDefaultValue(SpeakerType.AISpeaker).Build();
            context.AddOption<CharacterProperties>(characterOption).WithDisplayName("Character").WithTooltip(_characterTooltip).Build();
            context.AddOption<string>(speakerNameOption).WithDisplayName("Speaker").WithTooltip(_speakerNameTooltip).Delayed().Build();
            context.AddOption<string>(textOption).WithDisplayName("Text").AsTextArea(3, 10).Delayed().Build();
            context.AddOption<int>(depthOption).WithDefaultValue(unassignedPosition).WithTooltip(_positionTooltip).ShowInInspectorOnly().Build();
            context.AddOption<int>(breadthOption).WithDefaultValue(unassignedPosition).WithTooltip(_positionTooltip).ShowInInspectorOnly().Build();
        }
        #endregion
        
        #region Getters
        public SpeakerType GetSpeakerType() => this.GetOptionValue<SpeakerType>(speakerTypeOption);
        public CharacterProperties GetCharacter() => this.GetOptionValue<CharacterProperties>(characterOption);
        public string GetSpeakerName() => this.GetOptionValue<string>(speakerNameOption) ?? "";
        public string GetText() => this.GetOptionValue<string>(textOption) ?? "";
        public int GetDepth() => this.HasOptionsDefined(depthOption) ? this.GetOptionValue<int>(depthOption) : unassignedPosition;
        public int GetBreadth() => this.HasOptionsDefined(breadthOption) ? this.GetOptionValue<int>(breadthOption) : unassignedPosition;
        public bool IsPlaced() => GetDepth() != unassignedPosition && GetBreadth() != unassignedPosition;

        public static string GetVariablePortName(int index) => $"{_variablePortPrefix}{index}";
        public int GetVariableCount() => DialogueVariableFormatter.GetContiguousCount(GetText());
        public ScriptableObject GetVariable(int index)
        {
            IPort variablePort = GetInputPortByName(GetVariablePortName(index));
            return variablePort != null && variablePort.TryGetValue(out ScriptableObject variable) ? variable : null;
        }

        public List<ScriptableObject> GetVariables()
        {
            var variables = new List<ScriptableObject>();
            for (int index = 0; index < GetVariableCount(); index++) { variables.Add(GetVariable(index)); }
            return variables;
        }
        #endregion
        
        #region SettersUtility
        public void SetPosition(int depth, int breadth)
        {
            GetNodeOptionByName(depthOption).TrySetValue(depth);
            GetNodeOptionByName(breadthOption).TrySetValue(breadth);
        }
        
        public void StashVariables()
        {
            // Called per graph change:  old ports aren't readable in OnDefinePorts ~ values are kept here for a re-added placeholder (e.g. undo, or re-add variable in text)
            int variableCount = GetVariableCount();
            while (stashedVariables.Count < variableCount) { stashedVariables.Add(null); }
            for (int index = 0; index < variableCount; index++)
            {
                if (GetInputPortByName(GetVariablePortName(index)) == null) { continue; }
                stashedVariables[index] = GetVariable(index);
            }
        }
        #endregion

        #region Visuals
        public bool IsRecolour(Color speakerColour) => hasAppliedColour && appliedColour != speakerColour;

        public void ApplyColour(Color speakerColour)
        {
            DefaultColor = speakerColour;
            appliedColour = speakerColour;
            hasAppliedColour = true;
        }

        public void RefreshVisuals(Color speakerColour, bool applyColour)
        {
            if (!this.HasOptionsDefined(speakerNameOption)) { return; }
            string speakerName = GetSpeakerName();
            if (applyColour) { ApplyColour(speakerColour); }
            Title = string.IsNullOrWhiteSpace(speakerName) ? "Line" : speakerName;
            Subtitle = GetDepth() == unassignedPosition ? "" : $"{GetDepth()}.{GetBreadth()}";
            Tooltip = $"ID {ID}\nKey {Graph?.Name}.{GetDepth()}.{GetBreadth()}";
        }
        #endregion
    }
}

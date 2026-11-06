using System;
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

        private const string _characterTooltip = "For AI SpeakerType - speaker shown is this character's display name (overrides Speaker)";
        private const string _speakerNameTooltip = "Fallback speaker name - @ SpeakerType: Player, follows party leader name; @ : AI, follows character name (if set); otherwise: this";
        private const string _positionTooltip = "Localization key position (dialogue.depth.breadth) - assigned once the line is reachable from Entry";

        // State
        [NonSerialized] private bool hasAppliedColour; // Unset after a domain reload / window open ~ colour then applies directly
        [NonSerialized] private Color appliedColour;

        #region UnityMethods
        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            context.AddInputPort<DialogueFlow>(inPort).WithCapacity(PortCapacity.Multi).WithConnectorUI(PortConnectorUI.Arrowhead).Build();
            context.AddOutputPort<DialogueFlow>(outPort).WithCapacity(PortCapacity.Multi).WithConnectorUI(PortConnectorUI.Arrowhead).Build();
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

        public void SetPosition(int depth, int breadth)
        {
            GetNodeOptionByName(depthOption).TrySetValue(depth);
            GetNodeOptionByName(breadthOption).TrySetValue(breadth);
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

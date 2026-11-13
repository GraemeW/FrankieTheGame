using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Unity.GraphToolkit.Editor;
using Frankie.Stats;

namespace Frankie.Speech.Editor
{
    [Graph(assetExtension, GraphOptions.Default)]
    [Serializable]
    internal sealed class DialogueGraph : Graph
    {
        // Const Tunables
        public const string assetExtension = "dialoguegraph";

        // State
        [NonSerialized] private Dictionary<string, string> localizationBases; // Last-synced localized values per line field

        // Menu Item
        [MenuItem("Assets/Create/Dialogue/New Dialogue Graph", false, 11)]
        private static void CreateAssetFile() => GraphDatabase.PromptInProjectBrowserToCreateNewAsset<DialogueGraph>("New Dialogue");

        #region UnityMethods
        public override void OnEnable()
        {
            EditorApplication.delayCall += () => DialogueGraphWindowSkin.Apply(this); // Re-skin windows reopened by a domain reload
        }

        public override void OnGraphChanged(GraphLogger graphLogger)
        {
            // Note:  Graph Toolkit locks the graph during this call (structural updates thus scheduled til next frame)
            
            bool fromUndoRedo = DialogueGraphUndoTracker.ConsumeUndoRedo();
            RefreshVisuals(fromUndoRedo);
            DialogueGraphValidator.Validate(this, graphLogger);
            DialogueGraphLocalizationSync.HandleGraphChanged(this, fromUndoRedo, graphLogger);
            EditorApplication.delayCall += () => DialogueGraphWindowSkin.Apply(this);

            // Structural updates skipped after undo/redo ~ would otherwise clear the redo stack
            if (!fromUndoRedo) { ScheduleStructuralFixes(); }
        }
        #endregion

        #region Localization
        internal Dictionary<string, string> GetLocalizationBases() => localizationBases ??= new Dictionary<string, string>();
        #endregion

        #region Visuals
        private void RefreshVisuals(bool fromUndoRedo)
        {
            // Speaker colour changes are deferred:  a DefaultColor set inside OnGraphChanged updates the model but not the node's view
            var aiCharacters = new List<CharacterProperties>();
            var recolours = new List<(SpeechGraphNode line, Color colour)>();
            foreach (INode node in GetNodes())
            {
                switch (node)
                {
                    case EntryGraphNode entryNode:
                        entryNode.RefreshVisuals();
                        break;
                    case ConditionGraphNode conditionNode:
                        conditionNode.RefreshVisuals();
                        break;
                    case SpeechGraphNode speechNode:
                        speechNode.StashVariables();
                        Color colour = ResolveSpeakerColour(speechNode, aiCharacters);
                        bool shouldRecolour = !fromUndoRedo && speechNode.IsRecolour(colour);
                        if (shouldRecolour) { recolours.Add((speechNode, colour)); }
                        speechNode.RefreshVisuals(colour, applyColour: !shouldRecolour);
                        break;
                }
            }

            // Set up deferred recolours
            if (recolours.Count == 0) { return; }
            int editGroup = Undo.GetCurrentGroup();
            EditorApplication.delayCall += () => RecordGraphChange("Recolour Dialogue Lines", editGroup, collapse: true, () =>
            {
                foreach ((SpeechGraphNode line, Color colour) in recolours) { line.ApplyColour(colour); }
            });
        }

        private static Color ResolveSpeakerColour(SpeechGraphNode speechNode, List<CharacterProperties> aiCharacters)
        {
            switch (speechNode.GetSpeakerType())
            {
                case SpeakerType.PlayerSpeaker:
                    return DialogueGraphPalette.player;
                case SpeakerType.AISpeaker:
                    CharacterProperties character = speechNode.GetCharacter();
                    if (character == null) { return DialogueGraphPalette.aiSpeakers[0]; }
                    if (!aiCharacters.Contains(character)) { aiCharacters.Add(character); }
                    return DialogueGraphPalette.aiSpeakers[aiCharacters.IndexOf(character) % DialogueGraphPalette.aiSpeakers.Length];
                case SpeakerType.NarratorDirection:
                default:
                    return DialogueGraphPalette.narrator;
            }
        }
        #endregion

        #region StructuralFixes
        private void ScheduleStructuralFixes()
        {
            int editGroup = Undo.GetCurrentGroup();
            List<EntryGraphNode> entryNodes = GetNodes().OfType<EntryGraphNode>().ToList();
            if (entryNodes.Count != 1) { EditorApplication.delayCall += () => FixEntryNodes(editGroup); }

            // Constants and variables can still be created from the node library (no public API to hide them)
            if (GetNodes().Any(node => node is IConstantNode or IVariableNode) || VariableCount > 0) { EditorApplication.delayCall += () => RemoveUnsupported(editGroup); }
        }

        private void FixEntryNodes(int editGroup)
        {
            List<EntryGraphNode> entryNodes = GetNodes().OfType<EntryGraphNode>().ToList();
            if (entryNodes.Count == 1) { return; }
            RecordGraphChange("Fix Dialogue Entry", editGroup, collapse: true, () =>
            {
                if (entryNodes.Count == 0) { AddNode(new EntryGraphNode()); return; }
                EntryGraphNode keeper = entryNodes.FirstOrDefault(entryNode => ((INode)entryNode).IsConnected) ?? entryNodes[0];
                foreach (EntryGraphNode extra in entryNodes.Where(entryNode => entryNode != keeper)) { RemoveNode(extra); }
            });
        }

        private void RemoveUnsupported(int editGroup)
        {
            List<INode> unsupportedNodes = GetNodes().Where(node => node is IConstantNode or IVariableNode).ToList();
            if (unsupportedNodes.Count == 0 && VariableCount == 0) { return; }
            RecordGraphChange("Remove Unsupported Dialogue Nodes", editGroup, collapse: true, () =>
            {
                foreach (INode node in unsupportedNodes) { RemoveNode(node); }
                foreach (IVariable variable in GetVariables().ToList()) { RemoveVariable(variable, true); }
            });
        }

        internal void RecordGraphChange(string actionName, int editGroup, bool collapse, Action change)
        {
            // Collapse folds into the edit that triggered it -> one undo reverts both
            UndoBeginRecordGraph(actionName);
            try { change(); }
            finally
            {
                UndoEndRecordGraph();
                if (collapse) { Undo.CollapseUndoOperations(editGroup); }
            }
        }
        #endregion
    }
}

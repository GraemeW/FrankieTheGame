using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Localization.Tables;
using LowDefMustard.Utils;
using Frankie.Utils.Localization;

namespace Frankie.Speech
{
    public class Dialogue : ScriptableObject, ILocalizable
    {
        // Note:
        //  - Imported from a .dialoguegraph (DialogueGraphImporter) ~ author in the graphto auto-generate
        //  - Nodes are this asset's sub-assets

        // State
        [HideInInspector][SerializeField] private List<DialogueNode> dialogueNodes = new();
        [HideInInspector][SerializeField] private List<DialogueConditionNode> conditionNodes = new();
        [HideInInspector][SerializeField] private List<string> entryNodeIDs = new();

        #region StaticMethods
        private static bool IsPlayerNameOverrideable(string playerName) => !string.IsNullOrWhiteSpace(playerName);
        private static bool IsSpeakerNameOverrideable(SpeakerType speakerType, DialogueNode dialogueNode) => speakerType == SpeakerType.AISpeaker && dialogueNode.HasValidCharacterProperties();
        #endregion

        #region GettersSetters
        public IEnumerable<DialogueNode> GetAllDialogueNodes() => dialogueNodes;
        public IEnumerable<Predicate> GetAllPredicates() => conditionNodes.Where(conditionNode => conditionNode != null).SelectMany(conditionNode => conditionNode.GetPredicates()).Distinct();
        public DialogueNode GetDialogueNodeFromID(string nodeID) => dialogueNodes.FirstOrDefault(dialogueNode => dialogueNode != null && dialogueNode.GetNodeID() == nodeID);
        private DialogueConditionNode GetConditionNodeFromID(string nodeID) => conditionNodes.FirstOrDefault(conditionNode => conditionNode != null && conditionNode.GetNodeID() == nodeID);

        public void OverrideSpeakerNames(string playerName)
        {
            foreach (DialogueNode dialogueNode in dialogueNodes)
            {
                SpeakerType speakerType = dialogueNode.GetSpeakerType();
                if (speakerType == SpeakerType.PlayerSpeaker && IsPlayerNameOverrideable(playerName)) { dialogueNode.OverrideSpeakerName(playerName); }
                else if (speakerType == SpeakerType.AISpeaker && IsSpeakerNameOverrideable(speakerType, dialogueNode)) { dialogueNode.OverrideSpeakerName(); }
            }
        }
        #endregion

        #region Traversal
        public List<DialogueNode> ResolveEntry(IEnumerable<IPredicateEvaluator> evaluators) => ResolveTargets(entryNodeIDs, evaluators);
        public List<DialogueNode> ResolveChildren(DialogueNode dialogueNode, IEnumerable<IPredicateEvaluator> evaluators) => dialogueNode != null ? ResolveTargets(dialogueNode.GetChildren(), evaluators) : new List<DialogueNode>();

        private List<DialogueNode> ResolveTargets(IEnumerable<string> nodeIDs, IEnumerable<IPredicateEvaluator> evaluators)
        {
            var resolvedNodes = new List<DialogueNode>();
            AppendResolvedTargets(nodeIDs, evaluators.ToList(), resolvedNodes, new HashSet<DialogueConditionNode>());
            return resolvedNodes;
        }

        private void AppendResolvedTargets(IEnumerable<string> nodeIDs, List<IPredicateEvaluator> evaluators, List<DialogueNode> resolvedNodes, HashSet<DialogueConditionNode> visitedConditionNodes)
        {
            // Condition nodes are transparent -> a passing one contributes its own targets in place
            if (nodeIDs == null) { return; }
            foreach (string nodeID in nodeIDs)
            {
                DialogueNode dialogueNode = GetDialogueNodeFromID(nodeID);
                if (dialogueNode != null)
                {
                    if (!resolvedNodes.Contains(dialogueNode)) { resolvedNodes.Add(dialogueNode); }
                    continue;
                }

                DialogueConditionNode conditionNode = GetConditionNodeFromID(nodeID);
                if (conditionNode == null || !visitedConditionNodes.Add(conditionNode)) { continue; }
                if (!conditionNode.CheckCondition(evaluators)) { continue; }
                AppendResolvedTargets(conditionNode.GetChildren(), evaluators, resolvedNodes, visitedConditionNodes);
            }
        }
        #endregion

        #region Localization
        public LocalizationTableType localizationTableType { get; } = LocalizationTableType.Speech;
        public List<TableEntryReference> GetLocalizationEntries()
        {
            var entries = new List<TableEntryReference>();
            foreach (DialogueNode dialogueNode in dialogueNodes.Where(dialogueNode => dialogueNode != null))
            {
                entries.AddRange(dialogueNode.GetLocalizationEntries());
            }
            return entries;
        }
        #endregion

#if UNITY_EDITOR
        #region EditorMethods
        internal void Initialize(IEnumerable<DialogueNode> setDialogueNodes, IEnumerable<DialogueConditionNode> setConditionNodes, IEnumerable<string> setEntryNodeIDs)
        {
            dialogueNodes = new List<DialogueNode>(setDialogueNodes);
            conditionNodes = new List<DialogueConditionNode>(setConditionNodes);
            entryNodeIDs = new List<string>(setEntryNodeIDs);
        }
        #endregion
#endif
    }
}

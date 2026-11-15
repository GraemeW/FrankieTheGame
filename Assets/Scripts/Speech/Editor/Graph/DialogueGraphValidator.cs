using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.GraphToolkit.Editor;

namespace Frankie.Speech.Editor
{
    internal static class DialogueGraphValidator
    {
        // Static validator to report authoring problems via Graph Toolkit's logger
        
        #region PublicEntryPoint
        public static void Validate(DialogueGraph graph, GraphLogger graphLogger)
        {
            List<EntryGraphNode> entryNodes = graph.GetNodes().OfType<EntryGraphNode>().ToList();
            if (entryNodes.Count > 1) { graphLogger.LogWarning($"A dialogue has one Entry node: removing {entryNodes.Count - 1} extra", graph); }

            HashSet<INode> reachableNodes = GetReachableNodes(entryNodes);
            if (entryNodes.Count > 0 && !reachableNodes.Any(node => node is SpeechGraphNode))
            {
                graphLogger.LogWarning("Entry doesn't lead to any line, so dialogue exits immediately", entryNodes[0]);
            }

            foreach (INode node in graph.GetNodes())
            {
                if (node is (SpeechGraphNode or ConditionGraphNode) && entryNodes.Count > 0 && !reachableNodes.Contains(node))
                {
                    graphLogger.LogWarning("Not reachable from Entry, so it never plays", node);
                }

                switch (node)
                {
                    case SpeechGraphNode speechNode:
                        ValidateSpeechNode(speechNode, graphLogger);
                        break;
                    case ConditionGraphNode conditionNode:
                        ValidateConditionNode(conditionNode, graphLogger);
                        break;
                }
            }

            if (HasConditionOnlyLoop(graph)) { graphLogger.LogWarning("Condition nodes loop back on each other without a line in between", graph); }
        }
        #endregion

        #region PrivateMethods
        private static IEnumerable<INode> GetTargets(INode node)
        {
            var connectedPorts = new List<IPort>();
            foreach (IPort outputPort in node.GetOutputPorts())
            {
                connectedPorts.Clear();
                outputPort.GetConnectedPorts(connectedPorts);
                foreach (IPort connectedPort in connectedPorts) { yield return connectedPort.GetNode(); }
            }
        }
        
        private static HashSet<INode> GetReachableNodes(IEnumerable<EntryGraphNode> entryNodes)
        {
            var reachableNodes = new HashSet<INode>();
            var pendingNodes = new Queue<INode>(entryNodes);
            while (pendingNodes.Count > 0)
            {
                foreach (INode target in GetTargets(pendingNodes.Dequeue()))
                {
                    if (reachableNodes.Add(target)) { pendingNodes.Enqueue(target); }
                }
            }
            return reachableNodes;
        }
        
        private static bool HasConditionOnlyLoop(DialogueGraph graph)
        {
            // Depth-first search over condition -> condition wires only
            var visiting = new HashSet<INode>();
            var finished = new HashSet<INode>();
            return graph.GetNodes().OfType<ConditionGraphNode>().Any(conditionNode => HasLoopFrom(conditionNode, visiting, finished));
        }

        private static bool HasLoopFrom(INode node, HashSet<INode> visiting, HashSet<INode> finished)
        {
            if (finished.Contains(node)) { return false; }
            if (!visiting.Add(node)) { return true; }
            bool hasLoop = GetTargets(node).OfType<ConditionGraphNode>().Any(target => HasLoopFrom(target, visiting, finished));
            visiting.Remove(node);
            finished.Add(node);
            return hasLoop;
        }

        private static void ValidateSpeechNode(SpeechGraphNode speechNode, GraphLogger graphLogger)
        {
            if (!speechNode.HasOptionsDefined(SpeechGraphNode.textOption)) { return; }
            if (string.IsNullOrWhiteSpace(speechNode.GetText())) { graphLogger.LogWarning("Line has no text", speechNode); }

            bool needsSpeaker = speechNode.GetSpeakerType() == SpeakerType.AISpeaker && speechNode.GetCharacter() == null;
            if (needsSpeaker && string.IsNullOrWhiteSpace(speechNode.GetSpeakerName())) { graphLogger.LogWarning("Line has neither a Character nor a Speaker name", speechNode); }
            ValidateDialogueVariables(speechNode, graphLogger);
        }

        private static void ValidateDialogueVariables(SpeechGraphNode speechNode, GraphLogger graphLogger)
        {
            int variableCount = speechNode.GetVariableCount();
            List<int> skippedIndices = DialogueVariableFormatter.GetPlaceholderIndices(speechNode.GetText()).Where(index => index >= variableCount).ToList();
            if (skippedIndices.Count > 0)
            {
                string skipped = string.Join(", ", skippedIndices.Select(index => $"{{{index}}}"));
                graphLogger.LogWarning($"{skipped} shows as blank: placeholders must run {{0}}, {{1}}, ... without gaps", speechNode);
            }

            for (int index = 0; index < variableCount; index++)
            {
                ScriptableObject variable = speechNode.GetVariable(index);
                if (variable == null) { graphLogger.LogWarning($"{{{index}}} has no dialogue variable, so it shows as blank", speechNode); }
                else if (variable is not IDialogueVariable) { graphLogger.LogWarning($"{{{index}}} '{variable.name}' isn't a dialogue variable, so it shows as blank", speechNode); }
            }
        }

        private static void ValidateConditionNode(ConditionGraphNode conditionNode, GraphLogger graphLogger)
        {
            List<PredicateGraphBlock> blocks = conditionNode.GetPredicateBlocks().ToList();
            if (blocks.Count == 0) { graphLogger.LogWarning("Condition has no predicates, so it always passes", conditionNode); }
            if (blocks.Any(block => block.HasOptionsDefined(PredicateGraphBlock.predicateOption) && block.GetPredicate() == null))
            {
                graphLogger.LogWarning("Condition has an empty predicate block, which always passes", conditionNode);
            }
        }
        #endregion
    }
}

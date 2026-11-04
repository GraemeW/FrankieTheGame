using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Localization;
using UnityEditor.AssetImporters;
using Unity.GraphToolkit.Editor;
using Frankie.Utils.Localization;

namespace Frankie.Speech.Editor
{
    [ScriptedImporter(1, DialogueGraph.assetExtension)]
    internal sealed class DialogueGraphImporter : ScriptedImporter
    {
        // Emits the runtime Dialogue (main object) + one sub-asset per speech / condition node
        //  - sub-asset identifier = Graph Toolkit node ID -> fileIDs (e.g. DialogueTrigger.triggerNode) survive edits + reimports
        //  - Entry is authoring-only, its targets become the dialogue's entry node IDs
        // Note:    Localization tables are synced from the graph window, not here
        // Note2:   Plz bump the version on any change to the emitted data

        // Const Tunables
        private const string _dialogueIdentifier = "Dialogue";

        #region UnityMethods
        public override void OnImportAsset(AssetImportContext context)
        {
            var graph = GraphDatabase.LoadGraphForImporter<DialogueGraph>(context.assetPath);
            if (graph == null)
            {
                context.LogImportError($"Dialogue graph '{context.assetPath}' could not be loaded, so no dialogue was imported");
                return;
            }

            string dialogueName = Path.GetFileNameWithoutExtension(context.assetPath);
            var dialogueNodes = new List<DialogueNode>();
            var conditionNodes = new List<DialogueConditionNode>();
            foreach (INode node in graph.GetNodes())
            {
                switch (node)
                {
                    case SpeechGraphNode speechNode:
                        dialogueNodes.Add(BuildDialogueNode(speechNode, dialogueName));
                        break;
                    case ConditionGraphNode conditionGraphNode:
                        conditionNodes.Add(BuildConditionNode(conditionGraphNode));
                        break;
                }
            }

            var dialogue = ScriptableObject.CreateInstance<Dialogue>();
            dialogue.name = dialogueName;
            dialogue.InitializeGraph(dialogueNodes, conditionNodes, GetEntryNodeIDs(graph));

            context.AddObjectToAsset(_dialogueIdentifier, dialogue);
            foreach (ScriptableObject subAsset in dialogueNodes.Cast<ScriptableObject>().Concat(conditionNodes)) { context.AddObjectToAsset(subAsset.name, subAsset); }
            context.SetMainObject(dialogue);
        }
        #endregion

        #region PrivateMethods
        private static DialogueNode BuildDialogueNode(SpeechGraphNode speechNode, string dialogueName)
        {
            int depth = speechNode.GetDepth();
            int breadth = speechNode.GetBreadth();

            var dialogueNode = ScriptableObject.CreateInstance<DialogueNode>();
            dialogueNode.name = GetNodeID(speechNode);
            dialogueNode.InitializeFromGraph(dialogueName, speechNode.GetSpeakerType(), speechNode.GetCharacter(), depth, breadth,
                MakeLocalizedString(dialogueName, depth, breadth, "Speaker"), MakeLocalizedString(dialogueName, depth, breadth, "Text"), GetTargetIDs(speechNode));
            return dialogueNode;
        }

        private static DialogueConditionNode BuildConditionNode(ConditionGraphNode conditionGraphNode)
        {
            var conditionNode = ScriptableObject.CreateInstance<DialogueConditionNode>();
            conditionNode.name = GetNodeID(conditionGraphNode);
            conditionNode.Initialize(conditionGraphNode.BuildCondition(), GetTargetIDs(conditionGraphNode));
            return conditionNode;
        }

        private static LocalizedString MakeLocalizedString(string dialogueName, int depth, int breadth, string field)
        {
            // Unassigned position (line never connected) ~ no key, the line has no table entry yet
            if (depth == SpeechGraphNode.unassignedPosition || breadth == SpeechGraphNode.unassignedPosition) { return new LocalizedString(); }
            return LocalizationTool.MakeLocalizedString(LocalizationTableType.Speech, $"{dialogueName}.{depth}.{breadth}.{field}");
        }

        private static IEnumerable<string> GetEntryNodeIDs(DialogueGraph graph)
        {
            // Matches DialogueGraph's entry fix: extras are removed in favour of the first connected entry
            List<EntryGraphNode> entryNodes = graph.GetNodes().OfType<EntryGraphNode>().ToList();
            EntryGraphNode entryNode = entryNodes.FirstOrDefault(node => ((INode)node).IsConnected) ?? entryNodes.FirstOrDefault();
            return entryNode != null ? GetTargetIDs(entryNode) : Enumerable.Empty<string>();
        }

        private static List<string> GetTargetIDs(INode node)
        {
            // Wire order = child order (i.e. choice order)
            var targetIDs = new List<string>();
            var connectedPorts = new List<IPort>();
            foreach (IPort outputPort in node.GetOutputPorts())
            {
                connectedPorts.Clear();
                outputPort.GetConnectedPorts(connectedPorts);
                foreach (INode target in connectedPorts.Select(connectedPort => connectedPort.GetNode()))
                {
                    if (target is not (SpeechGraphNode or ConditionGraphNode)) { continue; }
                    string targetID = GetNodeID(target);
                    if (!targetIDs.Contains(targetID)) { targetIDs.Add(targetID); }
                }
            }
            return targetIDs;
        }

        private static string GetNodeID(INode node) => ((Node)node).ID.ToString();
        #endregion
    }
}

using UnityEditor;
using Unity.GraphToolkit.Editor;
using LowDefMustard.Utils;
using Frankie.Speech;
using Frankie.Speech.Editor;

namespace Frankie.Tests.Editor
{
    internal static class DialogueGraphTestBuilders
    {
        // Builds Graph Toolkit dialogue graphs in tests (nodes are added before options are set: options only exist once added)
        // Note:  Graphs are saved to disk ~ delete their folder via file operations (never AssetDatabase.DeleteAsset, which removes localization entries)

        public static EntryGraphNode AddEntry(this DialogueGraph graph)
        {
            var entry = new EntryGraphNode();
            graph.AddNode(entry);
            return entry;
        }

        public static SpeechGraphNode AddLine(this DialogueGraph graph, string text, SpeakerType speakerType = SpeakerType.AISpeaker, string speakerName = "")
        {
            var line = new SpeechGraphNode();
            graph.AddNode(line);
            line.GetNodeOptionByName(SpeechGraphNode.speakerTypeOption).TrySetValue(speakerType);
            line.GetNodeOptionByName(SpeechGraphNode.speakerNameOption).TrySetValue(speakerName);
            line.GetNodeOptionByName(SpeechGraphNode.textOption).TrySetValue(text);
            return line;
        }

        public static void Place(this SpeechGraphNode line, int depth, int breadth)
        {
            line.GetNodeOptionByName(SpeechGraphNode.depthOption).TrySetValue(depth);
            line.GetNodeOptionByName(SpeechGraphNode.breadthOption).TrySetValue(breadth);
        }

        public static ConditionGraphNode AddCondition(this DialogueGraph graph, params Condition.PredicateWrapper[] predicates)
        {
            var condition = new ConditionGraphNode();
            graph.AddNode(condition);
            condition.AddPredicates(predicates);
            return condition;
        }

        public static void Connect(this DialogueGraph graph, INode from, INode to) => graph.Connect(GetOutPort(from), GetInPort(to));

        public static Dialogue SaveAndImport(this DialogueGraph graph)
        {
            string graphPath = GraphDatabase.GetGraphAssetPath(graph);
            GraphDatabase.SaveGraph(graph);
            AssetDatabase.ImportAsset(graphPath, ImportAssetOptions.ForceSynchronousImport);
            return AssetDatabase.LoadMainAssetAtPath(graphPath) as Dialogue;
        }

        #region Helpers
        private static IPort GetOutPort(INode node)
        {
            return node switch
            {
                EntryGraphNode => node.GetOutputPortByName(EntryGraphNode.outPort),
                SpeechGraphNode => node.GetOutputPortByName(SpeechGraphNode.outPort),
                ConditionGraphNode => node.GetOutputPortByName(ConditionGraphNode.outPort),
                _ => null
            };
        }

        private static IPort GetInPort(INode node)
        {
            return node switch
            {
                SpeechGraphNode => node.GetInputPortByName(SpeechGraphNode.inPort),
                ConditionGraphNode => node.GetInputPortByName(ConditionGraphNode.inPort),
                _ => null
            };
        }
        #endregion
    }
}

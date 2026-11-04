using System.Collections.Generic;
using LowDefMustard.Utils;
using LowDefMustard.Utils.Tests;
using Frankie.Speech;

namespace Frankie.Tests.Editor
{
    internal static class DialogueTestBuilders
    {
        // Note:  Objects are owned by the tracker, so destroy via its DestroyAll in TearDown
        
        public static DialogueNode Speech(this TestObjectTracker tracker, string nodeName, params string[] children)
        {
            var dialogueNode = tracker.Create<DialogueNode>(nodeName);
            foreach (string child in children) { dialogueNode.AddChild(child); }
            return dialogueNode;
        }

        public static DialogueConditionNode ConditionNode(this TestObjectTracker tracker, string nodeName, Condition condition, params string[] children)
        {
            var conditionNode = tracker.Create<DialogueConditionNode>(nodeName);
            conditionNode.Initialize(condition, children);
            return conditionNode;
        }

        public static Dialogue Graph(this TestObjectTracker tracker, IEnumerable<DialogueNode> dialogueNodes, IEnumerable<DialogueConditionNode> conditionNodes, params string[] entryChildren)
        {
            var dialogue = tracker.Create<Dialogue>("TestDialogue");
            dialogue.InitializeGraph(dialogueNodes, conditionNodes, entryChildren);
            return dialogue;
        }
    }
}

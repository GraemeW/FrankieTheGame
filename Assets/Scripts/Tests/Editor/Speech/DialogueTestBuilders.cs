using System.Collections.Generic;
using UnityEngine.Localization;
using LowDefMustard.Utils;
using LowDefMustard.Utils.Tests;
using Frankie.Speech;

namespace Frankie.Tests.Editor
{
    internal static class DialogueTestBuilders
    {
        // Note:  Objects are owned by the tracker, so destroy via its DestroyAll in TearDown
        //  - node IDs = the given node names (the importer uses Graph Toolkit node IDs)
        
        public static DialogueNode Speech(this TestObjectTracker tracker, string nodeName, params string[] children)
        {
            var dialogueNode = tracker.Create<DialogueNode>(nodeName);
            dialogueNode.Initialize(nodeName, SpeakerType.AISpeaker, null, 0, 0, new LocalizedString(), new LocalizedString(), children);
            return dialogueNode;
        }

        public static DialogueConditionNode ConditionNode(this TestObjectTracker tracker, string nodeName, Condition condition, params string[] children)
        {
            var conditionNode = tracker.Create<DialogueConditionNode>(nodeName);
            conditionNode.Initialize(nodeName, condition, children);
            return conditionNode;
        }

        public static Dialogue Graph(this TestObjectTracker tracker, IEnumerable<DialogueNode> dialogueNodes, IEnumerable<DialogueConditionNode> conditionNodes, params string[] entryChildren)
        {
            var dialogue = tracker.Create<Dialogue>("TestDialogue");
            dialogue.Initialize(dialogueNodes, conditionNodes, entryChildren);
            return dialogue;
        }
    }
}

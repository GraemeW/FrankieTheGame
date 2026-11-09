#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using LowDefMustard.Utils;
using LowDefMustard.Utils.Tests;
using Frankie.Speech;

namespace Frankie.Tests
{
    internal static class DialogueTestBuilders
    {
        // Note:  Objects are owned by the tracker, so destroy via its DestroyAll in TearDown
        //  - node IDs = the given node names (the importer uses Graph Toolkit node IDs)
        
        public static DialogueNode CreateDialogueNode(this TestObjectTracker tracker, string nodeName, params string[] children)
        {
            var dialogueNode = tracker.Create<DialogueNode>(nodeName);
            dialogueNode.Initialize(nodeName, SpeakerType.AISpeaker, null, 0, 0, new LocalizedString(), new LocalizedString(), children);
            return dialogueNode;
        }

        public static DialogueConditionNode CreateDialogueConditionNode(this TestObjectTracker tracker, string nodeName, Condition condition, params string[] children)
        {
            var conditionNode = tracker.Create<DialogueConditionNode>(nodeName);
            conditionNode.Initialize(nodeName, condition, children);
            return conditionNode;
        }

        public static Dialogue CreateDialogue(this TestObjectTracker tracker, IEnumerable<DialogueNode> dialogueNodes, IEnumerable<DialogueConditionNode> conditionNodes, params string[] entryChildren)
        {
            var dialogue = tracker.Create<Dialogue>("TestDialogue");
            dialogue.Initialize(dialogueNodes, conditionNodes, entryChildren);
            return dialogue;
        }

        public static Dialogue CreateSingleLineDialogue(this TestObjectTracker tracker) => tracker.CreateDialogue(new[] { tracker.CreateDialogueNode("Line") }, new DialogueConditionNode[0], "Line");
        public static Dialogue CreateEmptyDialogue(this TestObjectTracker tracker) => tracker.CreateDialogue(new DialogueNode[0], new DialogueConditionNode[0]);

        // Conversant on a child, as in game (DialogueController reads evaluators from the conversant's parent)
        public static AIConversant SpawnConversant(this PlayerStateRig rig, string conversantName)
        {
            GameObject npc = rig.Track(new GameObject(conversantName));
            var conversantObject = new GameObject($"{conversantName}_Conversant");
            conversantObject.transform.SetParent(npc.transform);
            return conversantObject.AddComponent<AIConversant>();
        }
    }
}
#endif

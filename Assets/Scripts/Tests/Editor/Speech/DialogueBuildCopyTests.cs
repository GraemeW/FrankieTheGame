using NUnit.Framework;
using LowDefMustard.Utils.Tests;
using static LowDefMustard.Utils.Tests.ConditionBuilder;
using static LowDefMustard.Utils.Tests.EvaluatorBuilder;
using Frankie.Speech;

namespace Frankie.Tests.Editor
{
    public class DialogueBuildCopyTests
    {
        // Tested:
        //  - DialogueNode.AreNodesMatched - build copy matches, different node / null / empty ID don't
        //  - DialogueNode.AreNodesMatched - matchIfBothNull matches null + null only
        //  - Dialogue.ResolveChildren - a build copy of the parent resolves the same children (by child ID)
        //  - Dialogue.ResolveChildren - a child reached directly and through a passing condition node resolves once (visited by ID)
        // Not Tested:
        //  - DialogueTrigger / DialogueController matching - req's Play Mode (see DialogueTriggerBuildCopyTests)
        // Note:  Dialogues are in memory (DialogueTestBuilders);  node IDs = node names

        // State
        private readonly TestObjectTracker tracker = new();

        #region Setup
        [TearDown]
        public void TearDown()
        {
            tracker.DestroyAll();
        }
        #endregion

        #region Tests
        [Test]
        public void AreNodesMatched_BuildCopy_Matches()
        {
            DialogueNode dialogueNode = tracker.CreateDialogueNode("Line");

            Assert.IsTrue(DialogueNode.AreNodesMatched(dialogueNode, tracker.CreateBuildCopy(dialogueNode)));
        }

        [Test]
        public void AreNodesMatched_DifferentNodeNullOrEmptyID_NoMatch()
        {
            DialogueNode dialogueNode = tracker.CreateDialogueNode("Line");
            DialogueNode emptyIDNode = tracker.CreateDialogueNode("");

            Assert.IsFalse(DialogueNode.AreNodesMatched(dialogueNode, tracker.CreateDialogueNode("OtherLine")));
            Assert.IsFalse(DialogueNode.AreNodesMatched(dialogueNode, null));
            Assert.IsFalse(DialogueNode.AreNodesMatched(null, null));
            Assert.IsFalse(DialogueNode.AreNodesMatched(emptyIDNode, tracker.CreateBuildCopy(emptyIDNode)));
        }

        [Test]
        public void AreNodesMatched_MatchIfBothNull_OnlyBothNullMatch()
        {
            DialogueNode dialogueNode = tracker.CreateDialogueNode("Line");

            Assert.IsTrue(DialogueNode.AreNodesMatched(null, null, true));
            Assert.IsFalse(DialogueNode.AreNodesMatched(dialogueNode, null, true));
            Assert.IsFalse(DialogueNode.AreNodesMatched(null, dialogueNode, true));
        }

        [Test]
        public void ResolveChildren_ParentBuildCopy_ResolvesSameChildren()
        {
            DialogueNode parent = tracker.CreateDialogueNode("Parent", "Child");
            DialogueNode child = tracker.CreateDialogueNode("Child");
            Dialogue dialogue = tracker.CreateDialogue(new[] { parent, child }, new DialogueConditionNode[0], "Parent");

            DialogueAssert.NodeIDs(dialogue.ResolveChildren(tracker.CreateBuildCopy(parent), Evaluators()), "Child");
        }

        [Test]
        public void ResolveChildren_ChildReachedTwice_ResolvedOnce()
        {
            var passing = tracker.Create<TestPredicate>("Passing");
            DialogueNode parent = tracker.CreateDialogueNode("Parent", "Child", "Gate");
            DialogueNode child = tracker.CreateDialogueNode("Child");
            DialogueConditionNode gate = tracker.CreateDialogueConditionNode("Gate", When(passing), "Child");
            Dialogue dialogue = tracker.CreateDialogue(new[] { parent, child }, new[] { gate }, "Parent");

            DialogueAssert.NodeIDs(dialogue.ResolveChildren(parent, Evaluators((passing, true))), "Child");
        }
        #endregion
    }
}

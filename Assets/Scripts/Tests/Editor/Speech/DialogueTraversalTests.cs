using System.Collections.Generic;
using NUnit.Framework;
using LowDefMustard.Utils;
using LowDefMustard.Utils.Tests;
using static LowDefMustard.Utils.Tests.ConditionBuilder;
using static LowDefMustard.Utils.Tests.EvaluatorBuilder;
using Frankie.Speech;

namespace Frankie.Tests.Editor
{
    public class DialogueTraversalTests
    {
        // Covers Dialogue.ResolveEntry / ResolveChildren over in-memory dialogues (no assets)
        //  - Graph dialogues: entry children + condition nodes (AND chained, OR parallel)
        // Not covered here: DialogueController (entry events, random pick, choosing) - req's Play Mode

        // State
        private readonly TestObjectTracker tracker = new();
        private TestPredicate passing;
        private TestPredicate failing;
        private List<IPredicateEvaluator> evaluators;

        #region Setup
        [SetUp]
        public void SetUp()
        {
            passing = tracker.Create<TestPredicate>("Passing");
            failing = tracker.Create<TestPredicate>("Failing");
            evaluators = Evaluators((passing, true), (failing, false));
        }

        [TearDown]
        public void TearDown()
        {
            tracker.DestroyAll();
        }
        #endregion

        #region GraphDialogues
        [Test]
        public void ResolveEntry_EntryChildren_ResolvedInOrder()
        {
            DialogueNode first = tracker.CreateDialogueNode("First");
            DialogueNode second = tracker.CreateDialogueNode("Second");
            Dialogue dialogue = tracker.CreateDialogue(new[] { first, second }, new DialogueConditionNode[0], "Second", "First");

            DialogueAssert.NodeIDs(dialogue.ResolveEntry(evaluators), "Second", "First");
        }

        [Test]
        public void ResolveChildren_PassingConditionNode_ExpandsToItsChildren()
        {
            DialogueNode parent = tracker.CreateDialogueNode("Parent", "Gate");
            DialogueNode gated = tracker.CreateDialogueNode("Gated");
            DialogueConditionNode gate = tracker.CreateDialogueConditionNode("Gate", When(passing), "Gated");
            Dialogue dialogue = tracker.CreateDialogue(new[] { parent, gated }, new[] { gate }, "Parent");

            DialogueAssert.NodeIDs(dialogue.ResolveChildren(parent, evaluators), "Gated");
        }

        [Test]
        public void ResolveChildren_FailingConditionNode_DropsItsChildren()
        {
            DialogueNode parent = tracker.CreateDialogueNode("Parent", "Gate", "Open");
            DialogueNode gated = tracker.CreateDialogueNode("Gated");
            DialogueNode open = tracker.CreateDialogueNode("Open");
            DialogueConditionNode gate = tracker.CreateDialogueConditionNode("Gate", When(failing), "Gated");
            Dialogue dialogue = tracker.CreateDialogue(new[] { parent, gated, open }, new[] { gate }, "Parent");

            DialogueAssert.NodeIDs(dialogue.ResolveChildren(parent, evaluators), "Open");
        }

        [Test]
        public void ResolveChildren_NegatedFailingPredicate_Passes()
        {
            DialogueNode parent = tracker.CreateDialogueNode("Parent", "Gate");
            DialogueNode gated = tracker.CreateDialogueNode("Gated");
            DialogueConditionNode gate = tracker.CreateDialogueConditionNode("Gate", When(failing, negate: true), "Gated");
            Dialogue dialogue = tracker.CreateDialogue(new[] { parent, gated }, new[] { gate }, "Parent");

            DialogueAssert.NodeIDs(dialogue.ResolveChildren(parent, evaluators), "Gated");
        }

        [Test]
        public void ResolveChildren_ChainedConditionNodes_ActAsAnd()
        {
            // Parent -> Outer(pass) -> Inner(fail) -> Blocked, and Parent -> Outer -> Reached
            DialogueNode parent = tracker.CreateDialogueNode("Parent", "Outer");
            DialogueNode blocked = tracker.CreateDialogueNode("Blocked");
            DialogueNode reached = tracker.CreateDialogueNode("Reached");
            DialogueConditionNode outer = tracker.CreateDialogueConditionNode("Outer", When(passing), "Inner", "Reached");
            DialogueConditionNode inner = tracker.CreateDialogueConditionNode("Inner", When(failing), "Blocked");
            Dialogue dialogue = tracker.CreateDialogue(new[] { parent, blocked, reached }, new[] { outer, inner }, "Parent");

            DialogueAssert.NodeIDs(dialogue.ResolveChildren(parent, evaluators), "Reached");
        }

        [Test]
        public void ResolveChildren_ParallelConditionNodes_ActAsOr()
        {
            // Either gate reaching the same node is enough, and it's listed once
            DialogueNode parent = tracker.CreateDialogueNode("Parent", "FailGate", "PassGate", "OtherPassGate");
            DialogueNode target = tracker.CreateDialogueNode("Target");
            DialogueConditionNode failGate = tracker.CreateDialogueConditionNode("FailGate", When(failing), "Target");
            DialogueConditionNode passGate = tracker.CreateDialogueConditionNode("PassGate", When(passing), "Target");
            DialogueConditionNode otherPassGate = tracker.CreateDialogueConditionNode("OtherPassGate", When(passing), "Target");
            Dialogue dialogue = tracker.CreateDialogue(new[] { parent, target }, new[] { failGate, passGate, otherPassGate }, "Parent");

            DialogueAssert.NodeIDs(dialogue.ResolveChildren(parent, evaluators), "Target");
        }

        [Test]
        public void ResolveChildren_ParallelConditionNodes_AllFailing_DropsTarget()
        {
            DialogueNode parent = tracker.CreateDialogueNode("Parent", "GateA", "GateB");
            DialogueNode target = tracker.CreateDialogueNode("Target");
            DialogueConditionNode gateA = tracker.CreateDialogueConditionNode("GateA", When(failing), "Target");
            DialogueConditionNode gateB = tracker.CreateDialogueConditionNode("GateB", When(failing), "Target");
            Dialogue dialogue = tracker.CreateDialogue(new[] { parent, target }, new[] { gateA, gateB }, "Parent");

            Assert.IsEmpty(dialogue.ResolveChildren(parent, evaluators));
        }

        [Test]
        public void ResolveChildren_DirectAndGatedMix_KeepsFirstReachOrder()
        {
            // Parent -> A, Gate(pass) -> [C, A], B : A is already reached directly, so it isn't repeated
            DialogueNode parent = tracker.CreateDialogueNode("Parent", "A", "Gate", "B");
            DialogueNode a = tracker.CreateDialogueNode("A");
            DialogueNode b = tracker.CreateDialogueNode("B");
            DialogueNode c = tracker.CreateDialogueNode("C");
            DialogueConditionNode gate = tracker.CreateDialogueConditionNode("Gate", When(passing), "C", "A");
            Dialogue dialogue = tracker.CreateDialogue(new[] { parent, a, b, c }, new[] { gate }, "Parent");

            DialogueAssert.NodeIDs(dialogue.ResolveChildren(parent, evaluators), "A", "C", "B");
        }

        [Test]
        public void ResolveChildren_ConditionNodeWithAndOfPredicates_NeedsAllToPass()
        {
            DialogueNode parent = tracker.CreateDialogueNode("Parent", "BothGate");
            DialogueNode target = tracker.CreateDialogueNode("Target");
            DialogueConditionNode bothGate = tracker.CreateDialogueConditionNode("BothGate", WhenAll(passing, failing), "Target");
            Dialogue dialogue = tracker.CreateDialogue(new[] { parent, target }, new[] { bothGate }, "Parent");

            Assert.IsEmpty(dialogue.ResolveChildren(parent, evaluators));
        }

        [Test]
        public void ResolveChildren_LogicOnlyCycle_Terminates()
        {
            // GateA -> GateB -> GateA (+ Target): must not recurse forever
            DialogueNode parent = tracker.CreateDialogueNode("Parent", "GateA");
            DialogueNode target = tracker.CreateDialogueNode("Target");
            DialogueConditionNode gateA = tracker.CreateDialogueConditionNode("GateA", When(passing), "GateB");
            DialogueConditionNode gateB = tracker.CreateDialogueConditionNode("GateB", When(passing), "GateA", "Target");
            Dialogue dialogue = tracker.CreateDialogue(new[] { parent, target }, new[] { gateA, gateB }, "Parent");

            DialogueAssert.NodeIDs(dialogue.ResolveChildren(parent, evaluators), "Target");
        }

        [Test]
        public void ResolveChildren_MissingChildID_IsIgnored()
        {
            DialogueNode parent = tracker.CreateDialogueNode("Parent", "Missing", "Present");
            DialogueNode present = tracker.CreateDialogueNode("Present");
            Dialogue dialogue = tracker.CreateDialogue(new[] { parent, present }, new DialogueConditionNode[0], "Parent");

            DialogueAssert.NodeIDs(dialogue.ResolveChildren(parent, evaluators), "Present");
        }

        [Test]
        public void ResolveChildren_NullNode_ReturnsEmpty()
        {
            Dialogue dialogue = tracker.CreateDialogue(new[] { tracker.CreateDialogueNode("Only") }, new DialogueConditionNode[0], "Only");

            Assert.IsEmpty(dialogue.ResolveChildren(null, evaluators));
        }

        [Test]
        public void ResolveEntry_GatedEntryChildren_Filtered()
        {
            // e.g. first-visit vs. repeat-visit openers: entry fans out through conditions directly
            DialogueNode first = tracker.CreateDialogueNode("FirstVisit");
            DialogueNode repeat = tracker.CreateDialogueNode("RepeatVisit");
            DialogueConditionNode firstGate = tracker.CreateDialogueConditionNode("FirstGate", When(failing), "FirstVisit");
            DialogueConditionNode repeatGate = tracker.CreateDialogueConditionNode("RepeatGate", When(passing), "RepeatVisit");
            Dialogue dialogue = tracker.CreateDialogue(new[] { first, repeat }, new[] { firstGate, repeatGate }, "FirstGate", "RepeatGate");

            DialogueAssert.NodeIDs(dialogue.ResolveEntry(evaluators), "RepeatVisit");
        }

        [Test]
        public void ResolveEntry_EmptyDialogue_ReturnsEmpty()
        {
            Dialogue dialogue = tracker.CreateDialogue(new DialogueNode[0], new DialogueConditionNode[0]);

            Assert.IsEmpty(dialogue.ResolveEntry(evaluators));
        }
        #endregion
    }
}

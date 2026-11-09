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
        private readonly TestObjectTracker objects = new();
        private TestPredicate passing;
        private TestPredicate failing;
        private List<IPredicateEvaluator> evaluators;

        #region Setup
        [SetUp]
        public void SetUp()
        {
            passing = objects.Create<TestPredicate>("Passing");
            failing = objects.Create<TestPredicate>("Failing");
            evaluators = Evaluators((passing, true), (failing, false));
        }

        [TearDown]
        public void TearDown()
        {
            objects.DestroyAll();
        }
        #endregion

        #region GraphDialogues
        [Test]
        public void ResolveEntry_EntryChildren_ResolvedInOrder()
        {
            DialogueNode first = objects.CreateDialogueNode("First");
            DialogueNode second = objects.CreateDialogueNode("Second");
            Dialogue dialogue = objects.CreateDialogue(new[] { first, second }, new DialogueConditionNode[0], "Second", "First");

            DialogueAssert.NodeIDs(dialogue.ResolveEntry(evaluators), "Second", "First");
        }

        [Test]
        public void ResolveChildren_PassingConditionNode_ExpandsToItsChildren()
        {
            DialogueNode parent = objects.CreateDialogueNode("Parent", "Gate");
            DialogueNode gated = objects.CreateDialogueNode("Gated");
            DialogueConditionNode gate = objects.CreateDialogueConditionNode("Gate", When(passing), "Gated");
            Dialogue dialogue = objects.CreateDialogue(new[] { parent, gated }, new[] { gate }, "Parent");

            DialogueAssert.NodeIDs(dialogue.ResolveChildren(parent, evaluators), "Gated");
        }

        [Test]
        public void ResolveChildren_FailingConditionNode_DropsItsChildren()
        {
            DialogueNode parent = objects.CreateDialogueNode("Parent", "Gate", "Open");
            DialogueNode gated = objects.CreateDialogueNode("Gated");
            DialogueNode open = objects.CreateDialogueNode("Open");
            DialogueConditionNode gate = objects.CreateDialogueConditionNode("Gate", When(failing), "Gated");
            Dialogue dialogue = objects.CreateDialogue(new[] { parent, gated, open }, new[] { gate }, "Parent");

            DialogueAssert.NodeIDs(dialogue.ResolveChildren(parent, evaluators), "Open");
        }

        [Test]
        public void ResolveChildren_NegatedFailingPredicate_Passes()
        {
            DialogueNode parent = objects.CreateDialogueNode("Parent", "Gate");
            DialogueNode gated = objects.CreateDialogueNode("Gated");
            DialogueConditionNode gate = objects.CreateDialogueConditionNode("Gate", When(failing, negate: true), "Gated");
            Dialogue dialogue = objects.CreateDialogue(new[] { parent, gated }, new[] { gate }, "Parent");

            DialogueAssert.NodeIDs(dialogue.ResolveChildren(parent, evaluators), "Gated");
        }

        [Test]
        public void ResolveChildren_ChainedConditionNodes_ActAsAnd()
        {
            // Parent -> Outer(pass) -> Inner(fail) -> Blocked, and Parent -> Outer -> Reached
            DialogueNode parent = objects.CreateDialogueNode("Parent", "Outer");
            DialogueNode blocked = objects.CreateDialogueNode("Blocked");
            DialogueNode reached = objects.CreateDialogueNode("Reached");
            DialogueConditionNode outer = objects.CreateDialogueConditionNode("Outer", When(passing), "Inner", "Reached");
            DialogueConditionNode inner = objects.CreateDialogueConditionNode("Inner", When(failing), "Blocked");
            Dialogue dialogue = objects.CreateDialogue(new[] { parent, blocked, reached }, new[] { outer, inner }, "Parent");

            DialogueAssert.NodeIDs(dialogue.ResolveChildren(parent, evaluators), "Reached");
        }

        [Test]
        public void ResolveChildren_ParallelConditionNodes_ActAsOr()
        {
            // Either gate reaching the same node is enough, and it's listed once
            DialogueNode parent = objects.CreateDialogueNode("Parent", "FailGate", "PassGate", "OtherPassGate");
            DialogueNode target = objects.CreateDialogueNode("Target");
            DialogueConditionNode failGate = objects.CreateDialogueConditionNode("FailGate", When(failing), "Target");
            DialogueConditionNode passGate = objects.CreateDialogueConditionNode("PassGate", When(passing), "Target");
            DialogueConditionNode otherPassGate = objects.CreateDialogueConditionNode("OtherPassGate", When(passing), "Target");
            Dialogue dialogue = objects.CreateDialogue(new[] { parent, target }, new[] { failGate, passGate, otherPassGate }, "Parent");

            DialogueAssert.NodeIDs(dialogue.ResolveChildren(parent, evaluators), "Target");
        }

        [Test]
        public void ResolveChildren_ParallelConditionNodes_AllFailing_DropsTarget()
        {
            DialogueNode parent = objects.CreateDialogueNode("Parent", "GateA", "GateB");
            DialogueNode target = objects.CreateDialogueNode("Target");
            DialogueConditionNode gateA = objects.CreateDialogueConditionNode("GateA", When(failing), "Target");
            DialogueConditionNode gateB = objects.CreateDialogueConditionNode("GateB", When(failing), "Target");
            Dialogue dialogue = objects.CreateDialogue(new[] { parent, target }, new[] { gateA, gateB }, "Parent");

            Assert.IsEmpty(dialogue.ResolveChildren(parent, evaluators));
        }

        [Test]
        public void ResolveChildren_DirectAndGatedMix_KeepsFirstReachOrder()
        {
            // Parent -> A, Gate(pass) -> [C, A], B : A is already reached directly, so it isn't repeated
            DialogueNode parent = objects.CreateDialogueNode("Parent", "A", "Gate", "B");
            DialogueNode a = objects.CreateDialogueNode("A");
            DialogueNode b = objects.CreateDialogueNode("B");
            DialogueNode c = objects.CreateDialogueNode("C");
            DialogueConditionNode gate = objects.CreateDialogueConditionNode("Gate", When(passing), "C", "A");
            Dialogue dialogue = objects.CreateDialogue(new[] { parent, a, b, c }, new[] { gate }, "Parent");

            DialogueAssert.NodeIDs(dialogue.ResolveChildren(parent, evaluators), "A", "C", "B");
        }

        [Test]
        public void ResolveChildren_ConditionNodeWithAndOfPredicates_NeedsAllToPass()
        {
            DialogueNode parent = objects.CreateDialogueNode("Parent", "BothGate");
            DialogueNode target = objects.CreateDialogueNode("Target");
            DialogueConditionNode bothGate = objects.CreateDialogueConditionNode("BothGate", WhenAll(passing, failing), "Target");
            Dialogue dialogue = objects.CreateDialogue(new[] { parent, target }, new[] { bothGate }, "Parent");

            Assert.IsEmpty(dialogue.ResolveChildren(parent, evaluators));
        }

        [Test]
        public void ResolveChildren_LogicOnlyCycle_Terminates()
        {
            // GateA -> GateB -> GateA (+ Target): must not recurse forever
            DialogueNode parent = objects.CreateDialogueNode("Parent", "GateA");
            DialogueNode target = objects.CreateDialogueNode("Target");
            DialogueConditionNode gateA = objects.CreateDialogueConditionNode("GateA", When(passing), "GateB");
            DialogueConditionNode gateB = objects.CreateDialogueConditionNode("GateB", When(passing), "GateA", "Target");
            Dialogue dialogue = objects.CreateDialogue(new[] { parent, target }, new[] { gateA, gateB }, "Parent");

            DialogueAssert.NodeIDs(dialogue.ResolveChildren(parent, evaluators), "Target");
        }

        [Test]
        public void ResolveChildren_MissingChildID_IsIgnored()
        {
            DialogueNode parent = objects.CreateDialogueNode("Parent", "Missing", "Present");
            DialogueNode present = objects.CreateDialogueNode("Present");
            Dialogue dialogue = objects.CreateDialogue(new[] { parent, present }, new DialogueConditionNode[0], "Parent");

            DialogueAssert.NodeIDs(dialogue.ResolveChildren(parent, evaluators), "Present");
        }

        [Test]
        public void ResolveChildren_NullNode_ReturnsEmpty()
        {
            Dialogue dialogue = objects.CreateDialogue(new[] { objects.CreateDialogueNode("Only") }, new DialogueConditionNode[0], "Only");

            Assert.IsEmpty(dialogue.ResolveChildren(null, evaluators));
        }

        [Test]
        public void ResolveEntry_GatedEntryChildren_Filtered()
        {
            // e.g. first-visit vs. repeat-visit openers: entry fans out through conditions directly
            DialogueNode first = objects.CreateDialogueNode("FirstVisit");
            DialogueNode repeat = objects.CreateDialogueNode("RepeatVisit");
            DialogueConditionNode firstGate = objects.CreateDialogueConditionNode("FirstGate", When(failing), "FirstVisit");
            DialogueConditionNode repeatGate = objects.CreateDialogueConditionNode("RepeatGate", When(passing), "RepeatVisit");
            Dialogue dialogue = objects.CreateDialogue(new[] { first, repeat }, new[] { firstGate, repeatGate }, "FirstGate", "RepeatGate");

            DialogueAssert.NodeIDs(dialogue.ResolveEntry(evaluators), "RepeatVisit");
        }

        [Test]
        public void ResolveEntry_EmptyDialogue_ReturnsEmpty()
        {
            Dialogue dialogue = objects.CreateDialogue(new DialogueNode[0], new DialogueConditionNode[0]);

            Assert.IsEmpty(dialogue.ResolveEntry(evaluators));
        }
        #endregion
    }
}

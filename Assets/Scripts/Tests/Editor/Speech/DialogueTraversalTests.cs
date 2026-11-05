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
        //  - Legacy (pre-graph) dialogues: root / skip-root entry and per-node conditions, until migration retires them - To-Be-Removed (pending)
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
            DialogueNode first = objects.Speech("First");
            DialogueNode second = objects.Speech("Second");
            Dialogue dialogue = objects.Graph(new[] { first, second }, new DialogueConditionNode[0], "Second", "First");

            DialogueAssert.NodeIDs(dialogue.ResolveEntry(evaluators), "Second", "First");
        }

        [Test]
        public void ResolveChildren_PassingConditionNode_ExpandsToItsChildren()
        {
            DialogueNode parent = objects.Speech("Parent", "Gate");
            DialogueNode gated = objects.Speech("Gated");
            DialogueConditionNode gate = objects.ConditionNode("Gate", When(passing), "Gated");
            Dialogue dialogue = objects.Graph(new[] { parent, gated }, new[] { gate }, "Parent");

            DialogueAssert.NodeIDs(dialogue.ResolveChildren(parent, evaluators), "Gated");
        }

        [Test]
        public void ResolveChildren_FailingConditionNode_DropsItsChildren()
        {
            DialogueNode parent = objects.Speech("Parent", "Gate", "Open");
            DialogueNode gated = objects.Speech("Gated");
            DialogueNode open = objects.Speech("Open");
            DialogueConditionNode gate = objects.ConditionNode("Gate", When(failing), "Gated");
            Dialogue dialogue = objects.Graph(new[] { parent, gated, open }, new[] { gate }, "Parent");

            DialogueAssert.NodeIDs(dialogue.ResolveChildren(parent, evaluators), "Open");
        }

        [Test]
        public void ResolveChildren_NegatedFailingPredicate_Passes()
        {
            DialogueNode parent = objects.Speech("Parent", "Gate");
            DialogueNode gated = objects.Speech("Gated");
            DialogueConditionNode gate = objects.ConditionNode("Gate", When(failing, negate: true), "Gated");
            Dialogue dialogue = objects.Graph(new[] { parent, gated }, new[] { gate }, "Parent");

            DialogueAssert.NodeIDs(dialogue.ResolveChildren(parent, evaluators), "Gated");
        }

        [Test]
        public void ResolveChildren_ChainedConditionNodes_ActAsAnd()
        {
            // Parent -> Outer(pass) -> Inner(fail) -> Blocked, and Parent -> Outer -> Reached
            DialogueNode parent = objects.Speech("Parent", "Outer");
            DialogueNode blocked = objects.Speech("Blocked");
            DialogueNode reached = objects.Speech("Reached");
            DialogueConditionNode outer = objects.ConditionNode("Outer", When(passing), "Inner", "Reached");
            DialogueConditionNode inner = objects.ConditionNode("Inner", When(failing), "Blocked");
            Dialogue dialogue = objects.Graph(new[] { parent, blocked, reached }, new[] { outer, inner }, "Parent");

            DialogueAssert.NodeIDs(dialogue.ResolveChildren(parent, evaluators), "Reached");
        }

        [Test]
        public void ResolveChildren_ParallelConditionNodes_ActAsOr()
        {
            // Either gate reaching the same node is enough, and it's listed once
            DialogueNode parent = objects.Speech("Parent", "FailGate", "PassGate", "OtherPassGate");
            DialogueNode target = objects.Speech("Target");
            DialogueConditionNode failGate = objects.ConditionNode("FailGate", When(failing), "Target");
            DialogueConditionNode passGate = objects.ConditionNode("PassGate", When(passing), "Target");
            DialogueConditionNode otherPassGate = objects.ConditionNode("OtherPassGate", When(passing), "Target");
            Dialogue dialogue = objects.Graph(new[] { parent, target }, new[] { failGate, passGate, otherPassGate }, "Parent");

            DialogueAssert.NodeIDs(dialogue.ResolveChildren(parent, evaluators), "Target");
        }

        [Test]
        public void ResolveChildren_ParallelConditionNodes_AllFailing_DropsTarget()
        {
            DialogueNode parent = objects.Speech("Parent", "GateA", "GateB");
            DialogueNode target = objects.Speech("Target");
            DialogueConditionNode gateA = objects.ConditionNode("GateA", When(failing), "Target");
            DialogueConditionNode gateB = objects.ConditionNode("GateB", When(failing), "Target");
            Dialogue dialogue = objects.Graph(new[] { parent, target }, new[] { gateA, gateB }, "Parent");

            Assert.IsEmpty(dialogue.ResolveChildren(parent, evaluators));
        }

        [Test]
        public void ResolveChildren_DirectAndGatedMix_KeepsFirstReachOrder()
        {
            // Parent -> A, Gate(pass) -> [C, A], B : A is already reached directly, so it isn't repeated
            DialogueNode parent = objects.Speech("Parent", "A", "Gate", "B");
            DialogueNode a = objects.Speech("A");
            DialogueNode b = objects.Speech("B");
            DialogueNode c = objects.Speech("C");
            DialogueConditionNode gate = objects.ConditionNode("Gate", When(passing), "C", "A");
            Dialogue dialogue = objects.Graph(new[] { parent, a, b, c }, new[] { gate }, "Parent");

            DialogueAssert.NodeIDs(dialogue.ResolveChildren(parent, evaluators), "A", "C", "B");
        }

        [Test]
        public void ResolveChildren_ConditionNodeWithAndOfPredicates_NeedsAllToPass()
        {
            DialogueNode parent = objects.Speech("Parent", "BothGate");
            DialogueNode target = objects.Speech("Target");
            DialogueConditionNode bothGate = objects.ConditionNode("BothGate", WhenAll(passing, failing), "Target");
            Dialogue dialogue = objects.Graph(new[] { parent, target }, new[] { bothGate }, "Parent");

            Assert.IsEmpty(dialogue.ResolveChildren(parent, evaluators));
        }

        [Test]
        public void ResolveChildren_LogicOnlyCycle_Terminates()
        {
            // GateA -> GateB -> GateA (+ Target): must not recurse forever
            DialogueNode parent = objects.Speech("Parent", "GateA");
            DialogueNode target = objects.Speech("Target");
            DialogueConditionNode gateA = objects.ConditionNode("GateA", When(passing), "GateB");
            DialogueConditionNode gateB = objects.ConditionNode("GateB", When(passing), "GateA", "Target");
            Dialogue dialogue = objects.Graph(new[] { parent, target }, new[] { gateA, gateB }, "Parent");

            DialogueAssert.NodeIDs(dialogue.ResolveChildren(parent, evaluators), "Target");
        }

        [Test]
        public void ResolveChildren_MissingChildID_IsIgnored()
        {
            DialogueNode parent = objects.Speech("Parent", "Missing", "Present");
            DialogueNode present = objects.Speech("Present");
            Dialogue dialogue = objects.Graph(new[] { parent, present }, new DialogueConditionNode[0], "Parent");

            DialogueAssert.NodeIDs(dialogue.ResolveChildren(parent, evaluators), "Present");
        }

        [Test]
        public void ResolveChildren_NullNode_ReturnsEmpty()
        {
            Dialogue dialogue = objects.Graph(new[] { objects.Speech("Only") }, new DialogueConditionNode[0], "Only");

            Assert.IsEmpty(dialogue.ResolveChildren(null, evaluators));
        }

        [Test]
        public void ResolveEntry_GatedEntryChildren_Filtered()
        {
            // Replaces the legacy skipped dummy root: entry fans out through conditions directly
            DialogueNode first = objects.Speech("FirstVisit");
            DialogueNode repeat = objects.Speech("RepeatVisit");
            DialogueConditionNode firstGate = objects.ConditionNode("FirstGate", When(failing), "FirstVisit");
            DialogueConditionNode repeatGate = objects.ConditionNode("RepeatGate", When(passing), "RepeatVisit");
            Dialogue dialogue = objects.Graph(new[] { first, repeat }, new[] { firstGate, repeatGate }, "FirstGate", "RepeatGate");

            DialogueAssert.NodeIDs(dialogue.ResolveEntry(evaluators), "RepeatVisit");
        }
        #endregion

        #region LegacyDialogues
        [Test]
        public void ResolveEntry_LegacyWithoutEntryChildren_StartsAtRoot()
        {
            DialogueNode root = objects.Speech("Root", "Child");
            DialogueNode child = objects.Speech("Child");
            Dialogue dialogue = objects.Graph(new[] { root, child }, new DialogueConditionNode[0]);

            DialogueAssert.NodeIDs(dialogue.ResolveEntry(evaluators), "Root");
        }

        [Test]
        public void ResolveEntry_LegacySkipRoot_StartsAtRootChildren()
        {
            DialogueNode root = objects.Speech("Root", "ChildA", "ChildB");
            DialogueNode childA = objects.Speech("ChildA");
            DialogueNode childB = objects.Speech("ChildB");
            Dialogue dialogue = objects.Graph(new[] { root, childA, childB }, new DialogueConditionNode[0]);
            dialogue.skipRootNode = true;

            DialogueAssert.NodeIDs(dialogue.ResolveEntry(evaluators), "ChildA", "ChildB");
        }

        [Test]
        public void ResolveChildren_LegacyPerNodeCondition_StillFilters()
        {
            DialogueNode parent = objects.Speech("Parent", "Hidden", "Shown");
            DialogueNode hidden = objects.Speech("Hidden");
            DialogueNode shown = objects.Speech("Shown");
            hidden.SetCondition(When(failing));
            shown.SetCondition(When(passing));
            Dialogue dialogue = objects.Graph(new[] { parent, hidden, shown }, new DialogueConditionNode[0]);

            DialogueAssert.NodeIDs(dialogue.ResolveChildren(parent, evaluators), "Shown");
        }

        [Test]
        public void ResolveEntry_EmptyDialogue_ReturnsEmpty()
        {
            Dialogue dialogue = objects.Graph(new DialogueNode[0], new DialogueConditionNode[0]);

            Assert.IsEmpty(dialogue.ResolveEntry(evaluators));
        }
        #endregion
    }
}

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using Unity.GraphToolkit.Editor;
using LowDefMustard.Utils.Tests;
using Frankie.Speech.Editor;

namespace Frankie.Tests.Editor
{
    public class DialogueLinePositionPlannerTests
    {
        // Covers DialogueLinePositionPlanner over a saved temp graph (no window -> no localization sync runs)
        //  - new lines: placed once reachable from Entry; depth via the shallowest parent, condition nodes don't count; next free breadth
        //  - placed lines never move except off a shared (e.g. pasted) position

        // State
        private TempAssetFolder tempFolder;
        private DialogueGraph graph; // Deliberately state / not const:  reusing one path warns while the previous test's graph is still loaded
        private EntryGraphNode entry; // ^- same

        #region Setup
        [SetUp]
        public void SetUp()
        {
            tempFolder = new TempAssetFolder(nameof(DialogueLinePositionPlannerTests));
            graph = GraphDatabase.CreateGraph<DialogueGraph>(tempFolder.Path($"{TestContext.CurrentContext.Test.Name}.{DialogueGraph.assetExtension}"));
            entry = graph.AddEntry();
        }

        [TearDown]
        public void TearDown()
        {
            graph = null;
            tempFolder.Dispose();
        }
        #endregion

        #region Tests
        [Test]
        public void Plan_EntryChildren_DepthZeroInWireOrder()
        {
            SpeechGraphNode second = graph.AddLine("Second");
            SpeechGraphNode first = graph.AddLine("First");
            graph.Connect(entry, first);
            graph.Connect(entry, second);

            Dictionary<SpeechGraphNode, (int, int)> positions = Plan();

            Assert.AreEqual((0, 0), positions[first]);
            Assert.AreEqual((0, 1), positions[second]);
        }

        [Test]
        public void Plan_ThroughConditionNode_ConditionDoesNotCountTowardsDepth()
        {
            SpeechGraphNode parent = graph.AddLine("Parent");
            SpeechGraphNode gated = graph.AddLine("Gated");
            ConditionGraphNode gate = graph.AddCondition();
            graph.Connect(entry, gate);
            graph.Connect(gate, parent);
            graph.Connect(parent, gate);
            graph.Connect(parent, gated);

            Dictionary<SpeechGraphNode, (int, int)> positions = Plan();

            Assert.AreEqual((0, 0), positions[parent]);
            Assert.AreEqual((1, 0), positions[gated]);
        }

        [Test]
        public void Plan_NotReachableFromEntry_StaysUnplaced()
        {
            SpeechGraphNode orphanParent = graph.AddLine("Orphan");
            SpeechGraphNode orphanChild = graph.AddLine("Child");
            graph.Connect(orphanParent, orphanChild);

            Assert.IsEmpty(Plan());
        }

        [Test]
        public void Plan_SeveralParents_ShallowestWins()
        {
            SpeechGraphNode shallow = graph.AddLine("Shallow");
            SpeechGraphNode deep = graph.AddLine("Deep");
            SpeechGraphNode shared = graph.AddLine("Shared");
            shallow.Place(0, 0);
            deep.Place(4, 0);
            graph.Connect(entry, deep);
            graph.Connect(entry, shallow);
            graph.Connect(deep, shared);
            graph.Connect(shallow, shared);

            Dictionary<SpeechGraphNode, (int, int)> positions = Plan();

            CollectionAssert.AreEquivalent(new[] { shared }, positions.Keys);
            Assert.AreEqual((1, 0), positions[shared]);
        }

        [Test]
        public void Plan_NewLine_TakesNextFreeBreadth()
        {
            SpeechGraphNode parent = graph.AddLine("Parent");
            SpeechGraphNode placedA = graph.AddLine("A");
            SpeechGraphNode placedB = graph.AddLine("B");
            SpeechGraphNode added = graph.AddLine("Added");
            parent.Place(0, 0);
            placedA.Place(1, 0);
            placedB.Place(1, 3);
            graph.Connect(entry, parent);
            graph.Connect(parent, placedA);
            graph.Connect(parent, added);

            Assert.AreEqual((1, 4), Plan()[added]);
        }

        [Test]
        public void Plan_PlacedLines_NeverMove()
        {
            SpeechGraphNode placed = graph.AddLine("Placed");
            placed.Place(7, 2);
            graph.Connect(entry, placed);

            Assert.IsEmpty(Plan());
        }

        [Test]
        public void Plan_SharedPosition_SyncedLineKeepsIt()
        {
            SpeechGraphNode pasted = graph.AddLine("Copy");
            SpeechGraphNode original = graph.AddLine("Original");
            pasted.Place(0, 0);
            original.Place(0, 0);
            graph.Connect(entry, pasted);
            graph.Connect(entry, original);

            Dictionary<SpeechGraphNode, (int, int)> positions = Plan(original);

            CollectionAssert.AreEquivalent(new[] { pasted }, positions.Keys);
            Assert.AreEqual((0, 1), positions[pasted]);
        }

        [Test]
        public void PlanRekey_RenumbersBreadthFirstFromEntry()
        {
            SpeechGraphNode first = graph.AddLine("First");
            SpeechGraphNode second = graph.AddLine("Second");
            SpeechGraphNode child = graph.AddLine("Child");
            first.Place(3, 5);
            second.Place(2, 2);
            child.Place(1, 0);
            graph.Connect(entry, first);
            graph.Connect(entry, second);
            graph.Connect(first, child);

            Dictionary<SpeechGraphNode, (int, int)> positions = DialogueLinePositionPlanner.PlanRekey(graph).ToDictionary(position => position.line, position => (position.depth, position.breadth));

            CollectionAssert.AreEquivalent(new[] { first, second }, positions.Keys, "Only moved lines are returned (Child is already 1.0)");
            Assert.AreEqual((0, 0), positions[first]);
            Assert.AreEqual((0, 1), positions[second]);
        }

        [Test]
        public void PlanRekey_UnreachablePlacedLine_KeepsDepthAfterReachableOnes()
        {
            SpeechGraphNode reachable = graph.AddLine("Reachable");
            SpeechGraphNode unreachable = graph.AddLine("Unreachable");
            reachable.Place(0, 4);
            unreachable.Place(0, 0);
            graph.Connect(entry, reachable);

            Dictionary<SpeechGraphNode, (int, int)> positions = DialogueLinePositionPlanner.PlanRekey(graph).ToDictionary(position => position.line, position => (position.depth, position.breadth));

            Assert.AreEqual((0, 0), positions[reachable]);
            Assert.AreEqual((0, 1), positions[unreachable]);
        }
        #endregion

        #region Helpers
        private Dictionary<SpeechGraphNode, (int, int)> Plan(params SpeechGraphNode[] syncedLines)
        {
            var syncedLineIDs = new HashSet<string>(syncedLines.Select(DialogueLinePositionPlanner.GetID));
            return DialogueLinePositionPlanner.Plan(graph, syncedLineIDs).ToDictionary(position => position.line, position => (position.depth, position.breadth));
        }
        #endregion
    }
}

using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Unity.GraphToolkit.Editor;
using LowDefMustard.Utils;
using LowDefMustard.Utils.Tests;
using static LowDefMustard.Utils.Tests.EvaluatorBuilder;
using Frankie.Speech;
using Frankie.Speech.Editor;

namespace Frankie.Tests.Editor
{
    public class DialogueGraphImporterTests
    {
        // Covers DialogueGraphImporter w/ temp .dialoguegraph files
        //  - Runtime shape: main Dialogue, speech + condition sub-assets keyed by node ID, entry targets, wire order, keys
        //  - Reference stability: sub-asset fileIDs across reimport and graph edits
        // Not covered: Character option (needs a CharacterProperties asset), localization table contents (importer never writes them)

        // Const Tunables
        private const string _folder = "Assets/_TEMP_DialogueGraphImporterTests_SafeToDelete";
        private const string _graphName = "ImporterFixture";
        private const string _graphPath = _folder + "/" + _graphName + "." + DialogueGraph.assetExtension;
        private const string _predicatePath = _folder + "/Gate.asset";

        // State
        private DialogueGraph graph;
        private EntryGraphNode entry;
        private TestPredicate predicate;
        
        #region Setup
        [SetUp]
        public void SetUp()
        {
            Directory.CreateDirectory(_folder);
            AssetDatabase.Refresh();
            predicate = ScriptableObject.CreateInstance<TestPredicate>();
            AssetDatabase.CreateAsset(predicate, _predicatePath);

            graph = GraphDatabase.CreateGraph<DialogueGraph>(_graphPath);
            entry = graph.AddEntry();
        }

        [TearDown]
        public void TearDown()
        {
            // Deliberately using file operations (i.e. avoiding AssetDatabase.DeleteAsset): imported dialogues otherwise remove their localization entries on delete
            graph = null;
            if (Directory.Exists(_folder)) { Directory.Delete(_folder, true); }
            File.Delete(_folder + ".meta");
            AssetDatabase.Refresh();
        }
        #endregion

        #region PrivateHelpers
        private static IEnumerable<T> GetSubAssets<T>() where T : Object => AssetDatabase.LoadAllAssetsAtPath(_graphPath).OfType<T>();
        #endregion
        
        #region Tests
        [Test]
        public void Import_EmitsDialogueWithNodeSubAssetsKeyedByID()
        {
            SpeechGraphNode line = graph.AddLine("Hello");
            ConditionGraphNode gate = graph.AddCondition();
            graph.Connect(entry, line);
            graph.Connect(line, gate);

            Dialogue dialogue = graph.SaveAndImport();

            Assert.IsNotNull(dialogue, "Main object isn't a Dialogue");
            Assert.AreEqual(_graphName, dialogue.name);
            CollectionAssert.AreEquivalent(new[] { line.ID.ToString() }, GetSubAssets<DialogueNode>().Select(node => node.name));
            CollectionAssert.AreEquivalent(new[] { gate.ID.ToString() }, GetSubAssets<DialogueConditionNode>().Select(node => node.name));
            Assert.IsEmpty(GetSubAssets<ScriptableObject>().Where(subAsset => subAsset is not (Dialogue or DialogueNode or DialogueConditionNode)), "Unexpected sub-assets");
        }

        [Test]
        public void Import_EntryTargets_BecomeEntryNodes()
        {
            SpeechGraphNode first = graph.AddLine("First");
            SpeechGraphNode second = graph.AddLine("Second");
            graph.Connect(entry, second);
            graph.Connect(entry, first);

            Dialogue dialogue = graph.SaveAndImport();

            CollectionAssert.AreEqual(new[] { second.ID.ToString(), first.ID.ToString() }, dialogue.ResolveEntry(Evaluators((predicate, true))).Select(node => node.name));
        }

        [Test]
        public void Import_SpeechChildren_FollowWireOrder()
        {
            SpeechGraphNode parent = graph.AddLine("Parent");
            SpeechGraphNode a = graph.AddLine("A");
            SpeechGraphNode b = graph.AddLine("B");
            SpeechGraphNode c = graph.AddLine("C");
            graph.Connect(entry, parent);
            graph.Connect(parent, c);
            graph.Connect(parent, a);
            graph.Connect(parent, b);

            Dialogue dialogue = graph.SaveAndImport();

            DialogueNode imported = dialogue.GetNodeFromID(parent.ID.ToString());
            CollectionAssert.AreEqual(new[] { c, a, b }.Select(node => node.ID.ToString()), imported.GetChildren());
        }

        [Test]
        public void Import_ConditionNode_GatesItsTargets()
        {
            SpeechGraphNode parent = graph.AddLine("Parent");
            SpeechGraphNode gated = graph.AddLine("Gated");
            ConditionGraphNode gate = graph.AddCondition(new Condition.PredicateWrapper(predicate));
            graph.Connect(entry, parent);
            graph.Connect(parent, gate);
            graph.Connect(gate, gated);

            Dialogue dialogue = graph.SaveAndImport();
            DialogueNode importedParent = dialogue.GetNodeFromID(parent.ID.ToString());

            DialogueAssert.NodeNames(dialogue.ResolveChildren(importedParent, Evaluators((predicate, true))), gated.ID.ToString());
            Assert.IsEmpty(dialogue.ResolveChildren(importedParent, Evaluators((predicate, false))));
        }

        [Test]
        public void Import_SpeechNode_CarriesSpeakerAndLocalizationKeys()
        {
            SpeechGraphNode line = graph.AddLine("Hi", SpeakerType.PlayerSpeaker);
            line.Place(2, 1);
            SpeechGraphNode unplaced = graph.AddLine("Unplaced");
            graph.Connect(entry, line);
            graph.Connect(line, unplaced);

            Dialogue dialogue = graph.SaveAndImport();
            DialogueNode importedLine = dialogue.GetNodeFromID(line.ID.ToString());
            DialogueNode importedUnplaced = dialogue.GetNodeFromID(unplaced.ID.ToString());

            Assert.AreEqual(SpeakerType.PlayerSpeaker, importedLine.GetSpeakerType());
            Assert.AreEqual((2, 1), (importedLine.GetNodeDepth(), importedLine.GetNodeBreadth()));
            CollectionAssert.AreEqual(new[] { $"{_graphName}.2.1.Speaker", $"{_graphName}.2.1.Text" }, importedLine.GetLocalizationEntries().Select(entryReference => entryReference.Key));
            Assert.IsTrue(importedUnplaced.GetLocalizationEntries().All(entryReference => string.IsNullOrEmpty(entryReference.Key)), "A line without a depth/breadth shouldn't get a key");
        }

        [Test]
        public void Reimport_AfterGraphEdits_KeepsSubAssetFileIDs()
        {
            SpeechGraphNode kept = graph.AddLine("Kept");
            SpeechGraphNode removed = graph.AddLine("Removed");
            ConditionGraphNode gate = graph.AddCondition();
            graph.Connect(entry, kept);
            graph.Connect(kept, gate);
            graph.Connect(gate, removed);
            graph.SaveAndImport();
            Dictionary<string, long> before = AssetTestUtility.GetSubAssetFileIDs(_graphPath);

            graph.RemoveNode(removed);
            SpeechGraphNode added = graph.AddLine("Added");
            graph.Connect(gate, added);
            graph.SaveAndImport();
            AssetDatabase.ImportAsset(_graphPath, ImportAssetOptions.ForceUpdate);
            Dictionary<string, long> after = AssetTestUtility.GetSubAssetFileIDs(_graphPath);

            foreach (string nodeID in new[] { _graphName, kept.ID.ToString(), gate.ID.ToString() })
            {
                Assert.AreEqual(before[nodeID], after[nodeID], $"fileID changed for '{nodeID}'");
            }
            Assert.IsFalse(after.ContainsKey(removed.ID.ToString()));
            Assert.IsTrue(after.ContainsKey(added.ID.ToString()));
        }
        #endregion
    }
}

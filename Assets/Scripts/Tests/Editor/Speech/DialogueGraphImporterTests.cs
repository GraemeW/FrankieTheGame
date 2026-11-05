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
using Frankie.Stats;
using Frankie.Utils.Localization;

namespace Frankie.Tests.Editor
{
    public class DialogueGraphImporterTests
    {
        // Covers DialogueGraphImporter w/ temp .dialoguegraph files
        //  - Runtime shape: main Dialogue, speech + condition sub-assets keyed by node ID, entry targets, wire order, keys
        //  - Reference stability: sub-asset fileIDs across reimport and graph edits
        // Not covered: localization table contents (importer never writes them)

        // Const Tunables
        private const string _folder = "Assets/_TEMP_DialogueGraphImporterTests_SafeToDelete";
        private const string _predicatePath = _folder + "/Gate.asset";

        // State
        private string graphName; // Deliberately state / not const:  reusing one path warns while the previous test's graph is still loaded
        private string graphPath; // ^- same
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

            graphName = TestContext.CurrentContext.Test.Name;
            graphPath = $"{_folder}/{graphName}.{DialogueGraph.assetExtension}";
            graph = GraphDatabase.CreateGraph<DialogueGraph>(graphPath);
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
        private static string GetIdentity(Object asset) => asset switch
        {
            DialogueNode dialogueNode => dialogueNode.GetNodeID(),
            DialogueConditionNode conditionNode => conditionNode.GetNodeID(),
            _ => asset.name
        };

        private IEnumerable<T> GetSubAssets<T>() where T : Object => AssetDatabase.LoadAllAssetsAtPath(graphPath).OfType<T>();

        private static CharacterProperties FindLocalizedCharacter(out string englishName)
        {
            englishName = "";
            foreach (string guid in AssetDatabase.FindAssets($"t:{nameof(CharacterProperties)}"))
            {
                var character = AssetDatabase.LoadAssetAtPath<CharacterProperties>(AssetDatabase.GUIDToAssetPath(guid));
                if (character == null) { continue; }
                englishName = LocalizationTool.GetEnglishEntry(LocalizationTableType.Core, character.GetLocalizationEntries()[0]);
                if (!string.IsNullOrWhiteSpace(englishName) && englishName != character.name) { return character; }
            }
            return null;
        }
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
            Assert.AreEqual(graphName, dialogue.name);
            CollectionAssert.AreEquivalent(new[] { line.ID.ToString() }, GetSubAssets<DialogueNode>().Select(node => node.GetNodeID()));
            CollectionAssert.AreEquivalent(new[] { gate.ID.ToString() }, GetSubAssets<DialogueConditionNode>().Select(node => node.GetNodeID()));
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

            CollectionAssert.AreEqual(new[] { second.ID.ToString(), first.ID.ToString() }, dialogue.ResolveEntry(Evaluators((predicate, true))).Select(node => node.GetNodeID()));
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

            DialogueAssert.NodeIDs(dialogue.ResolveChildren(importedParent, Evaluators((predicate, true))), gated.ID.ToString());
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
            CollectionAssert.AreEqual(new[] { $"{graphName}.2.1.Speaker", $"{graphName}.2.1.Text" }, importedLine.GetLocalizationEntries().Select(entryReference => entryReference.Key));
            Assert.IsTrue(importedUnplaced.GetLocalizationEntries().All(entryReference => string.IsNullOrEmpty(entryReference.Key)), "A line without a depth/breadth shouldn't get a key");
        }

        [Test]
        public void Import_SubAssetNames_AreReadableLabels()
        {
            SpeechGraphNode line = graph.AddLine("Hello there\nfriend", SpeakerType.AISpeaker, "Bob");
            SpeechGraphNode longLine = graph.AddLine(new string('x', 200), SpeakerType.AISpeaker, "Bob");
            SpeechGraphNode playerLine = graph.AddLine("Hi");
            playerLine.GetNodeOptionByName(SpeechGraphNode.speakerTypeOption).TrySetValue(SpeakerType.PlayerSpeaker);
            ConditionGraphNode gate = graph.AddCondition(new Condition.PredicateWrapper(predicate, true));
            line.Place(0, 1);
            graph.Connect(entry, line);
            graph.Connect(line, gate);
            graph.Connect(gate, playerLine);
            graph.Connect(line, longLine);

            Dialogue dialogue = graph.SaveAndImport();
            string longLabel = dialogue.GetNodeFromID(longLine.ID.ToString()).name;

            Assert.AreEqual("0.1 Bob: Hello there friend", dialogue.GetNodeFromID(line.ID.ToString()).name);
            Assert.AreEqual("-.- Player: Hi", dialogue.GetNodeFromID(playerLine.ID.ToString()).name);
            Assert.LessOrEqual(longLabel.Length, 48, "Long text is clipped");
            StringAssert.EndsWith("\u2026", longLabel);
            Assert.AreEqual($"Condition - {gate.ID}", GetSubAssets<DialogueConditionNode>().Single().name);
        }

        [Test]
        public void Import_CharacterLineWithoutSpeaker_IsLabelledWithEnglishName()
        {
            CharacterProperties character = FindLocalizedCharacter(out string englishName);
            if (character == null) { Assert.Ignore("No CharacterProperties asset with an English display name in the project"); }
            SpeechGraphNode line = graph.AddLine("Hello");
            line.GetNodeOptionByName(SpeechGraphNode.characterOption).TrySetValue(character);
            graph.Connect(entry, line);

            Dialogue dialogue = graph.SaveAndImport();

            Assert.AreEqual($"-.- {englishName}: Hello", dialogue.GetNodeFromID(line.ID.ToString()).name);
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
            Dictionary<string, long> before = AssetTestUtility.GetSubAssetFileIDs(graphPath, GetIdentity);

            graph.RemoveNode(removed);
            SpeechGraphNode added = graph.AddLine("Added");
            graph.Connect(gate, added);
            graph.SaveAndImport();
            AssetDatabase.ImportAsset(graphPath, ImportAssetOptions.ForceUpdate);
            Dictionary<string, long> after = AssetTestUtility.GetSubAssetFileIDs(graphPath, GetIdentity);

            foreach (string nodeID in new[] { graphName, kept.ID.ToString(), gate.ID.ToString() })
            {
                Assert.AreEqual(before[nodeID], after[nodeID], $"fileID changed for '{nodeID}'");
            }
            Assert.IsFalse(after.ContainsKey(removed.ID.ToString()));
            Assert.IsTrue(after.ContainsKey(added.ID.ToString()));
        }
        #endregion
    }
}

using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Unity.GraphToolkit.Editor;
using LowDefMustard.Utils.Tests;
using Frankie.Speech.Editor;

namespace Frankie.Tests.Editor
{
    public class SpeechGraphNodeVariableTests
    {
        // Tested:
        //  - SpeechGraphNode variable ports - one per contiguous placeholder, rebuilt on text change, gap handling
        //  - SpeechGraphNode.StashVariables - a removed placeholder's value returns when it's re-added, incl. after save + reload
        //  - SpeechGraphNode.IsVariablePortName - variable port names only
        // Not Tested:
        //  - DialogueGraph.OnGraphChanged calling StashVariables - fires on the next editor tick only (stash called directly here)
        //  - DialogueGraphValidator warnings - GraphLogger has no public constructor ~ checked manually
        //  - Undo of a text change - headless Graph Toolkit undo crashes the editor ~ checked manually

        // State
        private TempAssetFolder tempFolder;
        private string graphPath;
        private DialogueGraph graph;
        private TestDialogueVariable hero;
        private TestDialogueVariable sidekick;

        #region Setup
        [SetUp]
        public void SetUp()
        {
            tempFolder = new TempAssetFolder(nameof(SpeechGraphNodeVariableTests));
            
            hero = ScriptableObject.CreateInstance<TestDialogueVariable>().Configure("Hero");
            sidekick = ScriptableObject.CreateInstance<TestDialogueVariable>().Configure("Sidekick");
            AssetDatabase.CreateAsset(hero, tempFolder.Path("Hero.asset"));
            AssetDatabase.CreateAsset(sidekick, tempFolder.Path("Sidekick.asset"));

            graphPath = tempFolder.Path($"{TestContext.CurrentContext.Test.Name}.{DialogueGraph.assetExtension}");
            graph = GraphDatabase.CreateGraph<DialogueGraph>(graphPath);
        }

        [TearDown]
        public void TearDown()
        {
            graph = null;
            tempFolder.Dispose();
        }
        #endregion

        #region Helpers
        private static int CountVariablePorts(SpeechGraphNode line)
        {
            int count = 0;
            while (line.GetInputPortByName(SpeechGraphNode.GetVariablePortName(count)) != null) { count++; }
            return count;
        }
        #endregion

        #region Tests
        [Test]
        public void TextChange_RebuildsOnePortPerPlaceholder()
        {
            SpeechGraphNode line = graph.AddLine("No placeholders");
            Assert.AreEqual(0, CountVariablePorts(line));

            line.SetText("{0} meets {1}");
            Assert.AreEqual(2, CountVariablePorts(line));
            Assert.AreEqual("{1}", line.GetInputPortByName(SpeechGraphNode.GetVariablePortName(1)).DisplayName);

            line.SetText("{0} alone");
            Assert.AreEqual(1, CountVariablePorts(line));
        }

        [Test]
        public void Gap_StopsPortsAtFirstMissingIndex()
        {
            SpeechGraphNode line = graph.AddLine("{0} then {2}");
            Assert.AreEqual(1, CountVariablePorts(line));
            Assert.AreEqual(1, line.GetVariableCount());
        }

        [Test]
        public void TextChange_KeepsValuesOfRemainingPorts()
        {
            SpeechGraphNode line = graph.AddLine("{0} meets {1}");
            line.SetVariable(0, hero);
            line.SetText("{0} alone");
            Assert.AreSame(hero, line.GetVariable(0));
        }

        [Test]
        public void StashVariables_RestoresValueWhenPlaceholderReturns()
        {
            SpeechGraphNode line = graph.AddLine("{0} meets {1}");
            line.SetVariable(0, hero);
            line.SetVariable(1, sidekick);
            line.StashVariables();

            line.SetText("{0} alone");
            CollectionAssert.AreEqual(new ScriptableObject[] { hero }, line.GetVariables());
            
            line.SetText("{0} meets {1}");
            CollectionAssert.AreEqual(new ScriptableObject[] { hero, sidekick }, line.GetVariables());
        }

        [Test]
        public void StashVariables_SurvivesSaveAndReload()
        {
            SpeechGraphNode line = graph.AddLine("{0} meets {1}");
            line.SetVariable(1, sidekick);
            line.StashVariables();
            line.SetText("{0} alone");
            GraphDatabase.SaveGraph(graph);

            var reloaded = GraphDatabase.LoadGraphForImporter<DialogueGraph>(graphPath);
            SpeechGraphNode reloadedLine = null;
            foreach (INode node in reloaded.GetNodes()) { if (node is SpeechGraphNode speechNode) { reloadedLine = speechNode; } }
            Assert.IsNotNull(reloadedLine);
            reloadedLine.SetText("{0} meets {1}");

            Assert.AreSame(sidekick, reloadedLine.GetVariable(1));
        }

        [Test]
        public void StashVariables_ClearedValueStaysCleared()
        {
            SpeechGraphNode line = graph.AddLine("{0} meets {1}");
            line.SetVariable(1, sidekick);
            line.StashVariables();
            line.SetVariable(1, null);
            line.StashVariables();

            line.SetText("{0} alone");
            line.SetText("{0} meets {1}");

            Assert.IsNull(line.GetVariable(1));
        }

        [Test]
        public void IsVariablePortName_MatchesVariablePortsOnly()
        {
            // SetUp names the graph asset after the test, and TestCase names hold path-invalid characters
            Assert.IsTrue(SpeechGraphNode.IsVariablePortName(SpeechGraphNode.GetVariablePortName(0)));
            Assert.IsTrue(SpeechGraphNode.IsVariablePortName(SpeechGraphNode.GetVariablePortName(12)));
            foreach (string portName in new[] { "Variable", "Variable-1", "VariableX", "variable0", SpeechGraphNode.inPort, null })
            {
                Assert.IsFalse(SpeechGraphNode.IsVariablePortName(portName), portName ?? "null");
            }
        }
        #endregion
    }
}

using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Unity.GraphToolkit.Editor;
using Unity.Scripting.LifecycleManagement;
using LowDefMustard.Utils;
using Frankie.Speech;
using Frankie.Speech.Editor;

namespace Frankie.Tests.Editor
{
    public class DialogueGraphSkinTests
    {
        // Guards DialogueGraph.uss against Graph Toolkit updates [Graph Toolkit has no public styling API, so we use skin to select its internal part names (classes, element names, types)]
        // Not covered:
        //  - hover / selection states ++ parts this fixture doesn't build
        //  - hovered title width (Graph Toolkit shrinks it inline by the buttons' width) ~ speech titles aren't built under the test runner (stale views)
        //  - header strip sizing ~ under the test runner, later fixtures' node views often miss their title + DefaultColor (check is unreliable)
        //  - surfaces that drift to another colour (e.g. a dropped interior rule) ~ only Graph Toolkit's greys are flagged

        // Const Tunables
        private const string _folder = "Assets/_TEMP_DialogueGraphSkinTests_SafeToDelete";
        private const string _graphPath = _folder + "/SkinFixture.dialoguegraph";
        private const double _timeoutSeconds = 10;

        // Graph Toolkit's own surface colours (RGB), as seen in an unskinned window
        [NoAutoStaticsCleanup] private static readonly HashSet<string> _graphToolkitGreys = new() { "383838", "303030", "2A2A2A", "5F5F5F", "272727", "515151", "282828", "373737", "3F3F3F", "191919" };

        // State
        private EditorWindow window;

        #region Setup
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            DialogueGraphLocalizationSync.suppressed = true; // The fixture window must not write the real Speech table
            Directory.CreateDirectory(_folder);
            BuildFixtureGraph();
            AssetDatabase.OpenAsset(AssetDatabase.LoadMainAssetAtPath(_graphPath));

            yield return WaitUntil(() => FindFixtureWindow() != null);
            window = FindFixtureWindow();
            Assert.IsNotNull(window, "Dialogue graph window didn't open");
            
            // Production applies the skin from a delayCall, which doesn't run reliably while Unity is unfocused
            DialogueGraphWindowSkin.Apply(((IGraphWindow)window).Graph);
            yield return WaitUntil(() => window.rootVisualElement.Query(className: "ge-block-node").ToList().Count == 2 && window.rootVisualElement.Query(className: "ge-node").ToList().All(HasLayout));
            for (int i = 0; i < 5; i++) { yield return null; }
        }

        [TearDown]
        public void TearDown()
        {
            if (window != null) { window.Close(); }
            window = null;

            // Deliberately using file operations (i.e. avoiding AssetDatabase.DeleteAsset): imported dialogues otherwise remove their localization entries on delete
            File.Delete(_graphPath);
            File.Delete(_graphPath + ".meta");
            if (Directory.Exists(_folder) && !Directory.EnumerateFileSystemEntries(_folder).Any())
            {
                Directory.Delete(_folder);
                File.Delete(_folder + ".meta");
            }
            AssetDatabase.Refresh();
            DialogueGraphLocalizationSync.suppressed = false;
        }
        #endregion
        
        #region Tests
        [Test]
        public void Stylesheet_EveryTargetedPart_ExistsInGraphWindow()
        {
            List<VisualElement> elements = window.rootVisualElement.Query<VisualElement>().ToList();
            var classes = new HashSet<string>(elements.SelectMany(element => element.GetClasses()));
            var names = new HashSet<string>(elements.Select(element => element.name).Where(elementName => !string.IsNullOrEmpty(elementName)));
            var types = new HashSet<string>(elements.Select(element => element.GetType().Name));

            var missing = new List<string>();
            foreach (string selector in GetStylesheetSelectors())
            {
                missing.AddRange(Regex.Matches(selector, @"\.([\w-]+)").Select(match => match.Groups[1].Value).Where(className => !classes.Contains(className)).Select(className => "." + className));
                missing.AddRange(Regex.Matches(selector, @"#([\w-]+)").Select(match => match.Groups[1].Value).Where(elementName => !names.Contains(elementName)).Select(elementName => "#" + elementName));
                missing.AddRange(Regex.Matches(selector, @"(?<![\w.#-])([A-Z]\w*)").Select(match => match.Groups[1].Value).Where(typeName => !types.Contains(typeName)));
            }

            Assert.IsEmpty(missing.Distinct(), "DialogueGraph.uss selects parts Graph Toolkit no longer builds");
        }

        [Test]
        public void GraphSurfaces_DoNotFallBackToGraphToolkitGreys()
        {
            VisualElement graphView = window.rootVisualElement.Q(className: "ge-graph-view");
            Assert.IsNotNull(graphView, "Graph view not found");

            List<string> greySurfaces = graphView.Query<VisualElement>().ToList()
                .Where(element => element.resolvedStyle.display == DisplayStyle.Flex && element.resolvedStyle.backgroundColor.a > 0f)
                .Where(element => _graphToolkitGreys.Contains(ColorUtility.ToHtmlStringRGB(element.resolvedStyle.backgroundColor)))
                .Select(Describe)
                .Distinct()
                .ToList();

            Assert.IsEmpty(greySurfaces, "Graph surfaces still use Graph Toolkit's default greys");
        }
        #endregion
        
        #region PrivateHelpers
        private static void BuildFixtureGraph()
        {
            var graph = GraphDatabase.CreateGraph<DialogueGraph>(_graphPath);
            EntryGraphNode entry = graph.AddEntry();
            SpeechGraphNode ai = graph.AddLine("Line one\nLine two", SpeakerType.AISpeaker, "AI");
            SpeechGraphNode player = graph.AddLine("Reply", SpeakerType.PlayerSpeaker, "Player");
            SpeechGraphNode narrator = graph.AddLine("Narration", SpeakerType.NarratorDirection);
            ConditionGraphNode condition = graph.AddCondition(new Condition.PredicateWrapper(null), new Condition.PredicateWrapper(null, true));
            ai.Position = new Vector2(250, 0);
            condition.Position = new Vector2(600, -200);
            player.Position = new Vector2(1000, -200);
            narrator.Position = new Vector2(1000, 200);

            graph.Connect(entry, ai);
            graph.Connect(ai, condition);
            graph.Connect(condition, player);
            graph.Connect(ai, narrator);
            GraphDatabase.SaveGraph(graph);
        }

        private static EditorWindow FindFixtureWindow()
        {
            return Resources.FindObjectsOfTypeAll<EditorWindow>().FirstOrDefault(candidate => candidate is IGraphWindow { Graph: not null } graphWindow && GraphDatabase.GetGraphAssetPath(graphWindow.Graph) == _graphPath);
        }

        private static IEnumerable<string> GetStylesheetSelectors()
        {
            string stylesheet = File.ReadAllText(AssetDatabase.GetAssetPath(DialogueGraphStyleSheets.instance.GetGraphStyleSheet()));
            stylesheet = Regex.Replace(stylesheet, @"/\*.*?\*/", "", RegexOptions.Singleline);
            return Regex.Matches(stylesheet, @"([^{}]+)\{").Select(match => match.Groups[1].Value.Trim());
        }

        private static bool HasLayout(VisualElement element) => !float.IsNaN(element.layout.height) && element.layout.height > 0f;

        private static string Describe(VisualElement element) => element.GetType().Name + (string.IsNullOrEmpty(element.name) ? "" : "#" + element.name) + string.Concat(element.GetClasses().Take(2).Select(className => "." + className));

        private static IEnumerator WaitUntil(System.Func<bool> condition)
        {
            double deadline = EditorApplication.timeSinceStartup + _timeoutSeconds;
            while (!condition() && EditorApplication.timeSinceStartup < deadline) { yield return null; }
        }
        #endregion
    }
}

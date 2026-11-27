using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Unity.GraphToolkit.Editor;
using LowDefMustard.Utils.Editor;
using LowDefMustard.Utils.Tests;
using Frankie.Inventory;
using Frankie.Speech;
using Frankie.Speech.Editor;
using Frankie.Stats;

namespace Frankie.Tests.Editor
{
    public class DialogueVariablePickerTests
    {
        // Tested:
        //  - DialogueVariableCatalog - implementer assets found, non-implementers skipped, grouped by highest implementing type
        //  - DialogueVariableFieldHook.TryGetVariableField - matches the picker button on {n} fields only
        //  - Setting {n} field's value - saved to the speech node's port
        //  - DialogueGraphWindowSkin - adds the Dialogue Variables overlay once (however often it's applied), shown on open
        //  - DialogueVariablePicker - popup content themed as the graph window, fixed size w/ scrolling list, panel in pick mode (None row first, pickable rows)
        // Not Tested:
        //  - DialogueVariablePicker popup open / position, row click picking + close, click interception - needs input, checked manually
        //  - DialogueVariablesPanel rows + drag onto a field - needs input, checked manually

        // Const Tunables
        private const float _timeoutSeconds = 10f;

        // State
        private TempAssetFolder tempFolder;
        private string graphPath; // Deliberately state / not const (see SetUp)
        private EditorWindow window;
        private TestDialogueVariable hero;

        #region Setup
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            DialogueGraphLocalizationSync.suppressed = true;
            tempFolder = new TempAssetFolder(nameof(DialogueVariablePickerTests));
            
            hero = ScriptableObject.CreateInstance<TestDialogueVariable>().Configure("Hero");
            AssetDatabase.CreateAsset(hero, tempFolder.Path("Hero.asset"));
            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<TestPredicate>(), tempFolder.Path("NotAVariable.asset"));

            graphPath = tempFolder.Path($"{TestContext.CurrentContext.Test.Name}.{DialogueGraph.assetExtension}"); // Per test:  reusing one path breaks while the previous graph is still loaded
            var graph = GraphDatabase.CreateGraph<DialogueGraph>(graphPath);
            graph.AddLine("{0} arrives", SpeakerType.AISpeaker, "AI");
            GraphDatabase.SaveGraph(graph);
            AssetDatabase.OpenAsset(AssetDatabase.LoadMainAssetAtPath(graphPath));

            yield return TestWait.Until(() => FindFixtureWindow() != null, _timeoutSeconds);
            window = FindFixtureWindow();
            Assert.IsNotNull(window, "Dialogue graph window didn't open");
            DialogueGraphWindowSkin.Apply(((IGraphWindow)window).Graph);
            yield return TestWait.Until(() => GetVariableFields().Count == 1, _timeoutSeconds);
            yield return TestWait.Frames(5); // Allow Graph Toolkit's warning markers attach ~ closing first will throw inside Graph Toolkit
        }

        [TearDown]
        public void TearDown()
        {
            if (window != null) { window.Close(); }
            window = null;

            tempFolder.Dispose();
            DialogueGraphLocalizationSync.suppressed = false;
        }
        #endregion

        #region Helpers
        private EditorWindow FindFixtureWindow()
        {
            return Resources.FindObjectsOfTypeAll<EditorWindow>().FirstOrDefault(candidate => candidate is IGraphWindow { Graph: not null } graphWindow && GraphDatabase.GetGraphAssetPath(graphWindow.Graph) == graphPath);
        }

        private List<ObjectField> GetObjectFields() => window.rootVisualElement.Query<ObjectField>().ToList();
        private List<ObjectField> GetVariableFields() => GetObjectFields().Where(field => field.objectType == typeof(ScriptableObject)).ToList();
        private static VisualElement GetSelector(VisualElement field) => field.Q(className: DialogueVariableFieldHook.selectorClass);
        private SpeechGraphNode GetLine() => ((IGraphWindow)window).Graph.GetNodes().OfType<SpeechGraphNode>().First();
        #endregion

        #region Tests
        [Test]
        public void Catalog_CollectsImplementersOnly()
        {
            List<DialogueVariableCatalogEntry> entries = DialogueVariableCatalog.Collect();
            Assert.IsTrue(entries.Any(entry => entry.variable == hero && entry.groupLabel == "Test Dialogue Variable"));
            Assert.IsTrue(entries.All(entry => entry.variable is IDialogueVariable), "Only IDialogueVariable assets belong in the catalog");
        }

        [Test]
        public void Catalog_GroupsByHighestImplementingType()
        {
            Assert.AreEqual(typeof(InventoryItem), DialogueVariableCatalog.GetGroupType(typeof(ActionItem)));
            Assert.AreEqual(typeof(CharacterProperties), DialogueVariableCatalog.GetGroupType(typeof(CharacterProperties)));
            CollectionAssert.Contains(DialogueVariableCatalog.GetGroupTypes(), typeof(InventoryItem));
            CollectionAssert.DoesNotContain(DialogueVariableCatalog.GetGroupTypes(), typeof(ActionItem));
        }

        [Test]
        public void FieldHook_MatchesVariableFieldPickerButtonOnly()
        {
            ObjectField variableField = GetVariableFields().Single();
            Assert.IsTrue(DialogueVariableFieldHook.TryGetVariableField(GetSelector(variableField), out ObjectField matched));
            Assert.AreSame(variableField, matched);

            ObjectField characterField = GetObjectFields().Single(field => field.objectType == typeof(CharacterProperties));
            Assert.IsFalse(DialogueVariableFieldHook.TryGetVariableField(GetSelector(characterField), out _), "The Character option keeps Unity's picker");
            Assert.IsFalse(DialogueVariableFieldHook.TryGetVariableField(variableField, out _), "Only the picker button opens the dialogue variable picker");
        }

        [Test]
        public void Skin_AddsVariablesOverlayOnce()
        {
            DialogueGraphWindowSkin.Apply(((IGraphWindow)window).Graph);
            Assert.IsTrue(window.TryGetOverlay(DialogueVariablesOverlay.overlayID, out Overlay overlay));
            Assert.AreEqual("Dialogue Variables", overlay.displayName);
            Assert.AreEqual(1, window.overlayCanvas.overlays.Count(candidate => candidate is DialogueVariablesOverlay));
            Assert.AreEqual(DisplayStyle.Flex, overlay.rootVisualElement.resolvedStyle.display, "Overlays added from code stay hidden until re-shown");
        }

        [UnityTest]
        public IEnumerator SettingFieldValue_SavesToSpeechNodePort()
        {
            GetVariableFields().Single().value = hero;
            yield return TestWait.Until(() => GetLine().GetVariable(0) == hero, _timeoutSeconds);
            Assert.AreSame(hero, GetLine().GetVariable(0));
        }

        [Test]
        public void Picker_CreateGUI_UsesDialogueGraphTheme()
        {
            VisualElement root = new DialogueVariablePicker(_ => { }, 0f).CreateGUI();
            Assert.IsTrue(root.ClassListContains(StandardBrownTheme.themeClass));
            Assert.IsTrue(root.ClassListContains(DialogueGraphWindowSkin.windowClass));
            Assert.IsTrue(root.styleSheets.Contains(DialogueGraphStyleSheets.instance.GetThemeStyleSheet()));
            Assert.IsTrue(root.styleSheets.Contains(DialogueGraphStyleSheets.instance.GetGraphStyleSheet()));
            Assert.IsNotNull(root.Q<DialogueVariablesPanel>());
        }

        [UnityTest]
        public IEnumerator Picker_Panel_ListsNoneFirstThenPickableVariables()
        {
            VisualElement root = new DialogueVariablePicker(_ => { }, 0f).CreateGUI();
            window.rootVisualElement.Add(root); // Attach ~ panel builds its list on attach
            try
            {
                yield return null;
                List<VisualElement> rows = root.Query(className: StandardBrownTheme.rowClass).ToList();
                Assert.AreEqual("None", rows.First().Q<Label>().text);
                Assert.IsTrue(rows.Any(row => row.Q<Label>().text == hero.name), "Variable assets list as pickable rows");
            }
            finally { root.RemoveFromHierarchy(); }
        }

        [UnityTest]
        public IEnumerator Picker_AllGroupsUnfolded_KeepsFixedSizeAndScrolls()
        {
            var picker = new DialogueVariablePicker(_ => { }, 0f);
            VisualElement root = picker.CreateGUI();
            window.rootVisualElement.Add(root);
            try
            {
                yield return null;
                root.Query<Foldout>().ForEach(foldout => foldout.value = true);
                yield return TestWait.Frames(3); // Layout settles over a few frames

                ScrollView scrollView = root.Q<ScrollView>();
                Assert.AreEqual(picker.GetWindowSize().y, root.layout.height, 0.5f, "Popup keeps its fixed height");
                Assert.LessOrEqual(scrollView.layout.yMax, root.layout.height + 0.5f, "List stays within the popup");
                Assert.Greater(scrollView.contentContainer.layout.height, scrollView.contentViewport.layout.height, "Unfolded list overflows ~ scrolls instead of growing");
            }
            finally { root.RemoveFromHierarchy(); }
        }
        #endregion
    }
}

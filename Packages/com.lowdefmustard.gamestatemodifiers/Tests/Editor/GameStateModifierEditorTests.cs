using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using LowDefMustard.GameStateModifiers.Editor;

namespace LowDefMustard.GameStateModifiers.Tests.Editor
{
    // Testing Notes:  "headless EditorWindow" technique doesn't apply since this is a custom Editor
    //  - thus, a real Editor instance is created via Editor.CreateEditor
    //  - thus, can rely on OnEnable, RegisterCallback handlers (e.g. for button clicks), etc. via its CreateInspectorGUI() root 
    // Not Covered:
    //  - RemoveInvalidEntries's click handler: calls EditorUtility.DisplayDialog
    //      - hangs test runner
    //  - MakeOpenSceneButton's click handler / SelectGameObject / DefaultOpenSceneAndAct:
    //      - calls  EditorSceneManager.OpenScene, pops blocking dialogue, unsafe to test in test runner

    public class GameStateModifierEditorTests
    {
        // State
        private TestGameStateModifier asset;
        private UnityEditor.Editor editorInstance;
        private GameStateModifierEditor gameStateModifierEditor;
        private TestHostWindow hostWindow;
        private VisualElement root;

        // Data Structures
        private class TestGameStateModifier : GameStateModifier { }
        private class TestHostWindow : EditorWindow { }
        
        #region Setup
        [SetUp]
        public void SetUp()
        {
            asset = ScriptableObject.CreateInstance<TestGameStateModifier>();
            editorInstance = UnityEditor.Editor.CreateEditor(asset);
            gameStateModifierEditor = editorInstance as GameStateModifierEditor;
            Assert.IsNotNull(gameStateModifierEditor); // sanity check the [CustomEditor] resolved as expected

            root = gameStateModifierEditor.CreateInspectorGUI();

            // Briefly shows a small utility window during the test run - same
            //   established pattern as SaveEditor's PoC, closed in TearDown
            hostWindow = ScriptableObject.CreateInstance<TestHostWindow>();
            hostWindow.rootVisualElement.Add(root);
            hostWindow.ShowUtility(); // attaches the tree to a real panel so ClickEvent dispatch actually fires
        }

        [TearDown]
        public void TearDown()
        {
            hostWindow.Close();
            Object.DestroyImmediate(editorInstance);
            Object.DestroyImmediate(asset);
        }
        #endregion

        #region PrivateMethods
        private static void SimulateClick(VisualElement element)
        {
            using ClickEvent clickEvent = ClickEvent.GetPooled();
            clickEvent.target = element;
            element.SendEvent(clickEvent);
        }
        
        private static VisualElement GetHeaderSection(VisualElement root) => root.Children().First(c => c.Q<HelpBox>() != null);
        private static VisualElement GetModifierHandlerListSection(VisualElement root) => root.Children().First(c => c.Q<Foldout>() != null);

        private VisualElement RebuildAndReattach()
        {
            VisualElement freshRoot = gameStateModifierEditor.CreateInspectorGUI();
            hostWindow.rootVisualElement.Clear();
            hostWindow.rootVisualElement.Add(freshRoot);
            return freshRoot;
        }
        #endregion

        #region Structure
        [Test]
        public void CreateInspectorGUI_ExcludesScriptAndHandlerDataDefaultPropertyFields()
        {
            string handlerDataRef = GameStateModifier.GetGameStateModifierHandlerDataRef();

            bool anyExcluded = root.Children().OfType<PropertyField>().Any(field => field.bindingPath == "m_Script" || field.bindingPath == handlerDataRef);

            Assert.IsFalse(anyExcluded);
        }

        [Test]
        public void CreateInspectorGUI_NoEntries_ShowsEmptyLabel_FoldoutShowsZeroCount()
        {
            VisualElement listSection = GetModifierHandlerListSection(root);
            var foldout = listSection.Q<Foldout>();

            Assert.AreEqual("Linked Handlers (0)", foldout.text);
            VisualElement listItemsContainer = foldout[0];
            Assert.AreEqual(1, listItemsContainer.childCount);
            Assert.IsInstanceOf<Label>(listItemsContainer[0]); // the "no entries" label, not an entry
        }

        [Test]
        public void CreateInspectorGUI_ToggleButton_InitiallyLocked()
        {
            VisualElement headerSection = GetHeaderSection(root);
            var toggleButton = headerSection.Q<Button>();
            var helpBox = headerSection.Q<HelpBox>();

            Assert.AreEqual(HelpBoxMessageType.Info, helpBox.messageType); // locked state uses Info, unlocked uses Warning
            Assert.IsNotNull(toggleButton);
        }
        #endregion

        #region ToggleEditing
        [Test]
        public void ToggleEditing_Click_SwitchesHelpBoxToWarning()
        {
            VisualElement headerSection = GetHeaderSection(root);
            var toggleButton = headerSection.Q<Button>();
            var helpBox = headerSection.Q<HelpBox>();

            SimulateClick(toggleButton);

            Assert.AreEqual(HelpBoxMessageType.Warning, helpBox.messageType);
        }

        [Test]
        public void ToggleEditing_ClickTwice_ReturnsToLockedState()
        {
            VisualElement headerSection = GetHeaderSection(root);
            var toggleButton = headerSection.Q<Button>();
            var helpBox = headerSection.Q<HelpBox>();

            SimulateClick(toggleButton);
            SimulateClick(toggleButton);

            Assert.AreEqual(HelpBoxMessageType.Info, helpBox.messageType);
        }

        [Test]
        public void ToggleEditing_Click_EnablesAddRemoveButtons()
        {
            VisualElement listSection = GetModifierHandlerListSection(root);
            VisualElement headerSection = GetHeaderSection(root);
            (Button addButton, Button removeButton) = GetAddRemoveButtons(listSection);
            Assert.IsFalse(addButton.enabledSelf); // sanity check on the initial locked state
            Assert.IsFalse(removeButton.enabledSelf);

            SimulateClick(headerSection.Q<Button>());

            Assert.IsTrue(addButton.enabledSelf);
            Assert.IsTrue(removeButton.enabledSelf);
        }
        #endregion

        #region AddRemoveButtons
        private static (Button add, Button remove) GetAddRemoveButtons(VisualElement listSection)
        {
            VisualElement row = listSection.Children().First(c => c.userData is System.ValueTuple<Button, Button>);
            (Button add, Button remove) = ((Button, Button))row.userData;
            return (add, remove);
        }

        [Test]
        public void AddEntryButton_WhenEditingDisabled_Click_DoesNothing()
        {
            VisualElement listSection = GetModifierHandlerListSection(root);
            (Button addButton, _) = GetAddRemoveButtons(listSection);

            SimulateClick(addButton);

            Assert.AreEqual(0, asset.gameStateModifierHandlerData.Count);
        }

        [Test]
        public void AddEntryButton_WhenEditingEnabled_Click_AddsEmptyEntry()
        {
            VisualElement headerSection = GetHeaderSection(root);
            SimulateClick(headerSection.Q<Button>()); // enable editing first
            VisualElement listSection = GetModifierHandlerListSection(root);
            (Button addButton, _) = GetAddRemoveButtons(listSection);

            SimulateClick(addButton);

            Assert.AreEqual(1, asset.gameStateModifierHandlerData.Count);
            Assert.AreEqual("", asset.gameStateModifierHandlerData[0].zoneName);
        }

        [Test]
        public void RemoveEntryButton_WhenEditingDisabled_Click_DoesNothing()
        {
            asset.gameStateModifierHandlerData.Add(new ZoneToGameObjectLinkData("Zone", "Object", "", "guid-1"));
            editorInstance.serializedObject.Update();
            VisualElement freshRoot = RebuildAndReattach();
            VisualElement listSection = GetModifierHandlerListSection(freshRoot);
            (_, Button removeButton) = GetAddRemoveButtons(listSection);

            SimulateClick(removeButton);

            Assert.AreEqual(1, asset.gameStateModifierHandlerData.Count);
        }

        [Test]
        public void RemoveEntryButton_WhenEditingEnabled_WithEntries_Click_RemovesLastEntry()
        {
            asset.gameStateModifierHandlerData.Add(new ZoneToGameObjectLinkData("Zone", "Object", "", "guid-1"));
            editorInstance.serializedObject.Update();
            VisualElement freshRoot = RebuildAndReattach();
            VisualElement headerSection = GetHeaderSection(freshRoot);
            SimulateClick(headerSection.Q<Button>()); // enable editing
            VisualElement listSection = GetModifierHandlerListSection(freshRoot);
            (_, Button removeButton) = GetAddRemoveButtons(listSection);

            SimulateClick(removeButton);

            Assert.AreEqual(0, asset.gameStateModifierHandlerData.Count);
        }

        [Test]
        public void RemoveEntryButton_WhenEditingEnabled_NoEntries_Click_DoesNotThrow()
        {
            VisualElement headerSection = GetHeaderSection(root);
            SimulateClick(headerSection.Q<Button>());
            VisualElement listSection = GetModifierHandlerListSection(root);
            (_, Button removeButton) = GetAddRemoveButtons(listSection);

            Assert.DoesNotThrow(() => SimulateClick(removeButton));
            Assert.AreEqual(0, asset.gameStateModifierHandlerData.Count);
        }
        #endregion

        #region MakeEntry
        [Test]
        public void MakeEntry_BlankZoneOrObjectName_ShowsGenericEntryIndexLabel()
        {
            asset.gameStateModifierHandlerData.Add(new ZoneToGameObjectLinkData("", "", "", "guid-1"));
            editorInstance.serializedObject.Update();
            VisualElement freshRoot = RebuildAndReattach();
            VisualElement listSection = GetModifierHandlerListSection(freshRoot);
            Foldout foldout = listSection.Q<Foldout>();
            VisualElement entryBox = foldout[0][0]; // listItemsContainer's first child is the one entry's box

            VisualElement entryHeaderRow = entryBox[0]; // entryBox[1] is fieldsContainer - scope queries to entryHeaderRow to avoid picking up lazily built internal Labels/Buttons 
            Label displayNameLabel = entryHeaderRow.Query<Label>().Build().ToList()[1]; // [0] is the "[0]" index label, [1] is the display name
            Assert.AreEqual("Entry 0", displayNameLabel.text);
        }

        [Test]
        public void MakeEntry_ZoneAndObjectNamePresent_NoParent_ShowsComposedDisplayName()
        {
            asset.gameStateModifierHandlerData.Add(new ZoneToGameObjectLinkData("MyZone", "MyObject", "", "guid-1"));
            editorInstance.serializedObject.Update();
            VisualElement freshRoot = RebuildAndReattach();
            VisualElement listSection = GetModifierHandlerListSection(freshRoot);
            VisualElement entryBox = listSection.Q<Foldout>()[0][0];

            VisualElement entryHeaderRow = entryBox[0]; // scoped to avoid PropertyField-internal buttons, see above
            Label displayNameLabel = entryHeaderRow.Query<Label>().Build().ToList()[1];
            Assert.AreEqual("MyZone/MyObject", displayNameLabel.text);
        }

        [Test]
        public void MakeEntry_ZoneAndObjectNamePresent_WithParent_ShowsComposedDisplayNameWithParentStem()
        {
            asset.gameStateModifierHandlerData.Add(new ZoneToGameObjectLinkData("MyZone", "MyObject", "MyParent", "guid-1"));
            editorInstance.serializedObject.Update();
            VisualElement freshRoot = RebuildAndReattach();
            VisualElement listSection = GetModifierHandlerListSection(freshRoot);
            VisualElement entryBox = listSection.Q<Foldout>()[0][0];

            VisualElement entryHeaderRow = entryBox[0]; // scoped to avoid PropertyField-internal buttons, see above
            Label displayNameLabel = entryHeaderRow.Query<Label>().Build().ToList()[1];
            Assert.AreEqual("MyZone/MyParent.MyObject", displayNameLabel.text);
        }

        [Test]
        public void MakeEntry_OpenSceneButton_DisabledWhenZoneNameBlank()
        {
            asset.gameStateModifierHandlerData.Add(new ZoneToGameObjectLinkData("", "MyObject", "", "guid-1"));
            editorInstance.serializedObject.Update();
            VisualElement freshRoot = RebuildAndReattach();
            VisualElement listSection = GetModifierHandlerListSection(freshRoot);
            VisualElement entryBox = listSection.Q<Foldout>()[0][0];
            VisualElement entryHeaderRow = entryBox[0]; // scoped to avoid PropertyField-internal buttons, see above
            Button openButton = entryHeaderRow.Query<Button>().Build().ToList()[0]; // "Open & Select"

            Assert.IsFalse(openButton.enabledSelf); // never click this - testing notes
        }

        [Test]
        public void MakeEntry_OpenSceneButton_EnabledWhenZoneAndObjectNamePresent()
        {
            asset.gameStateModifierHandlerData.Add(new ZoneToGameObjectLinkData("MyZone", "MyObject", "", "guid-1"));
            editorInstance.serializedObject.Update();
            VisualElement freshRoot = RebuildAndReattach();
            VisualElement listSection = GetModifierHandlerListSection(freshRoot);
            VisualElement entryBox = listSection.Q<Foldout>()[0][0];
            VisualElement entryHeaderRow = entryBox[0];
            Button openButton = entryHeaderRow.Query<Button>().Build().ToList()[0];

            Assert.IsTrue(openButton.enabledSelf); // never click this - testing notes
        }
        #endregion

        #region DeleteButton
        [Test]
        public void DeleteButton_Click_RemovesThatEntry()
        {
            asset.gameStateModifierHandlerData.Add(new ZoneToGameObjectLinkData("ZoneA", "ObjectA", "", "guid-1"));
            asset.gameStateModifierHandlerData.Add(new ZoneToGameObjectLinkData("ZoneB", "ObjectB", "", "guid-2"));
            editorInstance.serializedObject.Update();
            VisualElement freshRoot = RebuildAndReattach();
            VisualElement listSection = GetModifierHandlerListSection(freshRoot);
            VisualElement firstEntryBox = listSection.Q<Foldout>()[0][0];
            VisualElement entryHeaderRow = firstEntryBox[0]; // scoped to avoid PropertyField-internal buttons, see above
            Button deleteButton = entryHeaderRow.Query<Button>().Build().ToList()[1]; // delete
            deleteButton.SetEnabled(true); // MakeDeleteButton constructs as disabled by default

            SimulateClick(deleteButton);

            Assert.AreEqual(1, asset.gameStateModifierHandlerData.Count);
            Assert.AreEqual("ZoneB", asset.gameStateModifierHandlerData[0].zoneName); // surviving entry
        }
        #endregion
    }
}

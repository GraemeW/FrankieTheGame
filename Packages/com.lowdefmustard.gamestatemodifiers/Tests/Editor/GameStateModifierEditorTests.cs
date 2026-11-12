using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using LowDefMustard.GameStateModifiers.Editor;
using LowDefMustard.Utils.Tests;

namespace LowDefMustard.GameStateModifiers.Tests.Editor
{
    // Testing Notes:  "headless EditorWindow" technique doesn't apply since this is a custom Editor
    //  - thus, a real Editor instance is created via Editor.CreateEditor
    //  - thus, can rely on OnEnable, RegisterCallback handlers (e.g. for button clicks), etc. via its CreateInspectorGUI() root 
    // Not Covered:
    //  - RemoveInvalidEntries's click handler: calls EditorUtility.DisplayDialog
    //      - hangs test runner
    //  - MakeOpenSceneButton's click handler / SelectGameObject / DefaultOpenSceneAndAct (IsNameMatch fallback filter is covered):
    //      - calls  EditorSceneManager.OpenScene, pops blocking dialogue, unsafe to test in test runner

    public class GameStateModifierEditorTests
    {
        // State
        private TestGameStateModifier asset;
        private UnityEditor.Editor editorInstance;
        private GameStateModifierEditor gameStateModifierEditor;
        private EditorWindow window;
        private VisualElement root;
        private readonly TestObjectTracker tracker = new();

        // Const
        private const string _handlerTypeA = "TestHandlerTypeA";
        private const string _handlerTypeB = "TestHandlerTypeB";
        private static readonly string[] _testHandlerTypeLabels = { _handlerTypeA, _handlerTypeB };

        #region Setup
        [SetUp]
        public void SetUp()
        {
            asset = ScriptableObject.CreateInstance<TestGameStateModifier>();
            editorInstance = UnityEditor.Editor.CreateEditor(asset);
            gameStateModifierEditor = editorInstance as GameStateModifierEditor;
            Assert.IsNotNull(gameStateModifierEditor); // sanity check the [CustomEditor] resolved as expected

            root = gameStateModifierEditor.CreateInspectorGUI();
            
            window = HeadlessEditorWindow.CreateOffscreenWindow(400f, 600f); // attach to off-screen panel to ensure ClickEvent dispatch fires
            window.rootVisualElement.Add(root);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (string handlerTypeLabel in _testHandlerTypeLabels) { SessionState.EraseBool(GameStateModifierEditor.groupFoldoutStateKeyStem + handlerTypeLabel); }
            HeadlessEditorWindow.Close(window);
            Object.DestroyImmediate(editorInstance);
            Object.DestroyImmediate(asset);
            tracker.DestroyAll();
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

        private static List<VisualElement> GetEntryBoxes(VisualElement listSection) => listSection.Query<VisualElement>(className: GameStateModifierEditor.entryBoxClass).ToList();
        private static List<Foldout> GetGroupFoldouts(VisualElement listSection) => listSection.Query<Foldout>(className: GameStateModifierEditor.groupFoldoutClass).ToList();

        private VisualElement RebuildWithEntries(params LinkedHandlerRecord[] entries)
        {
            asset.linkedHandlers.AddRange(entries);
            editorInstance.serializedObject.Update();
            return RebuildAndReattach();
        }

        private VisualElement RebuildAndReattach()
        {
            VisualElement freshRoot = gameStateModifierEditor.CreateInspectorGUI();
            window.rootVisualElement.Clear();
            window.rootVisualElement.Add(freshRoot);
            return freshRoot;
        }
        #endregion

        #region Structure
        [Test]
        public void CreateInspectorGUI_ExcludesScriptAndHandlerDataDefaultPropertyFields()
        {
            string handlerDataRef = GameStateModifier.GetLinkedHandlersRef();

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

            Assert.AreEqual(0, asset.linkedHandlers.Count);
        }

        [Test]
        public void AddEntryButton_WhenEditingEnabled_Click_AddsEmptyEntry()
        {
            VisualElement headerSection = GetHeaderSection(root);
            SimulateClick(headerSection.Q<Button>()); // enable editing first
            VisualElement listSection = GetModifierHandlerListSection(root);
            (Button addButton, _) = GetAddRemoveButtons(listSection);

            SimulateClick(addButton);

            Assert.AreEqual(1, asset.linkedHandlers.Count);
            Assert.AreEqual("", asset.linkedHandlers[0].zoneName);
        }

        [Test]
        public void RemoveEntryButton_WhenEditingDisabled_Click_DoesNothing()
        {
            asset.linkedHandlers.Add(new LinkedHandlerRecord("Zone", "Object", "", "guid-1"));
            editorInstance.serializedObject.Update();
            VisualElement freshRoot = RebuildAndReattach();
            VisualElement listSection = GetModifierHandlerListSection(freshRoot);
            (_, Button removeButton) = GetAddRemoveButtons(listSection);

            SimulateClick(removeButton);

            Assert.AreEqual(1, asset.linkedHandlers.Count);
        }

        [Test]
        public void RemoveEntryButton_WhenEditingEnabled_WithEntries_Click_RemovesLastEntry()
        {
            asset.linkedHandlers.Add(new LinkedHandlerRecord("Zone", "Object", "", "guid-1"));
            editorInstance.serializedObject.Update();
            VisualElement freshRoot = RebuildAndReattach();
            VisualElement headerSection = GetHeaderSection(freshRoot);
            SimulateClick(headerSection.Q<Button>()); // enable editing
            VisualElement listSection = GetModifierHandlerListSection(freshRoot);
            (_, Button removeButton) = GetAddRemoveButtons(listSection);

            SimulateClick(removeButton);

            Assert.AreEqual(0, asset.linkedHandlers.Count);
        }

        [Test]
        public void RemoveEntryButton_WhenEditingEnabled_NoEntries_Click_DoesNotThrow()
        {
            VisualElement headerSection = GetHeaderSection(root);
            SimulateClick(headerSection.Q<Button>());
            VisualElement listSection = GetModifierHandlerListSection(root);
            (_, Button removeButton) = GetAddRemoveButtons(listSection);

            Assert.DoesNotThrow(() => SimulateClick(removeButton));
            Assert.AreEqual(0, asset.linkedHandlers.Count);
        }
        #endregion

        #region MakeEntry
        [Test]
        public void MakeEntry_BlankZoneOrObjectName_ShowsGenericEntryIndexLabel()
        {
            asset.linkedHandlers.Add(new LinkedHandlerRecord("", "", "", "guid-1"));
            editorInstance.serializedObject.Update();
            VisualElement freshRoot = RebuildAndReattach();
            VisualElement listSection = GetModifierHandlerListSection(freshRoot);
            VisualElement entryBox = GetEntryBoxes(listSection)[0];

            VisualElement entryHeaderRow = entryBox[0]; // entryBox[1] is fieldsContainer - scope queries to entryHeaderRow to avoid picking up lazily built internal Labels/Buttons 
            Label displayNameLabel = entryHeaderRow.Query<Label>().Build().ToList()[1]; // [0] is the "[0]" index label, [1] is the display name
            Assert.AreEqual("Entry 0", displayNameLabel.text);
        }

        [Test]
        public void MakeEntry_ZoneAndObjectNamePresent_NoParent_ShowsComposedDisplayName()
        {
            asset.linkedHandlers.Add(new LinkedHandlerRecord("MyZone", "MyObject", "", "guid-1"));
            editorInstance.serializedObject.Update();
            VisualElement freshRoot = RebuildAndReattach();
            VisualElement listSection = GetModifierHandlerListSection(freshRoot);
            VisualElement entryBox = GetEntryBoxes(listSection)[0];

            VisualElement entryHeaderRow = entryBox[0]; // scoped to avoid PropertyField-internal buttons, see above
            Label displayNameLabel = entryHeaderRow.Query<Label>().Build().ToList()[1];
            Assert.AreEqual("MyZone/MyObject", displayNameLabel.text);
        }

        [Test]
        public void MakeEntry_ZoneAndObjectNamePresent_WithParent_ShowsComposedDisplayNameWithParentStem()
        {
            asset.linkedHandlers.Add(new LinkedHandlerRecord("MyZone", "MyObject", "MyParent", "guid-1"));
            editorInstance.serializedObject.Update();
            VisualElement freshRoot = RebuildAndReattach();
            VisualElement listSection = GetModifierHandlerListSection(freshRoot);
            VisualElement entryBox = GetEntryBoxes(listSection)[0];

            VisualElement entryHeaderRow = entryBox[0]; // scoped to avoid PropertyField-internal buttons, see above
            Label displayNameLabel = entryHeaderRow.Query<Label>().Build().ToList()[1];
            Assert.AreEqual("MyZone/MyParent.MyObject", displayNameLabel.text);
        }

        [Test]
        public void MakeEntry_OpenSceneButton_DisabledWhenZoneNameBlank()
        {
            asset.linkedHandlers.Add(new LinkedHandlerRecord("", "MyObject", "", "guid-1"));
            editorInstance.serializedObject.Update();
            VisualElement freshRoot = RebuildAndReattach();
            VisualElement listSection = GetModifierHandlerListSection(freshRoot);
            VisualElement entryBox = GetEntryBoxes(listSection)[0];
            VisualElement entryHeaderRow = entryBox[0]; // scoped to avoid PropertyField-internal buttons, see above
            Button openButton = entryHeaderRow.Query<Button>().Build().ToList()[0]; // "Open & Select"

            Assert.IsFalse(openButton.enabledSelf); // never click this - testing notes
        }

        [Test]
        public void MakeEntry_OpenSceneButton_EnabledWhenZoneAndObjectNamePresent()
        {
            asset.linkedHandlers.Add(new LinkedHandlerRecord("MyZone", "MyObject", "", "guid-1"));
            editorInstance.serializedObject.Update();
            VisualElement freshRoot = RebuildAndReattach();
            VisualElement listSection = GetModifierHandlerListSection(freshRoot);
            VisualElement entryBox = GetEntryBoxes(listSection)[0];
            VisualElement entryHeaderRow = entryBox[0];
            Button openButton = entryHeaderRow.Query<Button>().Build().ToList()[0];

            Assert.IsTrue(openButton.enabledSelf); // never click this - testing notes
        }
        #endregion

        #region DeleteButton
        [Test]
        public void DeleteButton_Click_RemovesThatEntry()
        {
            asset.linkedHandlers.Add(new LinkedHandlerRecord("ZoneA", "ObjectA", "", "guid-1"));
            asset.linkedHandlers.Add(new LinkedHandlerRecord("ZoneB", "ObjectB", "", "guid-2"));
            editorInstance.serializedObject.Update();
            VisualElement freshRoot = RebuildAndReattach();
            VisualElement listSection = GetModifierHandlerListSection(freshRoot);
            VisualElement firstEntryBox = GetEntryBoxes(listSection)[0];
            VisualElement entryHeaderRow = firstEntryBox[0]; // scoped to avoid PropertyField-internal buttons, see above
            Button deleteButton = entryHeaderRow.Query<Button>().Build().ToList()[1]; // delete
            deleteButton.SetEnabled(true); // MakeDeleteButton constructs as disabled by default

            SimulateClick(deleteButton);

            Assert.AreEqual(1, asset.linkedHandlers.Count);
            Assert.AreEqual("ZoneB", asset.linkedHandlers[0].zoneName); // surviving entry
        }
        #endregion

        #region HandlerGroups
        [Test]
        public void HandlerGroups_MixedTypes_OneFoldoutPerType_OrderedByName_UntypedLast()
        {
            VisualElement freshRoot = RebuildWithEntries(
                new LinkedHandlerRecord("Zone", "Untyped", "", "guid-1"),
                new LinkedHandlerRecord("Zone", "B1", "", "guid-2", _handlerTypeB),
                new LinkedHandlerRecord("Zone", "A1", "", "guid-3", _handlerTypeA),
                new LinkedHandlerRecord("Zone", "B2", "", "guid-4", _handlerTypeB));

            List<Foldout> groupFoldouts = GetGroupFoldouts(GetModifierHandlerListSection(freshRoot));

            CollectionAssert.AreEqual(new[] { $"{_handlerTypeA} (1)", $"{_handlerTypeB} (2)", "Unknown Handler Type (1)" }, groupFoldouts.Select(foldout => foldout.text).ToList());
        }

        [Test]
        public void HandlerGroups_EntriesKeepTheirListIndex()
        {
            VisualElement freshRoot = RebuildWithEntries(
                new LinkedHandlerRecord("Zone", "B1", "", "guid-1", _handlerTypeB),
                new LinkedHandlerRecord("Zone", "A1", "", "guid-2", _handlerTypeA));

            Foldout firstGroup = GetGroupFoldouts(GetModifierHandlerListSection(freshRoot))[0];
            VisualElement entryHeaderRow = GetEntryBoxes(firstGroup)[0][0];

            Assert.AreEqual("[1]", entryHeaderRow.Query<Label>().Build().ToList()[0].text); // A1 sorts first but is still entry [1] -> implies that delete targets the right element
        }

        [Test]
        public void HandlerGroups_SingleGroup_ExpandedByDefault()
        {
            VisualElement freshRoot = RebuildWithEntries(new LinkedHandlerRecord("Zone", "A1", "", "guid-1", _handlerTypeA));

            Assert.IsTrue(GetGroupFoldouts(GetModifierHandlerListSection(freshRoot)).Single().value);
        }

        [Test]
        public void HandlerGroups_MultipleGroups_CollapsedByDefault_ThenRememberToggledState()
        {
            LinkedHandlerRecord[] entries = { new("Zone", "A1", "", "guid-1", _handlerTypeA), new("Zone", "B1", "", "guid-2", _handlerTypeB) };
            List<Foldout> groupFoldouts = GetGroupFoldouts(GetModifierHandlerListSection(RebuildWithEntries(entries)));
            Assert.IsTrue(groupFoldouts.All(foldout => !foldout.value));

            groupFoldouts[0].value = true;
            List<Foldout> rebuiltFoldouts = GetGroupFoldouts(GetModifierHandlerListSection(RebuildAndReattach()));

            Assert.IsTrue(rebuiltFoldouts[0].value);
            Assert.IsFalse(rebuiltFoldouts[1].value);
        }
        #endregion

        #region IsNameMatch
        [Test]
        public void IsNameMatch_SameNameDifferentParent_ReturnsFalse()
        {
            var parent = tracker.Track(new GameObject("ParentA"));
            var child = tracker.Track(new GameObject("Handler"));
            child.transform.SetParent(parent.transform);
            Assert.IsTrue(GameStateModifierEditor.IsNameMatch(child, "Handler", "ParentA"));
            Assert.IsFalse(GameStateModifierEditor.IsNameMatch(child, "Handler", "ParentB"));
        }

        [Test]
        public void IsNameMatch_NoParent_MatchesEmptyOrNullParentName()
        {
            var handlerObject = tracker.Track(new GameObject("Handler"));
            Assert.IsTrue(GameStateModifierEditor.IsNameMatch(handlerObject, "Handler", ""));
            Assert.IsTrue(GameStateModifierEditor.IsNameMatch(handlerObject, "Handler", null));
            Assert.IsFalse(GameStateModifierEditor.IsNameMatch(handlerObject, "Other", ""));
            Assert.IsFalse(GameStateModifierEditor.IsNameMatch(null, "Handler", ""));
        }
        #endregion
    }
}

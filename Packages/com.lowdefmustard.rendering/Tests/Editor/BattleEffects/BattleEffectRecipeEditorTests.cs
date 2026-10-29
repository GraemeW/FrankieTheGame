using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using LowDefMustard.Rendering.Editor;
using static LowDefMustard.Rendering.Tests.Editor.BattleEffectTestData;
using static LowDefMustard.Rendering.Editor.BattleEffectEntryId;

namespace LowDefMustard.Rendering.Tests.Editor
{
    public class BattleEffectRecipeEditorTests
    {
        // Notes:
        //  - Bound values + tracking callbacks land over the next editor updates ~ tests wait on the expected state
        //  - Test Items (list order):
        //      * 0 = Late (1 -> 2s, fill)
        //      * 1 = Long (0 -> 2s, jitter on)
        //      * 2 = Short (0 -> 1s, fill and outline)
        //      * an instance of a child at 0.5s
        //  - Timeline order:  Short, Long, Child instance, Late

        // State
        private HeadlessEditorWindow window;
        private BattleEffectRecipe recipe;
        private BattleEffectRecipe child;
        private BattleEffectRecipeEditor editor;

        #region Setup
        [SetUp]
        public void SetUp()
        {
            child = ScriptableObject.CreateInstance<BattleEffectRecipe>();
            child.name = "Child";
            child.SetItems(new[] { CreateItem(new BattleEffectCircle(), "{\"startTimeSeconds\":0,\"durationSeconds\":0.25}") });
            recipe = ScriptableObject.CreateInstance<BattleEffectRecipe>();
            recipe.SetItems(new[]
            {
                CreateItem(new BattleEffectCircle(), "{\"label\":\"Late\",\"startTimeSeconds\":1,\"durationSeconds\":1,\"style\":{\"mode\":0}}"),
                CreateItem(new BattleEffectDiamond(), "{\"label\":\"Long\",\"startTimeSeconds\":0,\"durationSeconds\":2,\"jitter\":{\"rate\":4}}"),
                CreateItem(new BattleEffectCircle(), "{\"label\":\"Short\",\"startTimeSeconds\":0,\"durationSeconds\":1,\"style\":{\"mode\":2}}")
            });
            recipe.SetInstances(new[] { new BattleEffectInstance(child, 0.5f, false, false, Vector2.one, Vector2.zero, new BattleEffectColourSwap[0]) });

            window = new HeadlessEditorWindow();
            editor = (BattleEffectRecipeEditor)UnityEditor.Editor.CreateEditor(recipe);
            VisualElement inspector = editor.CreateInspectorGUI();
            window.root.Add(inspector);
            inspector.Bind(editor.serializedObject);
        }

        [TearDown]
        public void TearDown()
        {
            window.Close();
            BattleEffectPreviewVisibility.ShowAll(recipe);
            Undo.ClearUndo(recipe);
            Object.DestroyImmediate(editor);
            Object.DestroyImmediate(recipe);
            Object.DestroyImmediate(child);
            window = null;
        }
        #endregion

        #region PrivateMethods
        private BattleEffectItemElement[] GetItemElements() => window.root.Query<BattleEffectItemElement>().ToList().ToArray();
        private PropertyField FindItemField(int itemIndex, string fieldPath) => window.root.Query<PropertyField>().Where(field => field.bindingPath == $"{BattleEffectRecipe.itemsField}.Array.data[{itemIndex}].{fieldPath}").First();
        private BattleEffectSection FindSection(string title) => window.root.Query<BattleEffectSection>().Where(section => section.text == title).First();
        private static bool IsShown(VisualElement element) => element.style.display != DisplayStyle.None;
        private static string StylePath(string fieldName) => $"{BattleEffectItem.styleField}.{fieldName}";
        private static string RepetitionPath(string fieldName) => $"{BattleEffectItem.repetitionField}.{fieldName}";
        private static string ShadePath(string fieldName) => $"{BattleEffectItem.shadeField}.{fieldName}";
        private IEnumerator WaitForItemField(int itemIndex, string fieldPath) => HeadlessEditorWindow.WaitUntil(() => FindItemField(itemIndex, fieldPath) != null); // Group sections build as their drawers bind

        private static void UndoStep(string expectedName)
        {
            Assert.AreEqual(expectedName, Undo.GetCurrentGroupName(), "the test's own undo step is next");
            Undo.PerformUndo();
        }
        
        private SerializedProperty ItemProperty(int index, string fieldPath = null) => editor.serializedObject.FindProperty($"{BattleEffectRecipe.itemsField}.Array.data[{index}]{(fieldPath == null ? "" : "." + fieldPath)}");
        private SerializedProperty InstanceProperty(int index) => editor.serializedObject.FindProperty($"{BattleEffectRecipe.instancesField}.Array.data[{index}]");
        private SerializedProperty[] Timing(int index) => new[] { ItemProperty(index, BattleEffectItem.startTimeSecondsField), ItemProperty(index, BattleEffectItem.durationSecondsField) };
        private SerializedProperty[] Style(int index) => new[] { ItemProperty(index, BattleEffectItem.styleField) };
        #endregion

        #region EntryList
        [Test]
        public void Entries_AreItemsAndInstancesInTimelineOrder()
        {
            CollectionAssert.AreEqual(new[] { "Short", "Long", "Child", "Late" }, editor.GetEntries().Select(entry => entry.label));
            Assert.IsTrue(editor.GetEntries()[2].isInstance);
            Assert.AreEqual(0.5f, editor.GetEntries()[2].startTime, 1e-5f, "instance offset + its recipe's first start");
        }

        [Test]
        public void ListSelection_SetsTheSelection_IncludingInstances()
        {
            window.root.Q<ListView>().SetSelection(new[] { 0, 2, 3 });

            CollectionAssert.AreEqual(new[] { Item(0), Item(2), Instance(0) }, BattleEffectRecipeSelection.Get(recipe), "Short, Child + Late");
        }

        [UnityTest]
        public IEnumerator Rows_AreTintedByKind_AndBrightenWhenSelected()
        {
            yield return HeadlessEditorWindow.WaitUntil(() => FindRow(3) != null);
            Assert.AreEqual(BattleEffectEntryColours.GetRowColour(false, false, false), FindRow(0).GetColour(), "item:  blue");
            Assert.AreEqual(BattleEffectEntryColours.GetRowColour(true, false, false), FindRow(2).GetColour(), "instance:  purple");

            window.root.Q<ListView>().SetSelection(new[] { 0, 2 });

            Assert.AreEqual(BattleEffectEntryColours.GetRowColour(false, true, false), FindRow(0).GetColour(), "selected item");
            Assert.AreEqual(BattleEffectEntryColours.GetRowColour(true, true, false), FindRow(2).GetColour(), "selected instance");
            Assert.AreEqual(BattleEffectEntryColours.GetRowColour(false, false, false), FindRow(1).GetColour(), "unselected item");
            yield break;
            
            // Local Function
            BattleEffectEntryRow FindRow(int rowIndex) => window.root.Query<BattleEffectEntryRow>().Where(row => row.rowIndex == rowIndex).First();
        }

        [UnityTest]
        public IEnumerator VisibilityButton_TogglesPreviewVisibility_NotTheRecipe()
        {
            yield return HeadlessEditorWindow.WaitUntil(() => FindRow(2) != null);
            int revision = recipe.GetRevision();

            ClickVisibility(FindRow(2));
            yield return HeadlessEditorWindow.WaitUntil(() => BattleEffectPreviewVisibility.IsHidden(recipe, Instance(0)));
            Assert.IsTrue(BattleEffectPreviewVisibility.IsHidden(recipe, Instance(0)), "row 2 = the Child instance");
            Assert.IsTrue(FindRow(2).IsShownHidden());
            Assert.IsFalse(FindRow(0).IsShownHidden());
            Assert.AreEqual(revision, recipe.GetRevision(), "the recipe is untouched");

            ClickVisibility(FindRow(2));
            yield return HeadlessEditorWindow.WaitUntil(() => !BattleEffectPreviewVisibility.IsHidden(recipe, Instance(0)));
            Assert.IsFalse(BattleEffectPreviewVisibility.IsHidden(recipe, Instance(0)));
            Assert.IsFalse(FindRow(2).IsShownHidden());
            yield break;

            
            // Local Functions
            BattleEffectEntryRow FindRow(int rowIndex) => window.root.Query<BattleEffectEntryRow>().Where(row => row.rowIndex == rowIndex).First();
            
            void ClickVisibility(BattleEffectEntryRow row)
            {
                using NavigationSubmitEvent clickEvent = NavigationSubmitEvent.GetPooled();
                Button button = row.Q<Button>();
                clickEvent.target = button;
                button.SendEvent(clickEvent);
            }
        }

        [Test]
        public void DeleteEntry_ShiftsLaterHiddenEntriesDown()
        {
            BattleEffectPreviewVisibility.SetHidden(recipe, Item(0), true);
            BattleEffectPreviewVisibility.SetHidden(recipe, Item(2), true);

            editor.DeleteEntry(Item(1));

            Assert.IsTrue(BattleEffectPreviewVisibility.IsHidden(recipe, Item(0)));
            Assert.IsTrue(BattleEffectPreviewVisibility.IsHidden(recipe, Item(1)), "Short moved up a slot");
            Assert.AreEqual(2, BattleEffectPreviewVisibility.GetHiddenCount(recipe));
        }

        [TestCase(true, false, 0.5f, TestName = "AddFromSelected_On_Add_LatestSelectedStart")]
        [TestCase(true, true, 1f, TestName = "AddFromSelected_On_AddPlus_LatestSelectedEnd")]
        [TestCase(false, false, 1f, TestName = "AddFromSelected_Off_Add_LastEntryStart")]
        [TestCase(false, true, 2f, TestName = "AddFromSelected_Off_AddPlus_LastEntryEnd")]
        public void AddFromSelected_TimesFromTheSelectionWhenOn(bool isFromSelected, bool isAfter, float expectedTime)
        {
            // Selected:  Short (0 -> 1s) + the Child instance (0.5 -> 0.75s);  last entry:  Late (1 -> 2s)
            bool wasFromSelected = BattleEffectRecipeEditor.IsAddingFromSelected();
            try
            {
                BattleEffectRecipeEditor.SetAddingFromSelected(isFromSelected);
                BattleEffectRecipeSelection.Set(recipe, new[] { Item(2), Instance(0) });

                editor.AddItem(isAfter);
                Assert.AreEqual(expectedTime, recipe.GetItems()[3].GetStartTime(), 1e-5f, "item");

                // An add selects (+ is) the new entry:  reset to the original selection + recipe for the instance
                Undo.ClearUndo(recipe);
                editor.DeleteEntry(Item(3));
                BattleEffectRecipeSelection.Set(recipe, new[] { Item(2), Instance(0) });
                editor.AddInstance(isAfter);
                Assert.AreEqual(expectedTime, recipe.GetInstances()[1].GetStartOffset(), 1e-5f, "instance");
            }
            finally { BattleEffectRecipeEditor.SetAddingFromSelected(wasFromSelected); }
        }

        [Test]
        public void AddFromSelected_NothingSelected_UsesTheLastEntry()
        {
            bool wasFromSelected = BattleEffectRecipeEditor.IsAddingFromSelected();
            try
            {
                BattleEffectRecipeEditor.SetAddingFromSelected(true);

                Assert.AreEqual(1f, editor.GetAddTime(false), 1e-5f);
                Assert.AreEqual(2f, editor.GetAddTime(true), 1e-5f);
            }
            finally { BattleEffectRecipeEditor.SetAddingFromSelected(wasFromSelected); }
        }

        [Test]
        public void ShiftSelected_NothingSelected_DoesNothing()
        {
            Assert.AreEqual(0f, editor.ShiftSelected(1f));
            CollectionAssert.AreEqual(new[] { 1f, 0f, 0f }, recipe.GetItems().Select(item => item.GetStartTime()));
        }

        [Test]
        public void ShiftSelected_MovesItemStartsAndInstanceOffsets()
        {
            BattleEffectRecipeSelection.Set(recipe, new[] { Item(1), Instance(0) });

            Assert.AreEqual(0.5f, editor.ShiftSelected(0.5f), 1e-5f);

            CollectionAssert.AreEqual(new[] { 1f, 0.5f, 0f }, recipe.GetItems().Select(item => item.GetStartTime()), "Long moved;  Late + Short untouched");
            Assert.AreEqual(1f, recipe.GetInstances()[0].GetStartOffset(), 1e-5f);
        }

        [Test]
        public void ShiftSelected_Negative_StopsWhenTheEarliestReachesZero()
        {
            // Late (1s) + the instance (offset 0.5s) by -1s:  only -0.5s fits
            BattleEffectRecipeSelection.Set(recipe, new[] { Item(0), Instance(0) });

            Assert.AreEqual(-0.5f, editor.ShiftSelected(-1f), 1e-5f);

            Assert.AreEqual(0.5f, recipe.GetItems()[0].GetStartTime(), 1e-5f);
            Assert.AreEqual(0f, recipe.GetInstances()[0].GetStartOffset(), 1e-5f);
        }

        [Test]
        public void ShiftSelected_Negative_WithOneAtZero_DoesNothing()
        {
            BattleEffectRecipeSelection.Set(recipe, new[] { Item(0), Item(1) });

            Assert.AreEqual(0f, editor.ShiftSelected(-1f));

            CollectionAssert.AreEqual(new[] { 1f, 0f, 0f }, recipe.GetItems().Select(item => item.GetStartTime()));
        }

        [Test]
        public void ShiftSelected_IsUndoable()
        {
            BattleEffectRecipeSelection.Set(recipe, new[] { Item(0) });
            Undo.IncrementCurrentGroup();
            editor.ShiftSelected(0.25f);

            UndoStep(BattleEffectRecipeEditor.shiftUndoName);

            Assert.AreEqual(1f, recipe.GetItems()[0].GetStartTime(), 1e-5f);
        }

        [Test]
        public void AddItem_StartsWithTheLastEntryAndSelectsIt()
        {
            editor.AddItem(false);

            Assert.AreEqual(4, recipe.GetItems().Count);
            Assert.AreEqual(1f, recipe.GetItems()[3].GetStartTime(), 1e-5f, "Late starts at 1s");
            CollectionAssert.AreEqual(new[] { Item(3) }, BattleEffectRecipeSelection.Get(recipe));
        }

        [Test]
        public void AddItemAfter_StartsAsTheLastEntryEnds()
        {
            editor.AddItem(true);

            Assert.AreEqual(2f, recipe.GetItems()[3].GetStartTime(), 1e-5f, "Late ends at 2s");
            Assert.IsNotNull(recipe.GetItems()[3].GetShape(), "a fresh item (w/ its own shape)");
        }

        [Test]
        public void AddInstanceAfter_OffsetToWhereTheLastEntryEnds()
        {
            editor.AddInstance(true);

            Assert.AreEqual(2f, recipe.GetInstances()[1].GetStartOffset(), 1e-5f, "Late ends at 2s");
        }

        [Test]
        public void AddInstance_OffsetToTheLastEntryAndSelectsIt()
        {
            editor.AddInstance(false);

            Assert.AreEqual(2, recipe.GetInstances().Count);
            Assert.AreEqual(1f, recipe.GetInstances()[1].GetStartOffset(), 1e-5f, "Late starts at 1s");
            Assert.IsNull(recipe.GetInstances()[1].GetRecipe());
            CollectionAssert.AreEqual(new[] { Instance(1) }, BattleEffectRecipeSelection.Get(recipe));
            Assert.IsTrue(editor.GetEntries().Any(entry => entry.isInstance && entry.index == 1), "listed w/o a recipe");
        }

        [Test]
        public void DeleteEntry_Item_RemovesItAndShiftsTheSelection()
        {
            BattleEffectRecipeSelection.Set(recipe, new[] { Item(0), Item(2), Instance(0) });

            editor.DeleteEntry(Item(1));

            CollectionAssert.AreEqual(new[] { "Late", "Short" }, recipe.GetItems().Select(item => item.GetLabel()));
            CollectionAssert.AreEqual(new[] { Item(0), Item(1), Instance(0) }, BattleEffectRecipeSelection.Get(recipe), "instances unaffected");
        }

        [Test]
        public void DeleteEntry_Instance_RemovesItAndItsSelection()
        {
            BattleEffectRecipeSelection.Set(recipe, new[] { Item(1), Instance(0) });

            editor.DeleteEntry(Instance(0));

            Assert.IsEmpty(recipe.GetInstances());
            CollectionAssert.AreEqual(new[] { Item(1) }, BattleEffectRecipeSelection.Get(recipe));
            Assert.AreEqual(3, recipe.GetItems().Count);
        }
        #endregion

        #region Undo
        [Test]
        public void AddItem_UndoRedo_RemovesAndRestoresIt()
        {
            Undo.IncrementCurrentGroup();
            editor.AddItem(false);
            int addedRevision = recipe.GetRevision();

            UndoStep(BattleEffectRecipeEditor.addItemUndoName);
            Assert.AreEqual(3, recipe.GetItems().Count, "undo");
            Assert.AreNotEqual(addedRevision, recipe.GetRevision(), "rebakes (the preview picks the undo up)");

            Undo.PerformRedo();
            Assert.AreEqual(4, recipe.GetItems().Count, "redo");
        }

        [Test]
        public void DeleteEntry_Undo_RestoresTheEntry()
        {
            Undo.IncrementCurrentGroup();
            editor.DeleteEntry(Item(1));
            Undo.IncrementCurrentGroup();
            editor.DeleteEntry(Instance(0));

            UndoStep(BattleEffectRecipeEditor.deleteInstanceUndoName);
            Assert.AreEqual(1, recipe.GetInstances().Count, "instance restored");
            UndoStep(BattleEffectRecipeEditor.deleteItemUndoName);
            CollectionAssert.AreEqual(new[] { "Late", "Long", "Short" }, recipe.GetItems().Select(item => item.GetLabel()), "item restored in place");
            Assert.IsInstanceOf<BattleEffectDiamond>(recipe.GetItems()[1].GetShape(), "w/ its shape");
        }

        [Test]
        public void ShapeSwap_Undo_RestoresTheShape()
        {
            Undo.IncrementCurrentGroup();
            BattleEffectShapeTypes.TrySetShape(editor.serializedObject.FindProperty($"{BattleEffectRecipe.itemsField}.Array.data[0].{BattleEffectItem.shapeField}"), "Star");
            Assert.IsInstanceOf<BattleEffectStar>(recipe.GetItems()[0].GetShape());

            UndoStep(BattleEffectShapeTypes.undoName);

            Assert.IsInstanceOf<BattleEffectCircle>(recipe.GetItems()[0].GetShape());
        }
        #endregion

        #region Clipboard
        [Test]
        public void Clipboard_Section_PastesOntoTheSameSectionOnly()
        {
            BattleEffectClipboard.Copy(ItemProperty(1), Timing(1), true);

            Assert.IsFalse(BattleEffectClipboard.CanPaste(ItemProperty(0), Style(0)), "timing doesn't paste onto style");
            Assert.IsFalse(BattleEffectClipboard.Paste(ItemProperty(0), Style(0), "Style"));
            Assert.IsTrue(BattleEffectClipboard.Paste(ItemProperty(0), Timing(0), "Timing"));

            Assert.AreEqual(0f, recipe.GetItems()[0].GetStartTime(), 1e-5f, "Long's timing");
            Assert.AreEqual(2f, recipe.GetItems()[0].GetEndTime(), 1e-5f);
            Assert.AreEqual("Late", recipe.GetItems()[0].GetLabel(), "the rest untouched");
        }

        [Test]
        public void Clipboard_CopyWithoutStartTime_KeepsTheTargetsStart()
        {
            BattleEffectClipboard.Copy(ItemProperty(1), Timing(1), false);

            Assert.IsTrue(BattleEffectClipboard.Paste(ItemProperty(0), Timing(0), "Timing"), "still pastes onto timing");

            Assert.AreEqual(1f, recipe.GetItems()[0].GetStartTime(), 1e-5f, "Late's start kept");
            Assert.AreEqual(3f, recipe.GetItems()[0].GetEndTime(), 1e-5f, "Long's duration");
        }

        [Test]
        public void Clipboard_WholeInstanceWithoutStartTime_KeepsTheTargetsOffset()
        {
            recipe.AddInstance(new BattleEffectInstance(null, 3f, false, false, Vector2.one, Vector2.zero, new BattleEffectColourSwap[0]));
            editor.serializedObject.Update();

            BattleEffectClipboard.Copy(InstanceProperty(0), BattleEffectSection.GetChildren(InstanceProperty(0)), false);
            BattleEffectClipboard.Paste(InstanceProperty(1), BattleEffectSection.GetChildren(InstanceProperty(1)), "Instance");

            Assert.AreSame(child, recipe.GetInstances()[1].GetRecipe());
            Assert.AreEqual(3f, recipe.GetInstances()[1].GetStartOffset(), 1e-5f);
        }

        [Test]
        public void Clipboard_HasStartTime_OnlyForSetsHoldingTheStart()
        {
            Assert.IsTrue(BattleEffectClipboard.HasStartTime(ItemProperty(0), Timing(0)), "timing");
            Assert.IsTrue(BattleEffectClipboard.HasStartTime(ItemProperty(0), BattleEffectSection.GetChildren(ItemProperty(0))), "whole item");
            Assert.IsTrue(BattleEffectClipboard.HasStartTime(InstanceProperty(0), BattleEffectSection.GetChildren(InstanceProperty(0))), "whole instance");
            Assert.IsFalse(BattleEffectClipboard.HasStartTime(ItemProperty(0), Style(0)), "style");
        }

        [Test]
        public void Clipboard_CapturesValuesAtCopy()
        {
            BattleEffectClipboard.Copy(ItemProperty(1), Timing(1), true);
            ItemProperty(1, BattleEffectItem.startTimeSecondsField).floatValue = 5f;
            editor.serializedObject.ApplyModifiedPropertiesWithoutUndo();

            BattleEffectClipboard.Paste(ItemProperty(0), Timing(0), "Timing");

            Assert.AreEqual(0f, recipe.GetItems()[0].GetStartTime(), 1e-5f);
        }

        [Test]
        public void Clipboard_WholeItem_PastesEverythingWithItsOwnShape()
        {
            BattleEffectClipboard.Copy(ItemProperty(1), BattleEffectSection.GetChildren(ItemProperty(1)), true);

            Assert.IsFalse(BattleEffectClipboard.CanPaste(InstanceProperty(0), BattleEffectSection.GetChildren(InstanceProperty(0))), "an item doesn't paste onto an instance");
            Assert.IsTrue(BattleEffectClipboard.Paste(ItemProperty(0), BattleEffectSection.GetChildren(ItemProperty(0)), "Item"));

            Assert.AreEqual("Long", recipe.GetItems()[0].GetLabel());
            Assert.AreEqual(4f, ItemProperty(0, $"{BattleEffectItem.jitterField}.{BattleEffectJitter.rateField}").floatValue, "settings");
            Assert.IsInstanceOf<BattleEffectDiamond>(recipe.GetItems()[0].GetShape());
            Assert.AreNotSame(recipe.GetItems()[1].GetShape(), recipe.GetItems()[0].GetShape(), "not a shared [SerializeReference]");
        }

        [Test]
        public void Clipboard_Shape_PastesOntoAnyShape()
        {
            SerializedProperty sourceShape = ItemProperty(1, BattleEffectItem.shapeField);
            BattleEffectClipboard.Copy(sourceShape, new[] { sourceShape }, false);
            SerializedProperty targetShape = ItemProperty(2, BattleEffectItem.shapeField);

            Assert.IsFalse(BattleEffectClipboard.CanPaste(ItemProperty(2), Timing(2)), "not onto timing");
            Assert.IsTrue(BattleEffectClipboard.Paste(targetShape, new[] { targetShape }, "Shape"), "diamond onto a circle");

            Assert.IsInstanceOf<BattleEffectDiamond>(recipe.GetItems()[2].GetShape());
            Assert.AreNotSame(recipe.GetItems()[1].GetShape(), recipe.GetItems()[2].GetShape());
        }

        [Test]
        public void Clipboard_WholeInstance_PastesItsColourSwaps()
        {
            recipe.SetInstances(new[]
            {
                new BattleEffectInstance(child, 0.5f, true, false, Vector2.one, Vector2.zero, new[] { new BattleEffectColourSwap(Color.white, Color.red), new BattleEffectColourSwap(Color.red, Color.blue) }),
                new BattleEffectInstance(null, 3f, false, false, Vector2.one, Vector2.zero, new BattleEffectColourSwap[0])
            });
            editor.serializedObject.Update();

            BattleEffectClipboard.Copy(InstanceProperty(0), BattleEffectSection.GetChildren(InstanceProperty(0)), true);
            Assert.IsTrue(BattleEffectClipboard.Paste(InstanceProperty(1), BattleEffectSection.GetChildren(InstanceProperty(1)), "Instance"));

            BattleEffectInstance pasted = recipe.GetInstances()[1];
            Assert.AreSame(child, pasted.GetRecipe());
            Assert.AreEqual(0.5f, pasted.GetStartOffset(), 1e-5f);
            SerializedProperty swaps = InstanceProperty(1).FindPropertyRelative(BattleEffectInstance.colourSwapsField);
            Assert.AreEqual(2, swaps.arraySize);
            Assert.AreEqual(Color.blue, (Color)swaps.GetArrayElementAtIndex(1).FindPropertyRelative("to").colorValue);
        }

        [Test]
        public void Clipboard_Paste_IsUndoable()
        {
            BattleEffectClipboard.Copy(ItemProperty(1), Timing(1), true);
            Undo.IncrementCurrentGroup();
            BattleEffectClipboard.Paste(ItemProperty(0), Timing(0), "Timing");

            UndoStep("Paste Timing");

            Assert.AreEqual(1f, recipe.GetItems()[0].GetStartTime(), 1e-5f);
        }
        #endregion

        #region ItemView
        [Test]
        public void NoSelection_ShowsNoItems()
        {
            Assert.IsEmpty(GetItemElements());
        }

        [Test]
        public void Selection_ShowsTheSelectedEntriesInListOrder()
        {
            BattleEffectRecipeSelection.Set(recipe, new[] { Item(0), Item(1), Instance(0) });

            CollectionAssert.AreEqual(new[] { Item(1), Instance(0), Item(0) }, editor.GetShownEntries(), "Long (0s), Child (0.5s), Late (1s)");
            CollectionAssert.AreEqual(new[] { 1, 0 }, GetItemElements().Select(element => element.GetItemIndex()));
            BattleEffectInstanceElement instanceElement = window.root.Q<BattleEffectInstanceElement>();
            Assert.AreEqual(0, instanceElement.GetInstanceIndex());
            StringAssert.StartsWith("Instance:  Child", instanceElement.text);
            Assert.IsNotNull(window.root.Query<PropertyField>().Where(field => field.bindingPath == $"{BattleEffectRecipe.instancesField}.Array.data[0].{BattleEffectInstance.startOffsetSecondsField}").First(), "instance settings");
        }

        [UnityTest]
        public IEnumerator ItemHeader_FollowsLabelAndShapeEdits()
        {
            BattleEffectRecipeSelection.Set(recipe, new[] { Item(0) });
            BattleEffectItemElement element = GetItemElements()[0];
            StringAssert.StartsWith("Late  ~  Circle", element.text);

            ItemProperty(0, BattleEffectItem.labelField).stringValue = "Later";
            editor.serializedObject.ApplyModifiedPropertiesWithoutUndo();
            yield return HeadlessEditorWindow.WaitUntil(() => element.text.StartsWith("Later"));
            StringAssert.StartsWith("Later  ~  Circle", element.text, "label");

            ItemProperty(0, BattleEffectItem.shapeField).managedReferenceValue = new BattleEffectStar();
            editor.serializedObject.ApplyModifiedPropertiesWithoutUndo();
            yield return HeadlessEditorWindow.WaitUntil(() => element.text.Contains("Star"));
            StringAssert.StartsWith("Later  ~  Star", element.text, "shape");
        }

        [UnityTest]
        public IEnumerator Style_OpensRegardlessOfItsSavedFoldState()
        {
            // The hosting PropertyField opens a drawer's foldout from the property's isExpanded (saved per item)
            ItemProperty(0, BattleEffectItem.styleField).isExpanded = false;
            BattleEffectRecipeSelection.Set(recipe, new[] { Item(0) });
            yield return HeadlessEditorWindow.WaitUntil(() => FindSection("Style") != null);
            yield return null;

            Assert.IsTrue(FindSection("Style").value);
        }

        [UnityTest]
        public IEnumerator Style_ShowsOnlyTheOutlineSettingsItDraws()
        {
            BattleEffectRecipeSelection.Set(recipe, new[] { Item(0), Item(2) });
            PropertyField fillThickness = null;
            yield return HeadlessEditorWindow.WaitUntil(() => (fillThickness = FindItemField(0, StylePath(BattleEffectStyle.outlineThicknessEUField))) != null && !IsShown(fillThickness));

            Assert.IsFalse(IsShown(fillThickness), "fill:  no outline thickness");
            Assert.IsFalse(IsShown(FindItemField(0, StylePath(BattleEffectStyle.outlineColourField))), "fill:  no outline colour");
            Assert.IsFalse(IsShown(FindItemField(0, BattleEffectItem.outlineEdgeGradientField)), "fill:  no edge gradient");
            Assert.IsTrue(IsShown(FindItemField(2, StylePath(BattleEffectStyle.outlineThicknessEUField))), "fill and outline:  thickness");
            Assert.IsTrue(IsShown(FindItemField(2, StylePath(BattleEffectStyle.outlineColourField))), "fill and outline:  outline colour");
            Assert.IsTrue(IsShown(FindItemField(2, BattleEffectItem.outlineEdgeGradientField)), "fill and outline:  edge gradient");
        }

        [UnityTest]
        public IEnumerator Style_OutlineFromPalette_SwapsTheOutlineColourForItsStep()
        {
            // Item 2:  fill and outline
            BattleEffectRecipeSelection.Set(recipe, new[] { Item(2) });
            PropertyField outlineColour = null;
            yield return HeadlessEditorWindow.WaitUntil(() => (outlineColour = FindItemField(2, StylePath(BattleEffectStyle.outlineColourField))) != null && IsShown(outlineColour));
            Assert.IsFalse(IsShown(FindItemField(2, StylePath(BattleEffectStyle.outlinePaletteStepField))), "off:  no step");

            editor.serializedObject.FindProperty($"{BattleEffectRecipe.itemsField}.Array.data[2].{StylePath(BattleEffectStyle.isOutlineFromPaletteField)}").boolValue = true;
            editor.serializedObject.ApplyModifiedPropertiesWithoutUndo();
            yield return HeadlessEditorWindow.WaitUntil(() => !IsShown(outlineColour));

            Assert.IsFalse(IsShown(outlineColour), "on:  no outline colour");
            Assert.IsTrue(IsShown(FindItemField(2, StylePath(BattleEffectStyle.outlinePaletteStepField))), "on:  its step");
        }

        [UnityTest]
        public IEnumerator Shade_ShowsOnlyTheStepsItsStyleDraws()
        {
            // Items 0 / 1 / 2:  fill / outline / fill and outline (its outline colour, not from the palette) ~ each w/ its shade on
            for (int index = 0; index < 3; index++) { ItemProperty(index, ShadePath(BattleEffectShade.isEnabledField)).boolValue = true; }
            editor.serializedObject.ApplyModifiedPropertiesWithoutUndo();
            BattleEffectRecipeSelection.Set(recipe, new[] { Item(0), Item(1), Item(2) });

            // (each item's fields build over the next editor updates:  wait for all of them + the last refresh)
            PropertyField fillStep = null, fillStroke = null, outlineFill = null, outlineStroke = null, fillAndOutlineStroke = null;
            yield return HeadlessEditorWindow.WaitUntil(() =>
            {
                fillStep = FindItemField(0, ShadePath(BattleEffectShade.fillPaletteStepField));
                fillStroke = FindItemField(0, ShadePath(BattleEffectShade.outlinePaletteStepField));
                outlineFill = FindItemField(1, ShadePath(BattleEffectShade.fillPaletteStepField));
                outlineStroke = FindItemField(1, ShadePath(BattleEffectShade.outlinePaletteStepField));
                fillAndOutlineStroke = FindItemField(2, ShadePath(BattleEffectShade.outlinePaletteStepField));
                return fillStep != null && fillStroke != null && outlineFill != null && outlineStroke != null && fillAndOutlineStroke != null
                    && IsShown(fillStep) && !IsShown(fillStroke) && !IsShown(outlineFill) && IsShown(outlineStroke) && !IsShown(fillAndOutlineStroke);
            });

            Assert.IsTrue(IsShown(fillStep), "fill:  fill step");
            Assert.IsFalse(IsShown(fillStroke), "fill:  no stroke step");
            Assert.IsFalse(IsShown(outlineFill), "outline:  no fill step");
            Assert.IsTrue(IsShown(outlineStroke), "outline:  stroke step");
            Assert.IsFalse(IsShown(fillAndOutlineStroke), "fill and outline w/ its outline colour:  no stroke step");

            ItemProperty(2, StylePath(BattleEffectStyle.isOutlineFromPaletteField)).boolValue = true;
            editor.serializedObject.ApplyModifiedPropertiesWithoutUndo();
            yield return HeadlessEditorWindow.WaitUntil(() => IsShown(fillAndOutlineStroke));

            Assert.IsTrue(IsShown(fillAndOutlineStroke), "outline from the palette:  stroke step");
        }

        [UnityTest]
        public IEnumerator StyleChange_RefreshesTheOutlineSettings()
        {
            BattleEffectRecipeSelection.Set(recipe, new[] { Item(0) });
            PropertyField thickness = null;
            yield return HeadlessEditorWindow.WaitUntil(() => (thickness = FindItemField(0, StylePath(BattleEffectStyle.outlineThicknessEUField))) != null && !IsShown(thickness));

            editor.serializedObject.FindProperty($"{BattleEffectRecipe.itemsField}.Array.data[0].{StylePath(BattleEffectStyle.modeField)}").intValue = (int)BattleEffectStyleMode.Outline;
            editor.serializedObject.ApplyModifiedPropertiesWithoutUndo();
            yield return HeadlessEditorWindow.WaitUntil(() => IsShown(thickness));

            Assert.IsTrue(IsShown(thickness));
            Assert.IsFalse(IsShown(FindItemField(0, StylePath(BattleEffectStyle.outlineColourField))), "outline draws w/ the colour");
        }

        [UnityTest]
        public IEnumerator OptionalSections_OpenOnlyWhileInUse()
        {
            BattleEffectRecipeSelection.Set(recipe, new[] { Item(1) });
            PropertyField flickerDepth = null;
            yield return HeadlessEditorWindow.WaitUntil(() => (flickerDepth = FindItemField(1, $"{BattleEffectItem.flickerField}.depthFraction")) != null && !IsShown(flickerDepth));

            Assert.IsTrue(FindSection("Jitter").value, "jitter (rate 4) open");
            Assert.IsTrue(FindSection("Jitter").IsEnabled());
            Assert.IsTrue(IsShown(FindItemField(1, $"{BattleEffectItem.jitterField}.amountEU")));
            Assert.IsFalse(FindSection("Flicker").value, "flicker (rate 0) folded");
            Assert.IsFalse(IsShown(flickerDepth), "flicker settings hidden while off");
            Assert.IsTrue(FindSection("Timing").value, "core sections open");
        }

        [TestCase(BattleEffectEasing.Linear, false)]
        [TestCase(BattleEffectEasing.EaseIn, false)]
        [TestCase(BattleEffectEasing.Approach, true)]
        public void Timing_ShowsApproachRatioForApproachOnly(BattleEffectEasing easing, bool isRatioShown)
        {
            editor.serializedObject.FindProperty($"{BattleEffectRecipe.itemsField}.Array.data[1].{BattleEffectItem.easingField}").intValue = (int)easing;
            editor.serializedObject.ApplyModifiedPropertiesWithoutUndo();
            BattleEffectRecipeSelection.Set(recipe, new[] { Item(1) });

            Assert.AreEqual(isRatioShown, IsShown(FindItemField(1, BattleEffectItem.approachRatioField)));
        }

        [UnityTest]
        [TestCase(BattleEffectRepetitionMode.None, BattleEffectWaveform.Trapezoid, false, false, ExpectedResult = null)]
        [TestCase(BattleEffectRepetitionMode.Line, BattleEffectWaveform.Sine, true, false, ExpectedResult = null)]
        [TestCase(BattleEffectRepetitionMode.Cross, BattleEffectWaveform.Trapezoid, true, true, ExpectedResult = null)]
        [TestCase(BattleEffectRepetitionMode.Line, BattleEffectWaveform.Pulse, true, true, ExpectedResult = null)]
        [TestCase(BattleEffectRepetitionMode.Radial, BattleEffectWaveform.Trapezoid, false, false, ExpectedResult = null)]
        [TestCase(BattleEffectRepetitionMode.Grid, BattleEffectWaveform.Trapezoid, false, false, ExpectedResult = null)]
        public IEnumerator Repetition_ShowsCopyWaveForLinesOnly(BattleEffectRepetitionMode repetition, BattleEffectWaveform waveform, bool isWaveShown, bool isPlateauShown)
        {
            editor.serializedObject.FindProperty($"{BattleEffectRecipe.itemsField}.Array.data[1].{RepetitionPath(BattleEffectRepetition.modeField)}").intValue = (int)repetition;
            editor.serializedObject.FindProperty($"{BattleEffectRecipe.itemsField}.Array.data[1].{RepetitionPath(BattleEffectRepetition.waveformField)}").intValue = (int)waveform;
            editor.serializedObject.ApplyModifiedPropertiesWithoutUndo();
            BattleEffectRecipeSelection.Set(recipe, new[] { Item(1) });
            yield return WaitForItemField(1, RepetitionPath(BattleEffectRepetition.wavePlateauFractionField));

            Assert.AreEqual(isWaveShown, IsShown(FindItemField(1, RepetitionPath(BattleEffectRepetition.waveAmplitudeSUField))), "amplitude");
            Assert.AreEqual(isWaveShown, IsShown(FindItemField(1, RepetitionPath(BattleEffectRepetition.waveCopiesPerCycleField))), "copies per cycle");
            Assert.AreEqual(isPlateauShown, IsShown(FindItemField(1, RepetitionPath(BattleEffectRepetition.wavePlateauFractionField))), "plateau (trapezoid / pulse only)");
        }

        [UnityTest]
        [TestCase(false, false, false, ExpectedResult = null)]
        [TestCase(true, false, true, ExpectedResult = null)]
        [TestCase(false, true, true, ExpectedResult = null)]
        public IEnumerator Repetition_ShowsBeatOffsetsSpreadWhenMirrored(bool mirrorX, bool mirrorY, bool isSpreadShown)
        {
            editor.serializedObject.FindProperty($"{BattleEffectRecipe.itemsField}.Array.data[1].{RepetitionPath(BattleEffectRepetition.mirrorXField)}").boolValue = mirrorX;
            editor.serializedObject.FindProperty($"{BattleEffectRecipe.itemsField}.Array.data[1].{RepetitionPath(BattleEffectRepetition.mirrorYField)}").boolValue = mirrorY;
            editor.serializedObject.ApplyModifiedPropertiesWithoutUndo();
            BattleEffectRecipeSelection.Set(recipe, new[] { Item(1) });
            yield return WaitForItemField(1, RepetitionPath(BattleEffectRepetition.beatOffsetsSpreadField));

            Assert.AreEqual(isSpreadShown, IsShown(FindItemField(1, RepetitionPath(BattleEffectRepetition.beatOffsetsSpreadField))));
        }

        [UnityTest]
        [TestCase(BattleEffectRepetitionMode.None, false, false, "Count", ExpectedResult = null)]
        [TestCase(BattleEffectRepetitionMode.Line, true, false, "Count", ExpectedResult = null)]
        [TestCase(BattleEffectRepetitionMode.Radial, true, false, "Count", ExpectedResult = null)]
        [TestCase(BattleEffectRepetitionMode.Grid, true, true, "Count X", ExpectedResult = null)]
        [TestCase(BattleEffectRepetitionMode.StaggeredGrid, true, true, "Count X", ExpectedResult = null)]
        public IEnumerator Repetition_ShowsCountYForGridsOnly(BattleEffectRepetitionMode repetition, bool isCountShown, bool isCountYShown, string countLabel)
        {
            editor.serializedObject.FindProperty($"{BattleEffectRecipe.itemsField}.Array.data[1].{RepetitionPath(BattleEffectRepetition.modeField)}").intValue = (int)repetition;
            editor.serializedObject.ApplyModifiedPropertiesWithoutUndo();
            BattleEffectRecipeSelection.Set(recipe, new[] { Item(1) });
            yield return WaitForItemField(1, RepetitionPath(BattleEffectRepetition.copyScaleRatioField));

            Assert.AreEqual(isCountShown, IsShown(FindItemField(1, RepetitionPath(BattleEffectRepetition.countField + ".x"))), "count");
            Assert.AreEqual(isCountYShown, IsShown(FindItemField(1, RepetitionPath(BattleEffectRepetition.countField + ".y"))), "count y");
            Assert.AreEqual(isCountShown, IsShown(FindItemField(1, RepetitionPath(BattleEffectRepetition.copyScaleRatioField))), "copy scale ratio");
            Assert.AreEqual(countLabel, FindItemField(1, RepetitionPath(BattleEffectRepetition.countField + ".x")).label);
        }

        [UnityTest]
        public IEnumerator RepetitionChange_RelabelsCountForGrids()
        {
            // Group sections are built by their drawers as they bind
            BattleEffectRecipeSelection.Set(recipe, new[] { Item(1) });
            yield return HeadlessEditorWindow.WaitUntil(() => FindItemField(1, RepetitionPath(BattleEffectRepetition.countField + ".x"))?.Q<IntegerField>() != null);
            PropertyField countField = FindItemField(1, RepetitionPath(BattleEffectRepetition.countField + ".x"));

            editor.serializedObject.FindProperty($"{BattleEffectRecipe.itemsField}.Array.data[1].{RepetitionPath(BattleEffectRepetition.modeField)}").intValue = (int)BattleEffectRepetitionMode.Grid;
            editor.serializedObject.ApplyModifiedPropertiesWithoutUndo();
            yield return HeadlessEditorWindow.WaitUntil(() => countField.Q<IntegerField>().label == "Count X");

            Assert.AreEqual("Count X", countField.Q<IntegerField>().label, "the built field relabels");
        }
        #endregion

        #region Entries
        [Test]
        public void GetActiveEntries_ListsTheEntriesRunningAtTheTime()
        {
            CollectionAssert.AreEqual(new[] { Item(2), Item(1), Instance(0) }, BattleEffectRecipeEntries.GetActiveEntries(recipe, 0.6f), "Short, Long + Child (0.5 -> 0.75s), in timeline order");
            CollectionAssert.AreEqual(new[] { Item(1), Item(0) }, BattleEffectRecipeEntries.GetActiveEntries(recipe, 1.5f));
        }

        [Test]
        public void GetActiveEntries_SkipsInstancesWithoutARecipe()
        {
            recipe.SetInstances(new[] { new BattleEffectInstance(null, 0f, false, false, Vector2.one, Vector2.zero, new BattleEffectColourSwap[0]) });

            CollectionAssert.DoesNotContain(BattleEffectRecipeEntries.GetActiveEntries(recipe, 0f), Instance(0));
        }
        #endregion
    }
}

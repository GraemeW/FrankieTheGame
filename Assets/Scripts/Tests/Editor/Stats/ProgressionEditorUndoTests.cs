using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using LowDefMustard.Utils.Tests;
using Frankie.Stats;
using Frankie.Stats.Editor;

namespace Frankie.Tests.Editor
{
    public class ProgressionEditorUndoTests
    {
        // Tested:
        //  - Undo / redo - selected character's card shows the restored value, selection kept on the re-deserialized character
        //  - No progression loaded - undo does nothing;  deleted progression - undo clears the panes
        //  - Unrelated undo / own stat edit - panes not redrawn (cards kept)
        //  - Editable stat fields (stats + initial level) are delayed ~ commit on enter / focus loss, one undo step per edit
        // Not Tested:
        //  - Buttons (reconcile, rebuild charts, remove)
        // Note:  Uses a project CharacterProperties as a read-only key (the progression itself is in memory)

        // State
        private readonly TestObjectTracker tracker = new();
        private ProgressionEditor window;
        private Progression progression;
        private Progression unrelatedProgression;
        private CharacterProperties characterProperties;
        private Stat editedStat;

        #region Setup
        [SetUp]
        public void SetUp()
        {
            characterProperties = AssetDatabase.FindAssets($"t:{nameof(CharacterProperties)}")
                .Select(guid => AssetDatabase.LoadAssetAtPath<CharacterProperties>(AssetDatabase.GUIDToAssetPath(guid)))
                .FirstOrDefault(found => found != null);
            if (characterProperties == null) { Assert.Ignore("No CharacterProperties in the project"); }

            progression = tracker.Create<Progression>("ProgressionEditorUndoTests_Progression");
            progression.AddToProgressionAsset(characterProperties);
            editedStat = progression.GetCharacters()[0].stats.First(progressionStat => progressionStat.stat != Stat.InitialLevel).stat;

            window = HeadlessEditorWindow.ShowOffscreen(ScriptableObject.CreateInstance<ProgressionEditor>(), 800f, 600f);
            if (window.progressionEntries == null) { window.CreateGUI(); }
            window.progression = progression;
            window.RedrawProgression();
            window.progressionEntries?.SetSelection(0);
        }

        [TearDown]
        public void TearDown()
        {
            if (progression != null) { Undo.ClearUndo(progression); }
            if (unrelatedProgression != null) { Undo.ClearUndo(unrelatedProgression); }
            HeadlessEditorWindow.Close(window);
            tracker.DestroyAll();
        }
        #endregion

        #region Helpers
        private float GetDataValue() => progression.GetCharacters()[0].stats.First(progressionStat => progressionStat.stat == editedStat).value;

        private float GetCardValue() => window.characterStatPane.Query<FloatField>().Where(field => field.label == editedStat.ToString()).First().value;

        private void EditStat(float newValue)
        {
            Undo.IncrementCurrentGroup();
            progression.UpdateProgressionAsset(characterProperties, editedStat, newValue);
            Undo.IncrementCurrentGroup();
        }
        #endregion

        #region Tests
        [Test]
        public void Undo_StatEdit_SelectedCardShowsRestoredValue()
        {
            float original = GetDataValue();
            EditStat(original + 42f);

            Undo.PerformUndo();

            Assert.AreEqual(original, GetDataValue());
            Assert.AreEqual(original, GetCardValue());
            Assert.AreSame(progression.GetCharacters()[0], window.selectedCharacters.Single(), "selection follows the re-deserialized character");
            Assert.AreEqual(0, window.progressionEntries.selectedIndex);
        }

        [Test]
        public void Redo_StatEdit_SelectedCardShowsReappliedValue()
        {
            float edited = GetDataValue() + 42f;
            EditStat(edited);
            Undo.PerformUndo();

            Undo.PerformRedo();

            Assert.AreEqual(edited, GetCardValue());
            Assert.AreSame(progression.GetCharacters()[0], window.selectedCharacters.Single());
        }

        [Test]
        public void StatCard_EditableFields_AreDelayed()
        {
            VisualElement card = window.characterStatPane[0];
            var statFields = card.Query<FloatField>().Where(field => !field.isReadOnly).ToList();
            IntegerField initialLevelField = card.Query<IntegerField>().Where(field => field.label == "Initial Level").First();

            Assert.IsNotEmpty(statFields);
            foreach (FloatField statField in statFields) { Assert.IsTrue(statField.isDelayed, $"'{statField.label}' commits per keystroke"); }
            Assert.IsTrue(initialLevelField.isDelayed, "'Initial Level' commits per keystroke");
        }

        [Test]
        public void Undo_UnrelatedObject_KeepsCards()
        {
            VisualElement card = window.characterStatPane[0];
            unrelatedProgression = tracker.Create<Progression>("ProgressionEditorUndoTests_Unrelated");
            Undo.IncrementCurrentGroup();
            Undo.RecordObject(unrelatedProgression, "ProgressionEditorUndoTests Unrelated Edit");
            unrelatedProgression.name = "Edited";
            EditorUtility.SetDirty(unrelatedProgression);
            Undo.IncrementCurrentGroup();

            Undo.PerformUndo();

            Assert.AreSame(card, window.characterStatPane[0], "panes not redrawn");
        }

        [Test]
        public void OnUndoRedoPerformed_AfterOwnStatEdit_KeepsCards()
        {
            VisualElement card = window.characterStatPane[0];
            window.characterStatPane.Query<FloatField>().Where(field => field.label == editedStat.ToString()).First().value = GetDataValue() + 1f; // Typed into the card ~ writes through UpdateStat

            window.OnUndoRedoPerformed();

            Assert.AreSame(card, window.characterStatPane[0], "own edit isn't an undo change");
        }

        [Test]
        public void OnUndoRedoPerformed_NoProgressionLoaded_DoesNothing()
        {
            window.progression = null;

            Assert.DoesNotThrow(() => window.OnUndoRedoPerformed());
            Assert.AreEqual(1, window.selectedCharacters.Count);
        }

        [Test]
        public void OnUndoRedoPerformed_ProgressionDeleted_ClearsPanes()
        {
            Object.DestroyImmediate(progression); // Stands in for deleting the asset ~ the window still holds the reference

            window.OnUndoRedoPerformed();

            Assert.AreEqual(0, window.selectedCharacters.Count);
            Assert.AreEqual(0, window.characterStatPane.childCount);
            Assert.IsNull(window.progressionEntries.itemsSource);
        }
        #endregion
    }
}

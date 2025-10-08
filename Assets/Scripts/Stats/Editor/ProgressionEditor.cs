using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Frankie.Stats.Editor
{
    public class ProgressionEditor : EditorWindow
    {
        // Data State
        private Progression progression;
        
        // UI State
        private readonly List<Progression.ProgressionCharacterClass> selectedCharacterClasses = new List<Progression.ProgressionCharacterClass>();
        
        // UI Cached References
        private ObjectField progressionInput;
        private ListView progressionEntries;
        private ScrollView characterStatPane;

        #region UIToolKitDraw
        [MenuItem("Tools/Progression Editor")]
        private static void ShowWindow()
        {
            var window = GetWindow<ProgressionEditor>("Progression Editor");
            window.Show();
        }

        private void OnEnable()
        {
            if (progressionEntries != null)  { progressionEntries.selectionChanged += OnCharacterClassSelectionChanged; }
        }

        private void OnDisable()
        {
            if (progressionEntries != null)  { progressionEntries.selectionChanged -= OnCharacterClassSelectionChanged; }
        }

        private void OnFocus()
        {
            Undo.undoRedoPerformed += ReloadProgressionForUndoRedo;
        }

        private void OnLostFocus()
        {
            Undo.undoRedoPerformed -= ReloadProgressionForUndoRedo;
        }

        private void CreateGUI()
        {
            var splitView = new TwoPaneSplitView(0, 250, TwoPaneSplitViewOrientation.Horizontal);
            rootVisualElement.Add(splitView);

            var characterNavigationPane = new VisualElement();
            splitView.Add(characterNavigationPane);
            characterStatPane = new ScrollView(ScrollViewMode.Vertical);
            splitView.Add(characterStatPane);

            var navigationPaneSplit = new TwoPaneSplitView(0, 100, TwoPaneSplitViewOrientation.Vertical);
            characterNavigationPane.Add(navigationPaneSplit);
            Box controlBox = CreateControlBox();
            navigationPaneSplit.Add(controlBox);
            Box entryBox = CreateEntryBox();
            navigationPaneSplit.Add(entryBox);
            
            ReloadProgression(true);
        }
        #endregion

        #region DrawUtilities
        private Box CreateControlBox()
        {
            var controlBox = new Box();
            controlBox.Add(new Label("Progression Editor"));
            progressionInput = new ObjectField
            {
                objectType = typeof(Progression),
                label = "Progression SO:  "
            };
            if (progression != null) { progressionInput.value = progression; }
            
            progressionInput.RegisterValueChangedCallback(OnProgressionChanged);
            controlBox.Add(progressionInput);

            var reloadProgression = new Button { text = "Reload" };
            reloadProgression.RegisterCallback<ClickEvent>(ReloadProgression);
            controlBox.Add(reloadProgression);
            
            return controlBox;
        }

        private Box CreateEntryBox()
        {
            var entryBox = new Box();
            if (progressionEntries != null)
            {
                progressionEntries.selectionChanged -= OnCharacterClassSelectionChanged;
                progressionEntries = null;
            }
            
            progressionEntries = new ListView { selectionType = SelectionType.Multiple };
            progressionEntries.selectionChanged += OnCharacterClassSelectionChanged;
            entryBox.Add(progressionEntries);
            
            return entryBox;
        }

        private Box CreateCharacterStatCard(Progression.ProgressionCharacterClass characterClass)
        {
            var characterStatCard = new Box
            {
                style =
                {
                    width = 400,
                    backgroundColor = Color.darkSlateGray
                }
            };
            characterStatCard.Add(new Label($" {characterClass.characterProperties.name}"));

            var leftRightSplit = new TwoPaneSplitView(0, 200, TwoPaneSplitViewOrientation.Horizontal);
            characterStatCard.Add(leftRightSplit);
            var leftPane = new VisualElement();
            leftRightSplit.Add(leftPane);
            var rightPane = new VisualElement();
            leftRightSplit.Add(rightPane);
            
            bool isLeft = true;
            foreach (Progression.ProgressionStat stat in characterClass.stats)
            {
                var statEntry = new FloatField
                {
                    label = stat.stat.ToString(),
                    value = stat.value
                };
                statEntry.RegisterValueChangedCallback(x => UpdateStat(characterClass.characterProperties, stat.stat, x.newValue));
                
                if (isLeft) { leftPane.Add(statEntry); }
                else { rightPane.Add(statEntry); }
                
                isLeft = !isLeft;
            }

            return characterStatCard;
        }

        private void DrawCharacterNavigationPane()
        {
            Progression.ProgressionCharacterClass[] characterClasses = progression.GetCharacterClasses();
            
            progressionEntries.makeItem = () => new Label();
            progressionEntries.bindItem = (item, index) =>
            {
                if (item is Label label) { label.text = characterClasses[index].characterProperties.name; }
            };
            progressionEntries.itemsSource = characterClasses;
        }

        private void DrawCharacterStatPane()
        {
            foreach (Box characterStatCard in selectedCharacterClasses.Select(CreateCharacterStatCard))
            {
                characterStatPane.Add(characterStatCard);
                    
                var spacer = new Box
                {
                    style =
                    {
                        height = 10,
                        width = 400,
                        backgroundColor = Color.gray2
                    }
                };
                characterStatPane.Add(spacer);
            }
        }
        #endregion

        #region EventHandlers
        private void OnProgressionChanged(ChangeEvent<Object> progressionChangedEvent)
        {
            progression = progressionChangedEvent.newValue as Progression;
            ReloadProgression(true);
        }
        private void ReloadProgression(ClickEvent clickEvent)
        {
            progression = progressionInput.value as Progression;
            ReloadProgression(true);
        }

        private void ReloadProgressionForUndoRedo()
        {
            ReloadProgression(false);
        }
        private void ReloadProgression(bool reconcileCharacterProperties)
        {
            progressionEntries.Clear();
            characterStatPane.Clear();
            if (progression == null) { return; }

            if (reconcileCharacterProperties) { ReconcileCharacterProperties(); }
            DrawCharacterNavigationPane();
            DrawCharacterStatPane();
        }

        private void ReconcileCharacterProperties()
        {
            if (progression == null) { return; }
            CharacterProperties.BuildCacheIfEmpty(true);
            
            Dictionary<CharacterProperties, bool> characterPropertiesCrossReference = new Dictionary<CharacterProperties, bool>();
            foreach (Progression.ProgressionCharacterClass characterClass in progression.GetCharacterClasses())
            {
                if (characterClass.characterProperties == null) { continue; }
                characterPropertiesCrossReference[characterClass.characterProperties] = true;
            }

            foreach (var entry in CharacterProperties.GetCharacterPropertiesLookup()
                         .Where(entry => !characterPropertiesCrossReference.ContainsKey(entry.Value)))
            {
                if (!entry.Value.hasProgressionStats) { continue; }
                
                Debug.Log($"Warning:  Missing character properties for {entry.Value}, adding entry to Progression.");
                progression.AddToProgressionAsset(entry.Value);
            }
        }

        private void OnCharacterClassSelectionChanged(IEnumerable<object> selectedItems)
        {
            selectedCharacterClasses.Clear();
            characterStatPane.Clear();
            foreach (var item in selectedItems)
            {
                if (item is not Progression.ProgressionCharacterClass progressionCharacterClass) continue;
                selectedCharacterClasses.Add(progressionCharacterClass);
            }
            
            DrawCharacterStatPane();
        }

        private void UpdateStat(CharacterProperties characterProperties, Stat stat, float value)
        {
            if (progression == null) { return; }
            progression.UpdateProgressionAsset(characterProperties, stat, value);
        }
        #endregion
    }
}

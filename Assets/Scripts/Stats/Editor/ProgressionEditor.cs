using System.Collections.Generic;
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
        }
        #endregion

        #region DrawUtilities
        private Box CreateControlBox()
        {
            var controlBox = new Box();
            controlBox.Add(new Label("Progression Editor"));
            controlBox.Add(new Label(" "));
            progressionInput = new ObjectField
            {
                objectType = typeof(Progression),
                label = "Progression SO:  "
            };
            
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
            
            ReloadProgression();
            
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
                statEntry.RegisterValueChangedCallback((ChangeEvent<float> x) => UpdateStat(characterClass.characterProperties, stat.stat, x.newValue));
                
                if (isLeft) { leftPane.Add(statEntry); }
                else { rightPane.Add(statEntry); }
                
                isLeft = !isLeft;
            }

            return characterStatCard;
        }
        #endregion

        #region EventHandlers
        private void OnProgressionChanged(ChangeEvent<Object> progressionChangedEvent)
        {
            progression = progressionChangedEvent.newValue as Progression;
            ReloadProgression();
        }

        private void ReloadProgression(ClickEvent clickEvent)
        {
            progression = progressionInput.value as Progression;
            ReloadProgression();
        }
        private void ReloadProgression()
        {
            progressionEntries.Clear();
            characterStatPane.Clear();
            if (progression == null) { return; }

            Progression.ProgressionCharacterClass[] characterClasses = progression.GetCharacterClasses();
            
            progressionEntries.makeItem = () => new Label();
            progressionEntries.bindItem = (item, index) =>
            {
                if (item is Label label) { label.text = characterClasses[index].characterProperties.name; }
            };
            progressionEntries.itemsSource = characterClasses;
        }

        private void OnCharacterClassSelectionChanged(IEnumerable<object> selectedItems)
        {
            characterStatPane.Clear();
            foreach (var item in selectedItems)
            {
                if (item is not Progression.ProgressionCharacterClass progressionCharacterClass) continue;
                Box characterStatCard = CreateCharacterStatCard(progressionCharacterClass);
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

        private void UpdateStat(CharacterProperties characterProperties, Stat stat, float value)
        {
            if (progression == null) { return; }
            progression.UpdateProgressionAsset(characterProperties, stat, value);
        }
        #endregion
    }
}

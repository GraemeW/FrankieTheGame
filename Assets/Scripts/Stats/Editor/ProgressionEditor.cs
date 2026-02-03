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
        // Tunables
        private const int _panelSize = 360;

        // Data State
        private Progression progression;
        
        // UI State
        private readonly List<Progression.ProgressionCharacter> selectedCharacters = new();
        
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
            if (progressionEntries != null)  { progressionEntries.selectionChanged += OnCharacterSelectionChanged; }
        }

        private void OnDisable()
        {
            if (progressionEntries != null)  { progressionEntries.selectionChanged -= OnCharacterSelectionChanged; }
        }

        private void OnFocus()
        {
            Undo.undoRedoPerformed += ReloadProgression;
        }

        private void OnLostFocus()
        {
            Undo.undoRedoPerformed -= ReloadProgression;
        }

        private void CreateGUI()
        {
            var splitView = new TwoPaneSplitView(0, 250, TwoPaneSplitViewOrientation.Horizontal);
            rootVisualElement.Add(splitView);

            var characterNavigationPane = new VisualElement();
            splitView.Add(characterNavigationPane);
            characterStatPane = new ScrollView(ScrollViewMode.Vertical);
            splitView.Add(characterStatPane);

            var navigationPaneSplit = new TwoPaneSplitView(0, 160, TwoPaneSplitViewOrientation.Vertical);
            characterNavigationPane.Add(navigationPaneSplit);
            Box controlBox = CreateControlBox();
            navigationPaneSplit.Add(controlBox);
            Box entryBox = CreateEntryBox();
            navigationPaneSplit.Add(entryBox);
            
            ReloadProgression();
        }
        #endregion

        #region DrawUtilities
        private Box CreateControlBox()
        {
            var controlBox = new Box();
            
            // Progression Loading & Tool Parameters
            controlBox.Add(new Label("Progression Editor"));
            progressionInput = new ObjectField
            {
                objectType = typeof(Progression),
                label = "Progression SO:  "
            };
            if (progression != null) { progressionInput.value = progression; }
            progressionInput.RegisterValueChangedCallback(OnProgressionChanged);
            controlBox.Add(progressionInput);

            // Progression Controls
            controlBox.Add(new Label("Progression Asset Controls"));
            var reloadProgression = new Button { text = "Reload Progression" };
            reloadProgression.RegisterCallback<ClickEvent>(ReloadProgression);
            controlBox.Add(reloadProgression);
            
            var reconcileCharacterProperties = new Button { text = "Reconcile Characters" };
            reconcileCharacterProperties.RegisterCallback<ClickEvent>(ReconcileCharacterProperties);
            controlBox.Add(reconcileCharacterProperties);
            
            var rebuildLevelCharts =  new Button { text = "Rebuild Level Charts" };
            rebuildLevelCharts.RegisterCallback<ClickEvent>(RebuildLevelCharts);
            controlBox.Add(rebuildLevelCharts);
                
            // Selection Controls
            controlBox.Add(new Label("Character Selection Controls"));
            var removeCharacters = new Button { text = "Remove Selected Characters" };
            removeCharacters.RegisterCallback<ClickEvent>(RemoveSelectedCharacters);
            controlBox.Add(removeCharacters);
            
            return controlBox;
        }

        private Box CreateEntryBox()
        {
            var entryBox = new Box();
            if (progressionEntries != null)
            {
                progressionEntries.selectionChanged -= OnCharacterSelectionChanged;
                progressionEntries = null;
            }
            
            progressionEntries = new ListView { selectionType = SelectionType.Multiple };
            progressionEntries.selectionChanged += OnCharacterSelectionChanged;
            entryBox.Add(progressionEntries);
            
            return entryBox;
        }

        private Box CreateCharacterStatCard(Progression.ProgressionCharacter character)
        {
            StatCardBase characterStatCard = GetCharacterStatCardBase(false);
            UpdateStatCardHeader(characterStatCard, character, false);
            
            bool isLeft = true;
            foreach (Progression.ProgressionStat progressionStat in character.stats)
            {
                if (progressionStat.stat is Stat.InitialLevel) { continue; }
                
                var statEntry = new FloatField
                {
                    label = progressionStat.stat.ToString(),
                    value = progressionStat.value
                };
                statEntry.RegisterValueChangedCallback(x => UpdateStat(character.characterProperties, progressionStat.stat, x.newValue));
                
                if (isLeft) { characterStatCard.leftPane.Add(statEntry); }
                else { characterStatCard.rightPane.Add(statEntry); }
                
                isLeft = !isLeft;
            }

            return characterStatCard.statCardBase;
        }

        private Box CreateCharacterSimulatedStatCard(Progression.ProgressionCharacter character)
        {
            StatCardBase characterStatCard = GetCharacterStatCardBase(true);
            UpdateStatCardHeader(characterStatCard, character, true);
            SimulateStats(characterStatCard, character, 1);
            
            return characterStatCard.statCardBase;
        }

        private StatCardBase GetCharacterStatCardBase(bool isSimulatedStatCard)
        {
            var characterStatCard = new Box
            {
                style =
                {
                    width = _panelSize,
                    backgroundColor = isSimulatedStatCard ? Color.chocolate : Color.darkSlateGray
                }
            };

            var header = new Box();
            characterStatCard.Add(header);
            
            var leftRightSplit = new TwoPaneSplitView(0, (float)_panelSize / 2, TwoPaneSplitViewOrientation.Horizontal);
            characterStatCard.Add(leftRightSplit);
            var leftPane = new VisualElement();
            leftRightSplit.Add(leftPane);
            var rightPane = new VisualElement();
            leftRightSplit.Add(rightPane);

            return new StatCardBase(characterStatCard, header, leftPane, rightPane);
        }

        private void UpdateStatCardHeader(StatCardBase statCardBase, Progression.ProgressionCharacter character, bool isSimulatedStatCard)
        {
            if (!isSimulatedStatCard)
            {
                var headerSplit = new TwoPaneSplitView(0, (float)_panelSize/2, TwoPaneSplitViewOrientation.Horizontal);
                headerSplit.Add(new Label($" {character.characterProperties.name}"));
                var initialLevel = new IntegerField
                {
                    label = "Initial Level",
                    value = Mathf.RoundToInt(progression.GetStat(Stat.InitialLevel, character.characterProperties))
                };
                initialLevel.RegisterValueChangedCallback(x => UpdateStat(character.characterProperties, Stat.InitialLevel, x.newValue));
                headerSplit.Add(initialLevel);
                
                statCardBase.header.Add(headerSplit);
            }
            else
            {
                var simulatedLevel = new IntegerField
                {
                    label = "Simulated Level",
                    value = 1
                };
                simulatedLevel.RegisterValueChangedCallback((changeEvent =>
                    SimulateStats(statCardBase, character, changeEvent.newValue)));
                statCardBase.header.Add(simulatedLevel);
            }
        }
        
        private void DrawCharacterNavigationPane()
        {
            Progression.ProgressionCharacter[] characters = progression.GetCharacters();
            
            progressionEntries.makeItem = () => new Label();
            progressionEntries.bindItem = (item, index) =>
            {
                if (item is Label label && index < characters.Length) { label.text = characters[index].characterProperties.name; }
            };
            progressionEntries.itemsSource = characters;
        }

        private void DrawCharacterStatPane()
        {
            foreach (Progression.ProgressionCharacter selectedCharacter in selectedCharacters)
            {
                Box characterStatCard = CreateCharacterStatCard(selectedCharacter);
                if (!selectedCharacter.characterProperties.incrementsStatsOnLevelUp)
                {
                    characterStatPane.Add(characterStatCard);
                }
                else
                {
                    var statSimulationSplit = new TwoPaneSplitView
                    {
                        orientation = TwoPaneSplitViewOrientation.Horizontal,
                        fixedPaneInitialDimension = _panelSize,
                        style =
                        {
                            width = _panelSize * 2
                        }
                    };
                    characterStatPane.Add(statSimulationSplit);
                    
                    Box simulatedStatCard = CreateCharacterSimulatedStatCard(selectedCharacter);
                    statSimulationSplit.Add(characterStatCard);
                    statSimulationSplit.Add(simulatedStatCard);
                }
                    
                var spacer = new Box
                {
                    style =
                    {
                        height = 10,
                        width = _panelSize,
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
            ReloadProgression();
        }
        private void ReloadProgression(ClickEvent clickEvent)
        {
            progression = progressionInput.value as Progression;
            ReloadProgression();
        }
        private void ReloadProgression()
        {
            Debug.Log("Reloading Progression");
            progressionEntries.Clear();
            characterStatPane.Clear();
            if (progression == null) { return; }
            
            progression.ForceBuildLookup();
            DrawCharacterNavigationPane();
            DrawCharacterStatPane();
        }

        private void ReconcileCharacterProperties(ClickEvent clickEvent)  { ReconcileCharacterProperties(); }
        private void ReconcileCharacterProperties()
        {
            if (progression == null) { return; }
            Debug.Log("Reconcile:  Updating Progression for any missing character properties");
            CharacterProperties.BuildCacheIfEmpty(true);
            
            var characterPropertiesCrossReference = new Dictionary<CharacterProperties, bool>();
            foreach (Progression.ProgressionCharacter character in progression.GetCharacters())
            {
                if (character.characterProperties == null) { continue; }
                characterPropertiesCrossReference[character.characterProperties] = true;
            }

            foreach (var entry in CharacterProperties.GetCharacterPropertiesLookup()
                         .Where(entry => !characterPropertiesCrossReference.ContainsKey(entry.Value)))
            {
                if (!entry.Value.hasProgressionStats) { continue; }
                
                Debug.Log($"Missing character properties for {entry.Value}, adding entry to Progression.");
                progression.AddToProgressionAsset(entry.Value);
            }
            ReloadProgression();
        }
        
        private void RebuildLevelCharts(ClickEvent clickEvent) { RebuildLevelCharts(); }

        private void RebuildLevelCharts()
        {
            if (progression == null) { return; }
            progression.RebuildLevelCharts();
            ReloadProgression();
        }

        private void OnCharacterSelectionChanged(IEnumerable<object> selectedItems)
        {
            selectedCharacters.Clear();
            characterStatPane.Clear();
            foreach (var item in selectedItems)
            {
                if (item is not Progression.ProgressionCharacter character) continue;
                selectedCharacters.Add(character);
            }
            
            DrawCharacterStatPane();
        }

        private void RemoveSelectedCharacters(ClickEvent clickEvent) {  RemoveSelectedCharacters(); }
        private void RemoveSelectedCharacters()
        {
            if (progression == null) { return; }
            if (selectedCharacters.Count == 0) { return; }
            
            Debug.Log("Removing selected characters");
            List<CharacterProperties> selectedCharacterProperties = selectedCharacters.Select(character => character.characterProperties).ToList();
            progression.RemoveFromProgressionAsset(selectedCharacterProperties);
            selectedCharacters.Clear();
            ReloadProgression();
        }

        private void UpdateStat(CharacterProperties characterProperties, Stat stat, float value)
        {
            if (progression == null) { return; }
            progression.UpdateProgressionAsset(characterProperties, stat, value);
        }

        private void SimulateStats(StatCardBase statCard, Progression.ProgressionCharacter character, int simulatedLevel)
        {
            if (progression == null)  { return; }
            if (character == null || character.characterProperties == null)  { return; }

            // Simulation
            Dictionary<Stat, float> activeStatSheet = Progression.GetLevelAveragedStatSheet(progression, character.characterProperties, simulatedLevel);
            if (activeStatSheet == null) { return; }
            
            // Draw entries onto card
            bool isLeft = true;
            statCard.leftPane.Clear();
            statCard.rightPane.Clear();
            foreach (Progression.ProgressionStat progressionStat in character.stats)
            {
                if (!activeStatSheet.ContainsKey(progressionStat.stat)) { continue; }
                if (BaseStats.GetNonModifyingStats().Contains(progressionStat.stat)) { continue; }
                
                var statEntry = new FloatField
                {
                    label = progressionStat.stat.ToString(),
                    value = Mathf.RoundToInt(activeStatSheet[progressionStat.stat]),
                    isReadOnly = true
                };
                
                if (isLeft) { statCard.leftPane.Add(statEntry); }
                else { statCard.rightPane.Add(statEntry); }
                
                isLeft = !isLeft;
            }
        }
        #endregion
        
        #region DataStructures
        private struct StatCardBase
        {
            public readonly Box statCardBase;
            public readonly Box header;
            public readonly VisualElement leftPane;
            public readonly VisualElement rightPane;

            public StatCardBase(Box statCardBase, Box header, VisualElement leftPane, VisualElement rightPane)
            {
                this.header = header;
                this.statCardBase = statCardBase;
                this.leftPane = leftPane;
                this.rightPane = rightPane;
            }
        }
        #endregion
    }
}

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
        private readonly List<Progression.ProgressionCharacterClass> selectedCharacterClasses = new();
        
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
            controlBox.Add(new Label("Progression Editor"));
            progressionInput = new ObjectField
            {
                objectType = typeof(Progression),
                label = "Progression SO:  "
            };
            if (progression != null) { progressionInput.value = progression; }
            
            progressionInput.RegisterValueChangedCallback(OnProgressionChanged);
            controlBox.Add(progressionInput);

            controlBox.Add(new Label("Progression Asset Controls"));
            
            var reloadProgression = new Button { text = "Reload Progression" };
            reloadProgression.RegisterCallback<ClickEvent>(ReloadProgression);
            controlBox.Add(reloadProgression);
            
            var reconcileCharacterProperties = new Button { text = "Reconcile Characters" };
            reconcileCharacterProperties.RegisterCallback<ClickEvent>(ReconcileCharacterProperties);
            controlBox.Add(reconcileCharacterProperties);
                
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
            StatCardBase characterStatCard = GetCharacterStatCardBase(false);
            UpdateStatCardHeader(characterStatCard, characterClass, false);
            
            bool isLeft = true;
            foreach (Progression.ProgressionStat progressionStat in characterClass.stats)
            {
                if (progressionStat.stat is Stat.InitialLevel) { continue; }
                
                var statEntry = new FloatField
                {
                    label = progressionStat.stat.ToString(),
                    value = progressionStat.value
                };
                statEntry.RegisterValueChangedCallback(x => UpdateStat(characterClass.characterProperties, progressionStat.stat, x.newValue));
                
                if (isLeft) { characterStatCard.leftPane.Add(statEntry); }
                else { characterStatCard.rightPane.Add(statEntry); }
                
                isLeft = !isLeft;
            }

            return characterStatCard.statCardBase;
        }

        private Box CreateCharacterSimulatedStatCard(Progression.ProgressionCharacterClass characterClass)
        {
            StatCardBase characterStatCard = GetCharacterStatCardBase(true);
            UpdateStatCardHeader(characterStatCard, characterClass, true);
            SimulateStats(characterStatCard, characterClass, 1);
            
            return characterStatCard.statCardBase;
        }

        private StatCardBase GetCharacterStatCardBase(bool isSimulatedStatCard)
        {
            var characterStatCard = new Box
            {
                style =
                {
                    width = 400,
                    backgroundColor = isSimulatedStatCard ? Color.chocolate : Color.darkSlateGray
                }
            };

            var header = new Box();
            characterStatCard.Add(header);
            
            var leftRightSplit = new TwoPaneSplitView(0, 200, TwoPaneSplitViewOrientation.Horizontal);
            characterStatCard.Add(leftRightSplit);
            var leftPane = new VisualElement();
            leftRightSplit.Add(leftPane);
            var rightPane = new VisualElement();
            leftRightSplit.Add(rightPane);

            return new StatCardBase(characterStatCard, header, leftPane, rightPane);
        }

        private void UpdateStatCardHeader(StatCardBase statCardBase, Progression.ProgressionCharacterClass characterClass, bool isSimulatedStatCard)
        {
            if (!isSimulatedStatCard)
            {
                var headerSplit = new TwoPaneSplitView(0, 200, TwoPaneSplitViewOrientation.Horizontal);
                headerSplit.Add(new Label($" {characterClass.characterProperties.name}"));
                var initialLevel = new IntegerField
                {
                    label = "Initial Level",
                    value = Mathf.RoundToInt(progression.GetStat(Stat.InitialLevel, characterClass.characterProperties))
                };
                initialLevel.RegisterValueChangedCallback(x => UpdateStat(characterClass.characterProperties, Stat.InitialLevel, x.newValue));
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
                    SimulateStats(statCardBase, characterClass, changeEvent.newValue)));
                statCardBase.header.Add(simulatedLevel);
            }
        }
        
        private void DrawCharacterNavigationPane()
        {
            Progression.ProgressionCharacterClass[] characterClasses = progression.GetCharacterClasses();
            
            progressionEntries.makeItem = () => new Label();
            progressionEntries.bindItem = (item, index) =>
            {
                if (item is Label label && index < characterClasses.Length) { label.text = characterClasses[index].characterProperties.name; }
            };
            progressionEntries.itemsSource = characterClasses;
        }

        private void DrawCharacterStatPane()
        {
            foreach (Progression.ProgressionCharacterClass progressionClass in selectedCharacterClasses)
            {
                Box characterStatCard = CreateCharacterStatCard(progressionClass);
                if (!progressionClass.characterProperties.incrementsStatsOnLevelUp)
                {
                    characterStatPane.Add(characterStatCard);
                }
                else
                {
                    var statSimulationSplit = new TwoPaneSplitView
                    {
                        orientation = TwoPaneSplitViewOrientation.Horizontal,
                        fixedPaneInitialDimension = 400,
                        style =
                        {
                            width = 800
                        }
                    };
                    characterStatPane.Add(statSimulationSplit);
                    
                    Box simulatedStatCard = CreateCharacterSimulatedStatCard(progressionClass);
                    statSimulationSplit.Add(characterStatCard);
                    statSimulationSplit.Add(simulatedStatCard);
                }
                    
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
            foreach (Progression.ProgressionCharacterClass characterClass in progression.GetCharacterClasses())
            {
                if (characterClass.characterProperties == null) { continue; }
                characterPropertiesCrossReference[characterClass.characterProperties] = true;
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

        private void RemoveSelectedCharacters(ClickEvent clickEvent) {  RemoveSelectedCharacters(); }
        private void RemoveSelectedCharacters()
        {
            if (progression == null) { return; }
            if (selectedCharacterClasses.Count == 0) { return; }
            
            Debug.Log("Removing selected characters");
            List<CharacterProperties> selectedCharacterProperties = selectedCharacterClasses.Select(characterClass => characterClass.characterProperties).ToList();
            progression.RemoveFromProgressionAsset(selectedCharacterProperties);
            selectedCharacterClasses.Clear();
            ReloadProgression();
        }

        private void UpdateStat(CharacterProperties characterProperties, Stat stat, float value)
        {
            if (progression == null) { return; }
            progression.UpdateProgressionAsset(characterProperties, stat, value);
        }

        private void SimulateStats(StatCardBase statCard, Progression.ProgressionCharacterClass characterClass, int simulatedLevel)
        {
            if (progression == null)  { return; }
            if (characterClass == null || characterClass.characterProperties == null)  { return; }

            // Simulation
            Dictionary<Stat, float> activeStatSheet = progression.GetStatSheet(characterClass.characterProperties);
            for (int currentLevel = 1; currentLevel < simulatedLevel; currentLevel++)
            {
                Dictionary<Stat, float> levelUpSheet = BaseStats.GetLevelUpSheet(currentLevel, activeStatSheet[Stat.Stoic], activeStatSheet[Stat.Smarts], characterClass.characterProperties, progression);
                activeStatSheet[Stat.HP] += levelUpSheet[Stat.HP];
                activeStatSheet[Stat.AP] += levelUpSheet[Stat.AP];
                activeStatSheet[Stat.Brawn] += levelUpSheet[Stat.Brawn];
                activeStatSheet[Stat.Beauty] += levelUpSheet[Stat.Beauty];
                activeStatSheet[Stat.Smarts] += levelUpSheet[Stat.Smarts];
                activeStatSheet[Stat.Nimble] += levelUpSheet[Stat.Nimble];
                activeStatSheet[Stat.Luck] += levelUpSheet[Stat.Luck];
                activeStatSheet[Stat.Pluck] += levelUpSheet[Stat.Pluck];
                activeStatSheet[Stat.Stoic] += levelUpSheet[Stat.Stoic];
            }
            
            // Draw entries onto card
            bool isLeft = true;
            statCard.leftPane.Clear();
            statCard.rightPane.Clear();
            foreach (Progression.ProgressionStat progressionStat in characterClass.stats)
            {
                if (!activeStatSheet.ContainsKey(progressionStat.stat)) { continue; }
                if (progressionStat.stat is Stat.InitialLevel or Stat.ExperienceReward or Stat.ExperienceToLevelUp) { continue; }
                
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

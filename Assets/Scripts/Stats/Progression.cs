using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Frankie.Stats
{
    [CreateAssetMenu(fileName = "Progression", menuName = "Stats/New Progression", order = 0)]
    public class Progression : ScriptableObject
    {
        // Tunables
        [Header("Behavioural Tunables")]
        [SerializeField] private float defaultStatValueIfMissing = 1f;
        [SerializeField] private float levelUpHPMultiplier = 4.0f;
        [SerializeField] private float levelUpAPMultiplier = 2.0f;
        [SerializeField] private int levelChartAveraging = 20;
        [SerializeField][Range(0.5f, 1.0f)] private float statCatchUpThreshold = 0.9f;
        [SerializeField][Tooltip("i.e. every n levels trigger stat catchup")] private int statCatchUpLevelMod = 4;
        [SerializeField] private float statCatchUpRatio = 0.35f;
        
        [Header("Primary Stat Sheets")]
        [SerializeField] private ProgressionCharacter[] characters;
        [HideInInspector][SerializeField] private List<ProgressionLevelChart> levelAveragedCharts = new();

        // Constants
        private const int _maxLevel = 99;
        
        private static readonly List<float[]> _levelUpStatScalerLerpPoints = new()
        {
            // Ensure LERP points match in length
            // Ensure incrementing in a logical manner
            new[]{0f, 0.3f, 0.85f, 0.95f, 1.0f}, // Roll Probability
            new[]{0.0f, 0.6f, 1.0f, 1.25f, 1.5f} // Multiplier
        };
        
        // State
        private Dictionary<CharacterProperties, Dictionary<Stat, float>> characterStatLookup;
        private Dictionary<CharacterProperties, Dictionary<int, Dictionary<Stat, float>>> levelAveragedChartLookup;
        
        #region StaticMethods
        public static int GetMaxLevel() => _maxLevel;
        
        private static float GetDefaultStatValue(Stat stat)
        {
            return stat switch
            {
                Stat.HP => 20f,
                Stat.AP => 10f,
                Stat.ExperienceReward => 500f,
                Stat.ExperienceToLevelUp => 999f,
                Stat.Brawn => 3f,
                Stat.Beauty => 3f,
                Stat.Smarts => 3f,
                Stat.Nimble => 3f,
                Stat.Luck => 3f,
                Stat.Pluck => 3f,
                Stat.Stoic => 2f,
                Stat.InitialLevel => 1f,
                _ => 0f
            };
        }
        
        private static float GetLevelUpStatMultiplier()
        {
            float chance = UnityEngine.Random.Range(0f, 1f);
            int lowAnchorIndex;
            for (lowAnchorIndex = 0; lowAnchorIndex < _levelUpStatScalerLerpPoints[0].Length - 1; lowAnchorIndex++)
            {
                if (chance >= _levelUpStatScalerLerpPoints[0][lowAnchorIndex] && chance <= _levelUpStatScalerLerpPoints[0][lowAnchorIndex+1]) { break; }
            }
            
            float normalizedChance = (chance - _levelUpStatScalerLerpPoints[0][lowAnchorIndex]) / (_levelUpStatScalerLerpPoints[0][lowAnchorIndex+1] - _levelUpStatScalerLerpPoints[0][lowAnchorIndex]);
            return Mathf.Lerp(_levelUpStatScalerLerpPoints[1][lowAnchorIndex],  _levelUpStatScalerLerpPoints[1][lowAnchorIndex+1], normalizedChance);
        }
        #endregion
        
#if UNITY_EDITOR
        #region EditorMethods
        public void ForceBuildLookup()
        {
            BuildLookup(true);
        }

        public void ForceInitializeLevelCharts()
        {
            InitializeLevelAveragedCharts(true);
        }
        
        public void UpdateProgressionAsset(CharacterProperties characterProperties, Stat updatedStat, float newValue)
        {
            Undo.RegisterCompleteObjectUndo(this, "Update Progression");
            bool characterFound = false;
            foreach (ProgressionCharacter character in characters)
            {
                if (character.characterProperties != characterProperties) { continue; }

                characterFound = true;
                bool statFound = false;
                foreach (ProgressionStat progressionStat in character.stats)
                {
                    if (progressionStat.stat != updatedStat) { continue; }
                    
                    statFound = true;
                    progressionStat.value = newValue;
                    break;
                }

                if (!statFound)
                {
                    var newProgressionStat = new ProgressionStat { stat = updatedStat, value = newValue };
                    Array.Resize(ref character.stats, character.stats.Length + 1);
                    character.stats[^1] =  newProgressionStat;
                }
                break;
            }
            EditorUtility.SetDirty(this);

            if (characterFound)
            {
                // Update dictionary to avoid need to rebuild entire dict on every update
                characterStatLookup[characterProperties][updatedStat] = newValue;
            }
        }

        public void AddToProgressionAsset(CharacterProperties characterProperties)
        {
            var character = new ProgressionCharacter
            {
                characterProperties = characterProperties,
                stats = (from Stat stat in Enum.GetValues(typeof(Stat)) select new ProgressionStat { stat = stat, value = GetDefaultStatValue(stat) }).ToArray()
            };
            
            Undo.RegisterCompleteObjectUndo(this, "Add to Progression");
            Array.Resize(ref characters, characters.Length + 1);
            characters[^1] = character;
            EditorUtility.SetDirty(this);
            
            ForceBuildLookup();
        }

        public void RemoveFromProgressionAsset(IEnumerable<CharacterProperties> charactersProperties)
        {
            Undo.RegisterCompleteObjectUndo(this, "Remove from Progression");
            foreach (CharacterProperties characterProperties in charactersProperties)
            {
                var charactersVolatile = new List<ProgressionCharacter>(characters);
                charactersVolatile.RemoveAll(x => x.characterProperties == characterProperties);
                characters =  charactersVolatile.ToArray();
            }
            EditorUtility.SetDirty(this);
            
            ForceBuildLookup();
        }
        #endregion
#endif
        
        #region PublicMethods
        public ProgressionCharacter[] GetCharacters() => characters;

        public bool HasProgression(CharacterProperties characterProperties)
        {
            BuildLookup();
            return characterStatLookup.ContainsKey(characterProperties);
        }
        
        public float GetStat(Stat stat, CharacterProperties characterProperties)
        {
            BuildLookup();
            return characterStatLookup[characterProperties][stat];
        }

        public Dictionary<Stat, float> GetStatSheet(CharacterProperties characterProperties)
        {
            BuildLookup();
            var statSheet = new Dictionary<Stat, float>();
            Dictionary<Stat, float> statBook = characterStatLookup[characterProperties];
            foreach (Stat stat in statBook.Keys)
            {
                statSheet[stat] = GetStat(stat, characterProperties);
            }
            return statSheet;
        }
        
        public Dictionary<Stat, float> GetLevelUpSheet(CharacterProperties characterProperties, int currentLevel, Dictionary<Stat, float> activeStatSheet)
        {
            InitializeLevelAveragedCharts();
            Dictionary<Stat, float> levelUpSheet = GetStandardLevelUpSheet(characterProperties, currentLevel, activeStatSheet);
            if (currentLevel % statCatchUpLevelMod != 0 || !IsLevelAveragedChartLookupSet()) { return levelUpSheet; }

            return MakeCatchUpLevelUpSheet(levelUpSheet, activeStatSheet, characterProperties, currentLevel);
        }

        public Dictionary<Stat, float> GetLevelAveragedStatSheet(CharacterProperties characterProperties, int currentLevel)
        {
            InitializeLevelAveragedCharts();
            if (!IsLevelAveragedChartLookupSet()) { return null; }
            
            Dictionary<int, Dictionary<Stat, float>> averagedLevelUpSheets = levelAveragedChartLookup.GetValueOrDefault(characterProperties);
            return averagedLevelUpSheets?.GetValueOrDefault(currentLevel);
        }
        #endregion
        
        #region PrivateMethods
        private void BuildLookup(bool forceBuild = false)
        {
            if (characterStatLookup != null && !forceBuild) { return; }
            characterStatLookup = new Dictionary<CharacterProperties, Dictionary<Stat, float>>();

            foreach (ProgressionCharacter character in characters)
            {
                var statDictionary = new Dictionary<Stat, float>();

                foreach (Stat stat in Enum.GetValues(typeof(Stat)))
                {
                    bool foundStat = false;
                    foreach (ProgressionStat progressionStat in character.stats)
                    {
                        if (progressionStat.stat != stat) continue;
                        statDictionary[stat] = progressionStat.value;
                        foundStat = true;
                        break;
                    }
                    if (!foundStat) { statDictionary[stat] = defaultStatValueIfMissing; }
                }
                characterStatLookup[character.characterProperties] = statDictionary;
            }
        }
        
        private void InitializeLevelAveragedCharts(bool forceBuild = false)
        {
            if (forceBuild || levelAveragedCharts == null || levelAveragedCharts.Count == 0)
            {
                RebuildLevelAveragedCharts();
                BuildLevelAveragedChartLookup(true);
                return;
            }
            BuildLevelAveragedChartLookup();
        }
        
        private void RebuildLevelAveragedCharts()
        {
#if UNITY_EDITOR
            Undo.RegisterCompleteObjectUndo(this, "Rebuild Level Charts");
#endif
            Debug.Log("Rebuilding Level Charts");
            levelAveragedCharts.Clear();
            foreach (var characterEntry in CharacterProperties.GetCharacterPropertiesLookup().Where(entry => entry.Value != null && entry.Value.incrementsStatsOnLevelUp))
            {
                CharacterProperties characterProperties = characterEntry.Value;
                var progressionLevelChart = new ProgressionLevelChart { characterProperties = characterProperties };

                var averageLeveledStats = new List<ProgressionLeveledStats>();
                Dictionary<int, Dictionary<Stat, float>> averagedLevelSheets = GetLevelAveragedSheets(characterProperties);
                foreach (var averageLevelSheet in averagedLevelSheets)
                {
                    var averageLeveledStatsEntry = new ProgressionLeveledStats
                    {
                        level = averageLevelSheet.Key,
                        progressionStats = averageLevelSheet.Value.Select(statEntry => new ProgressionStat { stat = statEntry.Key, value = statEntry.Value }).ToArray()
                    };
                    averageLeveledStats.Add(averageLeveledStatsEntry);
                }
                
                progressionLevelChart.leveledStats = averageLeveledStats.ToArray();
                levelAveragedCharts.Add(progressionLevelChart);
            }
            
#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif
        }
        
        private void BuildLevelAveragedChartLookup(bool forceBuild = false)
        {
            if (levelAveragedChartLookup != null && !forceBuild) { return; }
            if (levelAveragedCharts == null || levelAveragedCharts.Count == 0) { return; }

            levelAveragedChartLookup = new Dictionary<CharacterProperties, Dictionary<int, Dictionary<Stat, float>>>();
            
            foreach (ProgressionLevelChart levelChart in levelAveragedCharts)
            {
                CharacterProperties characterProperties = levelChart.characterProperties;
                if (characterProperties == null || levelChart.leveledStats == null) { continue; }
                
                var statsSheets = new Dictionary<int, Dictionary<Stat, float>>();
                foreach (ProgressionLeveledStats progressionLeveledStats in levelChart.leveledStats)
                {
                    if (progressionLeveledStats.progressionStats == null) { continue; }

                    int level =  progressionLeveledStats.level;
                    var statsSheet = new Dictionary<Stat, float>();
                    foreach (ProgressionStat progressionStat in progressionLeveledStats.progressionStats)
                    {
                        statsSheet[progressionStat.stat] = progressionStat.value;
                    }
                    statsSheets[level] = statsSheet;
                }
                levelAveragedChartLookup[characterProperties] = statsSheets;
            }
        }
        
        private bool IsLevelAveragedChartLookupSet() => levelAveragedChartLookup is { Count: > 0 }; 
        
        private Dictionary<Stat, float> GetStandardLevelUpSheet(CharacterProperties characterProperties, int currentLevel, Dictionary<Stat, float> activeStatSheet)
        {
            var levelUpSheet = new Dictionary<Stat, float>();
            foreach (Stat stat in Enum.GetValues(typeof(Stat)))
            {
                if (BaseStats.GetNonModifyingStats().Contains(stat)) { continue; }

                switch (stat)
                {
                    case Stat.HP:
                        float currentStoic = activeStatSheet.TryGetValue(Stat.Stoic, out var stoicEntry) ? stoicEntry : GetStat(Stat.Stoic, characterProperties);
                        levelUpSheet[stat] = (levelUpHPMultiplier * currentStoic / currentLevel) * GetLevelUpStatMultiplier();
                        break;
                    case Stat.AP:
                        float currentSmarts = activeStatSheet.TryGetValue(Stat.Smarts, out var smartsEntry) ? smartsEntry : GetStat(Stat.Smarts, characterProperties);
                        levelUpSheet[stat] = (levelUpAPMultiplier * currentSmarts / currentLevel) * GetLevelUpStatMultiplier();
                        break;
                    default:
                        levelUpSheet[stat] = GetStat(stat, characterProperties) * GetLevelUpStatMultiplier();
                        break;
                }
            }
            return levelUpSheet;
        }

        private Dictionary<int, Dictionary<Stat, float>> GetLevelAveragedSheets(CharacterProperties characterProperties)
        {
            int currentLevel = 1;
            var levelUpSheets = new Dictionary<int, Dictionary<Stat, float>> { [currentLevel] = new(GetStatSheet(characterProperties)) };
            
            while (currentLevel < _maxLevel)
            {
                levelUpSheets[currentLevel + 1] = SimulateStandardStatSheetLevelUp(characterProperties, levelUpSheets[currentLevel], currentLevel, levelChartAveraging);
                currentLevel++;
            }
            return levelUpSheets;
        }
        
        private Dictionary<Stat, float> SimulateStandardStatSheetLevelUp(CharacterProperties characterProperties, Dictionary<Stat, float> currentLevelSheet, int currentLevel, int averaging)
        {
            var leveledSheet = new Dictionary<Stat, float>(currentLevelSheet);
            
            Dictionary<Stat, float> sumLevelUpSheet = GetStandardLevelUpSheet(characterProperties, currentLevel, currentLevelSheet);
            for (int averageCount = 1; averageCount < averaging; averageCount++) // Start counter at 1, as initial values set in GetLevelUpSheet()
            {
                Dictionary<Stat, float> incrementalLevelUpSheet = GetStandardLevelUpSheet(characterProperties, currentLevel, currentLevelSheet);
                foreach (KeyValuePair<Stat, float> statValuePair in incrementalLevelUpSheet) { sumLevelUpSheet[statValuePair.Key] += statValuePair.Value; }
            }
            foreach (KeyValuePair<Stat, float> statValuePair in sumLevelUpSheet) { leveledSheet[statValuePair.Key] += (statValuePair.Value / averaging); }
            
            return leveledSheet;
        }
        
        private bool HasLevelAveragedChart(CharacterProperties characterProperties, int currentLevel, out  Dictionary<Stat, float> levelAverageStatSheet)
        {
            Dictionary<int, Dictionary<Stat, float>> levelAverageStatSheets = levelAveragedChartLookup.GetValueOrDefault(characterProperties);
            levelAverageStatSheet = levelAverageStatSheets?.GetValueOrDefault(currentLevel);
            return levelAverageStatSheet != null;
        }
        
        private Dictionary<Stat, float> MakeCatchUpLevelUpSheet(Dictionary<Stat, float> levelUpSheet, Dictionary<Stat, float> activeStatSheet, CharacterProperties characterProperties, int currentLevel)
        {
            var catchUpLevelUpSheet = new Dictionary<Stat, float>(levelUpSheet);
            if (!HasLevelAveragedChart(characterProperties, currentLevel, out Dictionary<Stat, float> levelAverageStatSheet)) { return catchUpLevelUpSheet; }
            
            Debug.Log("Special level-up:  Checking for stat catch-up");
            foreach (Stat stat in levelUpSheet.Keys)
            {
                if (!activeStatSheet.TryGetValue(stat, out var currentStatEntry)) { continue; }
                if (!levelAverageStatSheet.TryGetValue(stat, out var averagedStatEntry)) { continue; }
                if (currentStatEntry >= averagedStatEntry * statCatchUpThreshold) { continue; }
                
                catchUpLevelUpSheet[stat] += (averagedStatEntry - currentStatEntry) * statCatchUpRatio;
                Debug.Log($"Stat {stat} below average stat threshold - bolstering by {catchUpLevelUpSheet[stat]}");
            }
            Debug.Log("Ending stat catch-up checks.");
            return catchUpLevelUpSheet;
        }
        #endregion

        #region DataStructures
        [Serializable]
        public class ProgressionCharacter
        {
            public CharacterProperties characterProperties;
            public ProgressionStat[] stats;
        }

        [Serializable]
        public class ProgressionStat
        {
            public Stat stat;
            public float value;
        }

        [Serializable]
        public class ProgressionLeveledStats
        {
            public int level;
            public ProgressionStat[] progressionStats;
        }

        [Serializable]
        public class ProgressionLevelChart
        {
            public CharacterProperties characterProperties;
            public ProgressionLeveledStats[] leveledStats;
        }
        #endregion
    }
}

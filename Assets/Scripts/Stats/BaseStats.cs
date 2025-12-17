using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Frankie.Utils;
using Frankie.Saving;
using Frankie.Core;

namespace Frankie.Stats
{
    public class BaseStats : MonoBehaviour, ISaveable, IPredicateEvaluator
    {
        // Behavior Detail
        // Progression defines 1) Stats at level 1 --> pulled to the active stat sheet, which is what is saved
        // 2)  Modifiers used as basis for subsequent levels --> multiplied out by a random factor, and then added to active stat sheet

        // Tunables
        [Header("Parameters")]
        [SerializeField] private CharacterProperties characterProperties;
        [SerializeField] private bool levelUpOnInstantiation = false;
        [SerializeField][Tooltip("Default true for PCs, false for NPCs")] private bool useSavedStatsOnLoad = true;
        [Header("Hookups")]
        [SerializeField] private Progression progression;

        // Static/Const Parameters
        private const float _hpMultiplier = 4.0f;
        private const float _apMultiplier = 2.0f;
        private static readonly List<float[]> _levelUpStatScalerLerpPoints = new()
        {
            // Ensure LERP points match in length
            // Ensure incrementing in a logical manner
            new float[5]{0f, 0.2f, 0.85f, 0.95f, 1.0f}, // Roll Probability
            new float[5]{0.0f, 0.5f, 1.0f, 1.25f, 1.5f} // Multiplier
        };
        private const int _defaultLevelForNoCharacterProperties = 1;
        private const int _maxLevel = 99;
        
        // State
        private LazyValue<int> currentLevel;
        private Dictionary<Stat, float> activeStatSheet;

        // Events
        public event Action<BaseStats, int, Dictionary<Stat, float>> onLevelUp;

        #region Static
        public static Stat[] GetNonModifyingStats()
        {
            // Subset enum for those equipment should not touch
            Stat[] nonModifyingStats = { Stat.InitialLevel, Stat.ExperienceReward, Stat.ExperienceToLevelUp };
            return nonModifyingStats;
        }

        public static Dictionary<Stat, float> GetLevelAveragedStatSheet(Progression progression, CharacterProperties characterProperties, int simulatedLevel, int levelAveraging)
        {
            // Initial Sheet
            Dictionary<Stat, float> activeStatSheet = progression.GetStatSheet(characterProperties);
            
            for (int currentLevel = 1; currentLevel < simulatedLevel; currentLevel++)
            {
                // Level-Up Sheet
                Dictionary<Stat, float> sumLevelUpSheet = GetLevelUpSheet(progression, characterProperties, 
                    currentLevel, activeStatSheet[Stat.Stoic], activeStatSheet[Stat.Smarts]);
                // Averaging at each level
                for (int averageCount = 1; averageCount < levelAveraging; averageCount++)
                {
                    Dictionary<Stat, float> incrementalLevelUpSheet = GetLevelUpSheet(progression, characterProperties, 
                        currentLevel, activeStatSheet[Stat.Stoic], activeStatSheet[Stat.Smarts]);
                    foreach (KeyValuePair<Stat, float> statValuePair in incrementalLevelUpSheet) { sumLevelUpSheet[statValuePair.Key] += statValuePair.Value; }
                }
                
                // Take average value for active stat sheet
                foreach (KeyValuePair<Stat, float> statValuePair in sumLevelUpSheet) { activeStatSheet[statValuePair.Key] += statValuePair.Value / levelAveraging; }
            }

            return activeStatSheet;
        }
        
        private static Dictionary<Stat, float> GetLevelUpSheet(Progression progression, CharacterProperties characterProperties, int currentLevel, float currentStoic, float currentSmarts)
        {
            var levelUpSheet = new Dictionary<Stat, float>
            {
                [Stat.Brawn] = progression.GetStat(Stat.Brawn, characterProperties) * GetLevelUpStatMultiplier(),
                [Stat.Beauty] = progression.GetStat(Stat.Beauty, characterProperties) * GetLevelUpStatMultiplier(),
                [Stat.Nimble] = progression.GetStat(Stat.Nimble, characterProperties) * GetLevelUpStatMultiplier(),
                [Stat.Luck] = progression.GetStat(Stat.Luck, characterProperties) * GetLevelUpStatMultiplier(),
                [Stat.Pluck] = progression.GetStat(Stat.Pluck, characterProperties) * GetLevelUpStatMultiplier(),
                [Stat.Stoic] = progression.GetStat(Stat.Stoic, characterProperties) * GetLevelUpStatMultiplier(), // Used for HP adjust
                [Stat.Smarts] = progression.GetStat(Stat.Smarts, characterProperties)* GetLevelUpStatMultiplier(), // Used for AP adjust
                [Stat.HP] = (_hpMultiplier * currentStoic / currentLevel) * GetLevelUpStatMultiplier(), // Take overall stat normalized to level, bonus swing larger for HP
                [Stat.AP] = (_apMultiplier * currentSmarts / currentLevel) * GetLevelUpStatMultiplier() // Take overall stat normalized to level, bonus swing larger for AP
            };
            return levelUpSheet;
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

        #region UnityMethods
        private void Awake()
        {
            currentLevel = new LazyValue<int>(GetInitialLevel);
        }

        private void Start()
        {
            currentLevel.ForceInit();
        }
        #endregion

        #region PublicGetters
        public CharacterProperties GetCharacterProperties() => characterProperties;
        public int GetLevel() => currentLevel.value;
        public bool CanLevelUp() => GetLevel() < _maxLevel;
        public float GetStat(Stat stat) => GetBaseStat(stat) + GetAdditiveModifiers(stat);
        private float GetBaseStat(Stat stat)
        {
            BuildActiveStatSheetIfNull();
            return activeStatSheet.TryGetValue(stat, out var baseStat) ? baseStat : progression.GetStat(stat, characterProperties);
        }

        public bool GetStatForCalculatedStat(CalculatedStat calculatedStat, out Stat stat) => CalculatedStats.GetStatModifier(calculatedStat, out stat);

        public float GetCalculatedStat(CalculatedStat calculatedStat, float statValue, float opponentStatValue)
        {
            return CalculatedStats.GetCalculatedStat(calculatedStat, GetLevel(), statValue, opponentStatValue);
        }
        public float GetCalculatedStat(CalculatedStat calculatedStat)
        {
            if (!CalculatedStats.GetStatModifier(calculatedStat, out Stat statModifier)) return 1f;
            
            float stat = GetStat(statModifier);
            return CalculatedStats.GetCalculatedStat(calculatedStat, GetLevel(), stat);
        }

        public Dictionary<Stat, float> GetActiveStatSheet() => activeStatSheet; // NOTE:  Does NOT contain modifiers
        #endregion

        #region PublicFunctional
        public void OverrideLevel(int level)
        {
            currentLevel.value = level;
        }

        public void SetActiveStatSheet(Dictionary<Stat, float> setActiveStatSheet)
        {
            activeStatSheet = setActiveStatSheet;
        }

        public void AdjustStat(Stat stat, float value)
        {
            BuildActiveStatSheetIfNull();
            activeStatSheet[stat] += value;
        }

        public void IncrementLevel()
        {
            currentLevel.value++;
            if (!characterProperties.incrementsStatsOnLevelUp) return;
            
            Dictionary<Stat, float> levelUpSheet = IncrementStatsOnLevelUp();
            onLevelUp?.Invoke(this, GetLevel(), levelUpSheet);
        }
        #endregion

        #region PrivateMethods
        private void BuildActiveStatSheetIfNull()
        {
            if (activeStatSheet != null) { return; }
            activeStatSheet = progression.GetStatSheet(characterProperties);
            
            if (!levelUpOnInstantiation) return;
            currentLevel.value = 1;
            for (int i = 1; i < GetInitialLevel(); i++)
            {
                IncrementLevel();
            }
        }

        private int GetInitialLevel()
        {
            if (characterProperties == null || !progression.HasProgression(characterProperties)) { return _defaultLevelForNoCharacterProperties; }
            return Mathf.RoundToInt(progression.GetStat(Stat.InitialLevel, characterProperties));
        }

        private float GetAdditiveModifiers(Stat stat)
        {
            var modifierProviders = GetComponents<IModifierProvider>();
            return modifierProviders.SelectMany(modifierProvider => modifierProvider.GetAdditiveModifiers(stat)).Sum();
        }

        private Dictionary<Stat, float> IncrementStatsOnLevelUp()
        {
            var levelUpSheet = GetLevelUpSheet(progression, characterProperties, 
                GetLevel(), GetBaseStat(Stat.Stoic), GetBaseStat(Stat.Smarts));

            activeStatSheet[Stat.HP] += levelUpSheet[Stat.HP];
            activeStatSheet[Stat.AP] += levelUpSheet[Stat.AP];
            activeStatSheet[Stat.Brawn] += levelUpSheet[Stat.Brawn];
            activeStatSheet[Stat.Beauty] += levelUpSheet[Stat.Beauty];
            activeStatSheet[Stat.Smarts] += levelUpSheet[Stat.Smarts];
            activeStatSheet[Stat.Nimble] += levelUpSheet[Stat.Nimble];
            activeStatSheet[Stat.Luck] += levelUpSheet[Stat.Luck];
            activeStatSheet[Stat.Pluck] += levelUpSheet[Stat.Pluck];
            activeStatSheet[Stat.Stoic] += levelUpSheet[Stat.Stoic];

            return levelUpSheet;
        }
        #endregion

        #region Interfaces
        // Save State
        [Serializable]
        private class BaseStatsSaveData
        {
            public int level;
            public Dictionary<Stat, float> statSheet;
        }
        
        public bool IsCorePlayerState() => true;
        public LoadPriority GetLoadPriority() => LoadPriority.ObjectProperty;

        public SaveState CaptureState()
        {
            currentLevel ??= new LazyValue<int>(GetInitialLevel);
            var baseStatsSaveData = new BaseStatsSaveData
            {
                level = currentLevel.value,
                statSheet = activeStatSheet
            };
            var saveState = new SaveState(GetLoadPriority(), baseStatsSaveData);
            return saveState;
        }

        public void RestoreState(SaveState saveState)
        {
            if (!useSavedStatsOnLoad) { return; }
            
            var baseStatsSaveData = saveState.GetState(typeof(BaseStatsSaveData)) as BaseStatsSaveData;
            if (baseStatsSaveData == null) { return; }

            currentLevel ??= new LazyValue<int>(GetInitialLevel);
            currentLevel.value = baseStatsSaveData.level;
            activeStatSheet = baseStatsSaveData.statSheet;
        }

        public bool? Evaluate(Predicate predicate)
        {
            var predicateCombatParticipant = predicate as PredicateBaseStats;
            return predicateCombatParticipant != null ? predicateCombatParticipant.Evaluate(this) : null;
        }
        #endregion
    }
}

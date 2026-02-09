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
        // See:  Progression for behaviour detail
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
        private const int _defaultLevelForNoCharacterProperties = 1;
        private static readonly Stat[] _nonModifyingStats = { Stat.InitialLevel, Stat.ExperienceReward, Stat.ExperienceToLevelUp };
        
        // State
        private LazyValue<int> currentLevel;
        private Dictionary<Stat, float> activeStatSheet;

        // Events
        public event Action<BaseStats, int, Dictionary<Stat, float>> onLevelUp;

        #region Static
        // Subset enum for those equipment should not touch
        public static Stat[] GetNonModifyingStats() => _nonModifyingStats;
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
        public bool CanLevelUp() => GetLevel() <= Progression.GetMaxLevel();
        public float GetStat(Stat stat) => GetBaseStat(stat) + GetAdditiveModifiers(stat);
        private float GetBaseStat(Stat stat)
        {
            BuildActiveStatSheetIfNull();
            return activeStatSheet.TryGetValue(stat, out var baseStat) ? baseStat : progression.GetStat(stat, characterProperties);
        }

        public bool GetStatForCalculatedStat(CalculatedStat calculatedStat, out Stat stat) => CalculatedStats.GetStatModifier(calculatedStat, out stat);

        public float GetCalculatedStat(CalculatedStat calculatedStat, int level, float statValue, int opponentLevel, float opponentStatValue)
        {
            return CalculatedStats.GetCalculatedStat(calculatedStat, level, statValue, opponentLevel, opponentStatValue);
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
            if (currentLevel.value >= Progression.GetMaxLevel()) { return; }
            currentLevel.value++;
            
            if (characterProperties == null || !characterProperties.incrementsStatsOnLevelUp) { return; }
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
            Dictionary<Stat, float> levelUpSheet = progression.GetLevelUpSheet(characterProperties, GetLevel(), activeStatSheet);
            foreach ((Stat stat, float statIncrement) in levelUpSheet)
            {
                if (!activeStatSheet.ContainsKey(stat)) { continue; }
                activeStatSheet[stat] += statIncrement;
            }
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

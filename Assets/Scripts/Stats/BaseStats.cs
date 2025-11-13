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
        [SerializeField] private CharacterProperties characterProperties;
        [SerializeField] private bool levelUpOnInstantiation = false;
        [SerializeField] private Progression progression;

        // Static/Const Parameters
        private const float _bonusStatOnLevelMidProbability = 0.3f; // 0 to 1
        private const float _bonusStatOnLevelHighProbability = 0.1f; // 0 to 1\
        private const int _defaultLevelForNoCharacterProperties = 1;
        private const int _maxLevel = 99;
        
        // State
        private bool awakeCalled;
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
        
        public static Dictionary<Stat, float> GetLevelUpSheet(int currentLevel, float currentStoic, float currentSmarts, CharacterProperties characterProperties, Progression progression)
        {
            var levelUpSheet = new Dictionary<Stat, float>
            {
                [Stat.Brawn] = progression.GetStat(Stat.Brawn, characterProperties) * (1 + GetBonusMultiplier()),
                [Stat.Beauty] = progression.GetStat(Stat.Beauty, characterProperties) * (1 + GetBonusMultiplier()),
                [Stat.Nimble] = progression.GetStat(Stat.Nimble, characterProperties) * (1 + GetBonusMultiplier()),
                [Stat.Luck] = progression.GetStat(Stat.Luck, characterProperties) * (1 + GetBonusMultiplier()),
                [Stat.Pluck] = progression.GetStat(Stat.Pluck, characterProperties) * (1 + GetBonusMultiplier()),
                [Stat.Stoic] = progression.GetStat(Stat.Stoic, characterProperties) * (1 + GetBonusMultiplier()), // Used for HP adjust
                [Stat.Smarts] = progression.GetStat(Stat.Smarts, characterProperties)* (1 + GetBonusMultiplier()), // Used for AP adjust
                [Stat.HP] = (1.5f * currentStoic / currentLevel) * (1 + 4 * GetBonusMultiplier()), // Take overall stat normalized to level, bonus swing larger for HP
                [Stat.AP] = (0.8f * currentSmarts / currentLevel) * (1 + 2 * GetBonusMultiplier()) // Take overall stat normalized to level, bonus swing larger for AP
            };
            return levelUpSheet;
        }
        private static float GetBonusMultiplier()
        {
            float roll = UnityEngine.Random.Range(0f, 1f);
            if (roll <= _bonusStatOnLevelHighProbability) { return 0.5f; }
            if (roll <= (_bonusStatOnLevelMidProbability + _bonusStatOnLevelHighProbability)) { return 0.25f; }
            return 0;
        }
        #endregion

        #region UnityMethods
        private void Awake()
        {
            awakeCalled = true;
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
            if (!CalculatedStats.GetStatModifier(calculatedStat, out Stat statModifier)) return 0f;
            
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
            var levelUpSheet = GetLevelUpSheet(GetLevel(),
                GetBaseStat(Stat.Stoic), GetBaseStat(Stat.Smarts),
                characterProperties, progression);

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

        public LoadPriority GetLoadPriority()
        {
            return LoadPriority.ObjectProperty;
        }

        public SaveState CaptureState()
        {
            if (!awakeCalled) { Awake(); }

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
            var baseStatsSaveData = saveState.GetState(typeof(BaseStatsSaveData)) as BaseStatsSaveData;
            if (baseStatsSaveData == null) { return; }

            if (!awakeCalled) { Awake(); }
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

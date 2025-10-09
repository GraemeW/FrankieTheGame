using System;
using System.Collections.Generic;
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
        [SerializeField] CharacterProperties characterProperties = null;
        [Range(1, 99)] [SerializeField] int defaultLevel = 1;
        [Range(1, 99)] [SerializeField] int maxLevel = 99;
        [SerializeField] bool levelUpOnInstantiation = false;
        [SerializeField] Progression progression = null;

        // Static/Const Parameters
        private const float _bonusStatOnLevelMidProbability = 0.3f; // 0 to 1
        private const float _bonusStatOnLevelHighProbability = 0.1f; // 0 to 1
        
        // State
        private bool awakeCalled = false;
        private LazyValue<int> currentLevel;
        private Dictionary<Stat, float> activeStatSheet = null;

        // Events
        public event Action<BaseStats, int, Dictionary<Stat, float>> onLevelUp;

        #region Static
        public static Stat[] GetNonModifyingStats()
        {
            // Subset enum for those equipment should not touch
            Stat[] nonModifyingStats = new Stat[] { Stat.ExperienceReward, Stat.ExperienceToLevelUp };
            return nonModifyingStats;
        }
        
        private static float GetProgressionStat(Stat stat, CharacterProperties characterProperties, Progression progression) => progression.GetStat(stat, characterProperties);
        // Default behavior from original implementation
        // Carried forward for level-up behavior
        
        public static Dictionary<Stat, float> GetLevelUpSheet(int currentLevel, float currentStoic, float currentSmarts, CharacterProperties characterProperties, Progression progression)
        {
            var levelUpSheet = new Dictionary<Stat, float>
            {
                [Stat.Brawn] = GetProgressionStat(Stat.Brawn, characterProperties, progression) * (1 + GetBonusMultiplier()),
                [Stat.Beauty] = GetProgressionStat(Stat.Beauty, characterProperties, progression) * (1 + GetBonusMultiplier()),
                [Stat.Nimble] = GetProgressionStat(Stat.Nimble, characterProperties, progression) * (1 + GetBonusMultiplier()),
                [Stat.Luck] = GetProgressionStat(Stat.Luck, characterProperties, progression) * (1 + GetBonusMultiplier()),
                [Stat.Pluck] = GetProgressionStat(Stat.Pluck, characterProperties, progression) * (1 + GetBonusMultiplier()),
                [Stat.Stoic] = GetProgressionStat(Stat.Stoic, characterProperties, progression) * (1 + GetBonusMultiplier()), // Used for HP adjust
                [Stat.Smarts] = GetProgressionStat(Stat.Smarts, characterProperties, progression) * (1 + GetBonusMultiplier()), // Used for AP adjust
                [Stat.HP] = (2 * currentStoic / currentLevel) * (1 + 4 * GetBonusMultiplier()), // Take overall stat normalized to level, bonus swing larger for HP
                [Stat.AP] = (2 * currentSmarts / currentLevel) * (1 + 4 * GetBonusMultiplier()) // Take overall stat normalized to level, bonus swing larger for AP
            };
            return levelUpSheet;
        }
        private static float GetBonusMultiplier()
        {
            float roll = UnityEngine.Random.Range(0f, 1f);
            if (roll <= _bonusStatOnLevelHighProbability) { return 0.5f; }
            else if (roll <= (_bonusStatOnLevelMidProbability + _bonusStatOnLevelHighProbability)) { return 0.25f; }
            else { return 0; }
        }
        #endregion

        #region UnityMethods
        private void Awake()
        {
            awakeCalled = true;
            currentLevel = new LazyValue<int>(() => defaultLevel);
        }

        private void Start()
        {
            currentLevel.ForceInit();
        }
        #endregion

        #region PublicGetters
        public CharacterProperties GetCharacterProperties() => characterProperties;
        public int GetLevel() => currentLevel.value;
        public bool CanLevelUp() => GetLevel() < maxLevel;
        public float GetStat(Stat stat) => GetBaseStat(stat) + GetAdditiveModifiers(stat);
        private float GetBaseStat(Stat stat)
        {
            BuildActiveStatSheetIfNull();
            if (activeStatSheet.ContainsKey(stat)) { return activeStatSheet[stat]; }

            return GetProgressionStat(stat, characterProperties, progression);
        }

        public bool GetStatForCalculatedStat(CalculatedStat calculatedStat, out Stat stat) => CalculatedStats.GetStatModifier(calculatedStat, out stat);

        public float GetCalculatedStat(CalculatedStat calculatedStat, float statValue, float opponentStatValue)
        {
            return CalculatedStats.GetCalculatedStat(calculatedStat, GetLevel(), statValue, opponentStatValue);
        }
        public float GetCalculatedStat(CalculatedStat calculatedStat)
        {
            if (CalculatedStats.GetStatModifier(calculatedStat, out Stat statModifier))
            {
                float stat = GetStat(statModifier);
                return CalculatedStats.GetCalculatedStat(calculatedStat, GetLevel(), stat, 0f);
            }
            return 0f;
        }

        public Dictionary<Stat, float> GetActiveStatSheet() => activeStatSheet;
            // NOTE:  Does NOT contain modifiers
        #endregion

        #region PublicFunctional
        public void OverrideLevel(int level)
        {
            currentLevel.value = level;
        }

        public void SetActiveStatSheet(Dictionary<Stat, float> activeStatSheet)
        {
            this.activeStatSheet = activeStatSheet;
        }

        public void AdjustStat(Stat stat, float value)
        {
            BuildActiveStatSheetIfNull();
            activeStatSheet[stat] += value;
        }

        public void IncrementLevel()
        {
            currentLevel.value++;
            if (characterProperties.incrementsStatsOnLevelUp)
            {
                Dictionary<Stat, float> levelUpSheet = IncrementStatsOnLevelUp();
                onLevelUp?.Invoke(this, GetLevel(), levelUpSheet);
            }
        }
        #endregion

        #region PrivateMethods
        private void BuildActiveStatSheetIfNull()
        {
            if (activeStatSheet != null) { return; }

            activeStatSheet = progression.GetStatSheet(characterProperties);
            if (levelUpOnInstantiation)
            {
                currentLevel.value = 1;
                for (int i = 1; i < defaultLevel; i++)
                {
                    IncrementLevel();
                }
            }
        }

        private float GetAdditiveModifiers(Stat stat)
        {
            float sumModifier = 0f;
            IModifierProvider[] modifierProviders = GetComponents<IModifierProvider>();
            foreach (IModifierProvider modifierProvider in modifierProviders)
            {
                foreach (float modifier in modifierProvider.GetAdditiveModifiers(stat))
                {
                    sumModifier += modifier;
                }
            }
            return sumModifier;
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
        [System.Serializable]
        class BaseStatsSaveData
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

            BaseStatsSaveData baseStatsSaveData = new BaseStatsSaveData
            {
                level = currentLevel.value,
                statSheet = activeStatSheet
            };
            SaveState saveState = new SaveState(GetLoadPriority(), baseStatsSaveData);
            return saveState;
        }

        public void RestoreState(SaveState saveState)
        {
            BaseStatsSaveData baseStatsSaveData = saveState.GetState(typeof(BaseStatsSaveData)) as BaseStatsSaveData;
            if (baseStatsSaveData == null) { return; }

            if (!awakeCalled) { Awake(); }
            currentLevel.value = baseStatsSaveData.level;
            activeStatSheet = baseStatsSaveData.statSheet;
        }

        public bool? Evaluate(Predicate predicate)
        {
            PredicateBaseStats predicateCombatParticipant = predicate as PredicateBaseStats;
            return predicateCombatParticipant != null ? predicateCombatParticipant.Evaluate(this) : null;
        }
        #endregion
    }
}

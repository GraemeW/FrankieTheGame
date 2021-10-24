using System.Collections.Generic;
using UnityEngine;
using Frankie.Utils;
using System;
using Frankie.Saving;

namespace Frankie.Stats
{
    public class BaseStats : MonoBehaviour, ISaveable
    {
        // Behavior Detail
        // Progression defines 1) Stats at level 1 --> pulled to the active stat sheet, which is what is saved
        // 2)  Modifiers used as basis for subsequent levels --> multiplied out by a random factor, and then added to active stat sheet

        // Tunables
        [SerializeField] CharacterProperties characterProperties = null;
        [Range(1, 99)] [SerializeField] int defaultLevel = 1;
        [Range(1, 99)] [SerializeField] int maxLevel = 99;
        [SerializeField] Progression progression = null;
        [Range(0, 1)][SerializeField] float bonusStatOnLevelMidProbability = 0.3f;
        [Range(0, 1)][SerializeField] float bonusStatOnLevelHighProbability = 0.1f;

        // State
        LazyValue<int> currentLevel;
        Dictionary<Stat, float> activeStatSheet = null;

        // Cached Reference
        Experience experience = null;

        // Events
        public event Action<int, Dictionary<Stat, float>> onLevelUp;

        // Static
        public static Stat[] GetNonModifyingStats()
        {
            // Subset enum for those equipment should not touch
            Stat[] nonModifyingStats = new Stat[] { Stat.EffectiveLevel, Stat.ExperienceReward, Stat.ExperienceToLevelUp };
            return nonModifyingStats;
        }

        private void Awake()
        {
            experience = GetComponent<Experience>();
            currentLevel = new LazyValue<int>(() => defaultLevel);
        }

        private void Start()
        {
            currentLevel.ForceInit();
        }

        public CharacterProperties GetCharacterProperties()
        {
            return characterProperties;
        }

        public float GetStat(Stat stat)
        {
            return GetBaseStat(stat) + GetAdditiveModifiers(stat);
        }

        public float GetBaseStat(Stat stat)
        {
            if (activeStatSheet == null)
            {
                BuildActiveStatSheet();
            }
            if (activeStatSheet.ContainsKey(stat)) { return activeStatSheet[stat]; }

            return GetProgressionStat(stat);
        }

        private float GetProgressionStat(Stat stat)
        {
            // Default behavior from original implementation
            // Carried forward for level-up behavior

            return progression.GetStat(stat, characterProperties, GetLevel());
        }

        public float GetCalculatedStat(CalculatedStat calculatedStat)
        {
            if (CalculatedStats.GetStatModifier(calculatedStat, out Stat statModifier))
            {
                return CalculatedStats.GetCalculatedStat(calculatedStat, GetLevel(), GetStat(statModifier));
            }
            return 0f;
        }

        private void BuildActiveStatSheet()
        {
            activeStatSheet = progression.GetStatSheet(characterProperties, GetLevel());
        }

        public Dictionary<Stat, float> GetActiveStatSheet() // NOTE:  Does NOT contain modifiers
        {
            return activeStatSheet;
        }

        public void SetActiveStatSheet(Dictionary<Stat, float> activeStatSheet)
        {
            this.activeStatSheet = activeStatSheet;
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

        public int GetLevel()
        {
            return currentLevel.value;
        }

        public void AdjustStat(Stat stat, float value)
        {
            if (activeStatSheet == null)
            {
                BuildActiveStatSheet();
            }

            activeStatSheet[stat] += value;
        }

        public bool UpdateLevel()
        {
            if (GetLevel() >= maxLevel) { return false; }

            if (experience.GetPoints() > GetStat(Stat.ExperienceToLevelUp))
            {
                float experienceBalance = experience.GetPoints() - GetStat(Stat.ExperienceToLevelUp);
                experience.ResetPoints();
                currentLevel.value++;

                Dictionary<Stat, float> levelUpSheet = IncrementStatsOnLevelUp();

                if (onLevelUp != null)
                {
                    onLevelUp.Invoke(GetLevel(), levelUpSheet);
                }

                experience.GainExperienceToLevel(experienceBalance); // Adjust the balance up, can re-call present function for multi-levels
                return true;
            }
            return false;
        }

        private Dictionary<Stat, float> IncrementStatsOnLevelUp()
        {
            Dictionary<Stat, float> levelUpSheet = new Dictionary<Stat, float>();
            levelUpSheet[Stat.HP] = GetProgressionStat(Stat.Stoic) * (1 + GetBonusMultiplier());
            levelUpSheet[Stat.AP] = GetProgressionStat(Stat.Smarts) * (1 + GetBonusMultiplier());
            levelUpSheet[Stat.Brawn] = GetProgressionStat(Stat.Brawn) * (1 + GetBonusMultiplier());
            levelUpSheet[Stat.Beauty] = GetProgressionStat(Stat.Beauty) * (1 + GetBonusMultiplier());
            levelUpSheet[Stat.Smarts] = GetProgressionStat(Stat.Smarts) * (1 + GetBonusMultiplier());
            levelUpSheet[Stat.Nimble] = GetProgressionStat(Stat.Nimble) * (1 + GetBonusMultiplier());
            levelUpSheet[Stat.Luck] = GetProgressionStat(Stat.Luck) * (1 + GetBonusMultiplier());
            levelUpSheet[Stat.Pluck] = GetProgressionStat(Stat.Pluck) * (1 + GetBonusMultiplier());
            levelUpSheet[Stat.Stoic] = GetProgressionStat(Stat.Stoic) / 2; // Stoic treatment different

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

        private float GetBonusMultiplier()
        {
            float randomSeed = UnityEngine.Random.Range(0f, 1f);
            if (randomSeed <= bonusStatOnLevelHighProbability) { return 0.5f; }
            else if (randomSeed <= (bonusStatOnLevelMidProbability + bonusStatOnLevelHighProbability)) { return 0.25f; }
            else { return 0; }
        }

        #region Interfaces
        // Save State
        [System.Serializable]
        struct BaseStatsSaveData
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
            BaseStatsSaveData data = (BaseStatsSaveData)saveState.GetState();
            currentLevel.value = data.level;
            activeStatSheet = data.statSheet;
        }
        #endregion
    }
}

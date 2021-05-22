using System.Collections.Generic;
using UnityEngine;
using Frankie.Utils;
using System;
using Frankie.Saving;

namespace Frankie.Stats
{
    public class BaseStats : MonoBehaviour, ISaveable
    {
        // Tunables
        [SerializeField] CharacterProperties characterProperties = null;
        [Range(1, 99)] [SerializeField] int defaultLevel = 1;
        [Range(1, 99)] [SerializeField] int maxLevel = 99;
        [SerializeField] bool shouldUseModifiers = false;
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

        private void Awake()
        {
            experience = GetComponent<Experience>();
            currentLevel = new LazyValue<int>(GetDefaultLevel);
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

            return progression.GetStat(stat, characterProperties, GetLevel()); // Default behavior from original implementation
        }

        private float GetProgressionStat(Stat stat)
        {
            return progression.GetStat(stat, characterProperties, GetLevel());
        }

        private void BuildActiveStatSheet()
        {
            activeStatSheet = progression.GetStatSheet(characterProperties, GetLevel());
        }

        private float GetAdditiveModifiers(Stat stat)
        {
            if (!shouldUseModifiers) return 0;

            // TODO:  Implement additive modifiers
            return 0;
        }

        public float GetStatForLevel(Stat stat, int level)
        {
            return progression.GetStat(stat, characterProperties, level);
        }

        public int GetLevel()
        {
            return currentLevel.value;
        }

        public void RefreshLevel()
        {
            currentLevel.ForceInit();
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

        private int GetDefaultLevel()
        {
            return defaultLevel;
        }

        // Save State
        [System.Serializable]
        struct BaseStatsSaveData
        {
            public int level;
            public Dictionary<Stat, float> statSheet;
        }

        public object CaptureState()
        {
            BaseStatsSaveData baseStatsSaveData = new BaseStatsSaveData
            {
                level = currentLevel.value,
                statSheet = activeStatSheet
            };
            return baseStatsSaveData;
        }

        public void RestoreState(object state)
        {
            BaseStatsSaveData data = (BaseStatsSaveData)state;
            currentLevel.value = data.level;
            activeStatSheet = data.statSheet;
        }
    }
}

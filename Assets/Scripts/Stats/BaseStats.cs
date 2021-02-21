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
        [SerializeField] bool shouldUseModifiers = false;
        [SerializeField] Progression progression = null;

        // State
        LazyValue<int> currentLevel;
        Dictionary<Stat, float> activeStatSheet = null;

        // Cached Reference
        Experience experience = null;

        // Events
        public event Action onLevelUp;

        private void Awake()
        {
            experience = GetComponent<Experience>();
            currentLevel = new LazyValue<int>(CalculateLevel);
        }

        private void Start()
        {
            currentLevel.ForceInit();
        }
        private void OnEnable()
        {
            if (experience != null)
            {
                experience.onExperienceGained += UpdateLevel;
            }
        }

        private void OnDisable()
        {
            if (experience != null)
            {
                experience.onExperienceGained -= UpdateLevel;
            }
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

        public void UpdateLevel()
        {
            int newLevel = CalculateLevel();
            if (newLevel > currentLevel.value)
            {
                currentLevel.value = newLevel;
                if (onLevelUp != null)
                {
                    onLevelUp();
                }
            }
        }

        private int CalculateLevel()
        {
            if (experience == null) { return defaultLevel; } // Default behavior

            float currentExperiencePoints = experience.GetPoints();
            int penultimateLevel = progression.GetLevels(Stat.ExperienceToLevelUp, characterProperties);
            for (int level = 1; level <= penultimateLevel; level++)
            {
                float experienceToLevelUp = progression.GetStat(Stat.ExperienceToLevelUp, characterProperties, level);
                if (experienceToLevelUp > currentExperiencePoints)
                {
                    return level;
                }
            }
            return penultimateLevel + 1;
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

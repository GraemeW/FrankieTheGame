using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frankie.Utils;
using System;

namespace Frankie.Stats
{
    public class BaseStats : MonoBehaviour
    {
        // Tunables
        [SerializeField] CharacterName characterName = CharacterName.None;
        [Range(1, 99)] [SerializeField] int defaultLevel = 1;
        [SerializeField] bool shouldUseModifiers = false;
        [SerializeField] Progression progression = null;

        // State
        LazyValue<int> currentLevel;

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

        public float GetStat(Stat stat)
        {
            return GetBaseStat(stat) + GetAdditiveModifiers(stat);
        }

        public float GetBaseStat(Stat stat)
        {
            return progression.GetStat(stat, characterName, GetLevel());
        }

        private float GetAdditiveModifiers(Stat stat)
        {
            if (!shouldUseModifiers) return 0;

            // TODO:  Implement additive modifiers
            return 0;
        }

        public float GetStatForLevel(Stat stat, int level)
        {
            return progression.GetStat(stat, characterName, level);
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
            int penultimateLevel = progression.GetLevels(Stat.ExperienceToLevelUp, characterName);
            for (int level = 1; level <= penultimateLevel; level++)
            {
                float experienceToLevelUp = progression.GetStat(Stat.ExperienceToLevelUp, characterName, level);
                if (experienceToLevelUp > currentExperiencePoints)
                {
                    return level;
                }
            }
            return penultimateLevel + 1;
        }
    }
}

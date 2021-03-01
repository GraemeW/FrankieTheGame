using UnityEngine;
using Frankie.Utils;
using System;
using Frankie.Saving;

namespace Frankie.Stats
{
    public class Experience : MonoBehaviour, ISaveable
    {
        // Tunables
        [SerializeField] float initialPoints = 0f;
        [SerializeField] float experienceScalingPerLevelDelta = 0.4f;

        // State
        LazyValue<float> currentPoints;

        // Cached References
        BaseStats baseStats = null;

        private void Awake()
        {
            baseStats = GetComponent<BaseStats>();
            currentPoints = new LazyValue<float>(GetInitialPoints);
        }

        private float GetInitialPoints()
        {
            return initialPoints;
        }

        private void Start()
        {
            currentPoints.ForceInit();
        }

        public static float GetScaledExperience(float experience, int levelDelta, float experienceScaleFactor)
        {
            float preMultiplier = 1f;
            if (levelDelta != 0)
            {
                preMultiplier = Mathf.Pow((1 - Mathf.Sign(levelDelta) * experienceScaleFactor), Mathf.Abs(levelDelta));
            }

            return experience * preMultiplier;
        }

        public bool GainExperienceToLevel(float points)
        {
            currentPoints.value += points;
            return baseStats.UpdateLevel();
        }

        public float GetPoints()
        {
            return currentPoints.value;
        }

        public void ResetPoints()
        {
            currentPoints.value = 0f;
        }

        public float GetExperienceScaling()
        {
            return experienceScalingPerLevelDelta;
        }

        public int GetExperienceRequiredToLevel()
        {
            return Mathf.RoundToInt((baseStats.GetStat(Stat.ExperienceToLevelUp) - GetPoints()));
        }

        // Save State
        public object CaptureState()
        {
            return currentPoints.value;
        }

        public void RestoreState(object state)
        {
            currentPoints.value = (float)state;
        }
    }
}
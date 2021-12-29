using UnityEngine;
using Frankie.Utils;
using System;
using Frankie.Saving;

namespace Frankie.Stats
{
    [RequireComponent(typeof(BaseStats))]
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
            return UpdateLevel();
        }

        private bool UpdateLevel()
        {
            if (!baseStats.CanLevelUp()) { return false; }
            float experienceToLevel = baseStats.GetStat(Stat.ExperienceToLevelUp);
            if (experienceToLevel <= 0f) { return false; } // Failsafe on invalid settings

            if (GetPoints() > experienceToLevel)
            {
                float experienceBalance = GetPoints() - experienceToLevel;
                ResetPoints();
                baseStats.IncrementLevel();

                GainExperienceToLevel(experienceBalance); // Adjust the balance up, can re-call present function for multi-levels

                return true;
            }
            return false;
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
            return Mathf.CeilToInt((baseStats.GetStat(Stat.ExperienceToLevelUp) - GetPoints()));
        }

        #region Interfaces
        // Save State
        public LoadPriority GetLoadPriority()
        {
            return LoadPriority.ObjectProperty;
        }

        public SaveState CaptureState()
        {
            SaveState saveState = new SaveState(GetLoadPriority(), currentPoints.value);
            return saveState;
        }

        public void RestoreState(SaveState saveState)
        {
            float points = (float)saveState.GetState();
            currentPoints.value = points;
        }
        #endregion
    }
}
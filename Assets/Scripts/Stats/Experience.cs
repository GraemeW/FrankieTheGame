using UnityEngine;
using Frankie.Utils;
using System;
using Frankie.Saving;
using System.Collections.Generic;

namespace Frankie.Stats
{
    [RequireComponent(typeof(BaseStats))]
    public class Experience : MonoBehaviour, ISaveable
    {
        // Tunables
        [SerializeField] float initialPoints = 0f;

        // State
        LazyValue<float> currentPoints;

        // Cached References
        BaseStats baseStats = null;

        #region Static
        // Static
        static float experienceScalingPerLevelDelta = 0.1f; // Not serialiazed since force universal for every character
        static float maxExperienceReward = 2500f;

        public static float GetScaledExperience(float experience, int levelDelta)
        {
            float preMultiplier = 1f;
            if (levelDelta != 0)
            {
                preMultiplier = Mathf.Pow((1 - Mathf.Sign(levelDelta) * experienceScalingPerLevelDelta), Mathf.Abs(levelDelta));
            }

            return experience * preMultiplier;
        }

        public static float GetMaxExperienceReward()
        {
            return maxExperienceReward;
        }
        #endregion

        #region UnityMethods
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
        #endregion

        #region PublicMethods
        public bool GainExperienceToLevel(float points)
        {
            currentPoints.value += points;
            return UpdateLevel();
        }

        public float GetPoints()
        {
            return currentPoints.value;
        }

        public void ResetPoints()
        {
            currentPoints.value = 0f;
        }

        public int GetExperienceRequiredToLevel()
        {
            return Mathf.CeilToInt((baseStats.GetStat(Stat.ExperienceToLevelUp) - GetPoints()));
        }
        #endregion

        #region PrivateMethods
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
        #endregion

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
            float points = (float)saveState.GetState(typeof(float));
            currentPoints.value = points;
        }
        #endregion
    }
}
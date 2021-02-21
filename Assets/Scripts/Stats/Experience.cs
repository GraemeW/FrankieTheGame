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

        // State
        LazyValue<float> currentPoints;

        // Cached References
        BaseStats baseStats = null;

        // Events
        public event Action onExperienceGained;

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

        public void GainExperience(float points)
        {
            currentPoints.value += points;
            if (onExperienceGained != null)
            {
                onExperienceGained();
            }
        }

        public void OverrideExperience(float points)
        {
            currentPoints.value = points;
            baseStats.RefreshLevel();
        }

        public float GetPoints()
        {
            return currentPoints.value;
        }

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
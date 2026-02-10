using UnityEngine;
using Frankie.Utils;
using Frankie.Saving;

namespace Frankie.Stats
{
    [RequireComponent(typeof(BaseStats))]
    public class Experience : MonoBehaviour, ISaveable
    {
        // Tunables
        [SerializeField] private float initialPoints;

        // State
        private LazyValue<float> currentPoints;

        // Cached References
        private BaseStats baseStats;

        // Const
        private const float _experienceScalingPerLevelDelta = 0.3f; // Not serialized since force universal for every character
        private const float _maxExperienceReward = 10000f; // 10-level cap with standard 999 exp to level
        
        #region Static
        public static float GetScaledExperience(float experience, int levelDelta)
        {
            float preMultiplier = levelDelta != 0 ? Mathf.Pow((1 - Mathf.Sign(levelDelta) * _experienceScalingPerLevelDelta), Mathf.Abs(levelDelta)) : 1f;
            return experience * preMultiplier;
        }

        public static float GetMaxExperienceReward() => _maxExperienceReward;
        #endregion

        #region UnityMethods
        private void Awake()
        {
            baseStats = GetComponent<BaseStats>();
            currentPoints = new LazyValue<float>(GetInitialPoints);
        }

        private void Start()
        {
            currentPoints.ForceInit();
        }
        #endregion

        #region PublicMethods
        public int GetExperienceRequiredToLevel() => Mathf.CeilToInt(baseStats.GetStat(Stat.ExperienceToLevelUp) - GetPoints());
        public bool GainExperienceToLevel(float points)
        {
            currentPoints.value += points;
            return UpdateLevel();
        }
        #endregion

        #region PrivateMethods
        private float GetInitialPoints() => initialPoints;
        private float GetPoints() => currentPoints.value;
        private void ResetPoints()
        {
            currentPoints.value = 0f;
        }
        
        private bool UpdateLevel()
        {
            if (!baseStats.CanLevelUp()) { return false; }
            float experienceToLevel = baseStats.GetStat(Stat.ExperienceToLevelUp);
            
            if (experienceToLevel <= 0f) { return false; }
            if (!(GetPoints() > experienceToLevel)) return false;
            
            float experienceBalance = GetPoints() - experienceToLevel;
            ResetPoints();
            baseStats.IncrementLevel();

            GainExperienceToLevel(experienceBalance); // Adjust the balance up, can re-call present function for multi-levels
            return true;
        }
        #endregion

        #region Interfaces
        // Save State
        public bool IsCorePlayerState() => true;
        public LoadPriority GetLoadPriority() => LoadPriority.ObjectProperty;

        public SaveState CaptureState()
        {
            currentPoints ??= new LazyValue<float>(GetInitialPoints);
            var saveState = new SaveState(GetLoadPriority(), currentPoints.value);
            return saveState;
        }

        public void RestoreState(SaveState saveState)
        {
            var points = (float)saveState.GetState(typeof(float));
            currentPoints ??= new LazyValue<float>(GetInitialPoints);
            currentPoints.value = points;
        }
        #endregion
    }
}

using UnityEngine;
using Frankie.Utils;
using Frankie.Stats;
using Frankie.Saving;
using System;
using Frankie.Core;
using System.Collections.Generic;
using Frankie.Inventory;

namespace Frankie.Combat
{
    [RequireComponent(typeof(BaseStats))]
    public class CombatParticipant : MonoBehaviour, ISaveable, IPredicateEvaluator
    {
        // Tunables
        [Header("Behavior, Hookups")]
        [SerializeField] bool friendly = false;
        [SerializeField] Sprite combatSprite = null;
        [SerializeField] AudioClip combatAudio = null;
        [SerializeField] MovingBackgroundProperties movingBackgroundProperties;

        [Header("Combat Properties")]
        [SerializeField] bool usesAP = true;
        [SerializeField] float battleStartCooldown = 1.0f;
        [SerializeField] float damageTimeSpan = 4.0f;
        [SerializeField] float fractionOfHPInstantOnRevival = 0.5f;

        // Cached References
        BaseStats baseStats = null;
        Experience experience = null;
        Knapsack knapsack = null;
        Equipment equipment = null;
        LootDispenser lootDispenser = null;

        // State
        bool inCombat = false;
        bool inCooldown = false;
        float cooldownTimer = 0f;
        float targetHP = 1f;
        float deltaHPTimeFraction = 0.0f;
        LazyValue<bool> isDead;
        LazyValue<float> currentHP;
        LazyValue<float> currentAP;

        // Events
        public event Action<bool> enterCombat;
        public event Action<CombatParticipant, StateAlteredData> stateAltered;
        public event Action<CombatParticipant, int, List<Tuple<string, int>>> characterLevelUp;

        #region UnityMethods
        private void Awake()
        {
            // Hard requirement
            baseStats = GetComponent<BaseStats>();
            // Not strictly necessary -- will fail elegantly
            experience = GetComponent<Experience>();
            knapsack = GetComponent<Knapsack>();
            equipment = GetComponent<Equipment>();
            lootDispenser = GetComponent<LootDispenser>();

            // State parameters
            currentHP = new LazyValue<float>(GetMaxHP);
            currentAP = new LazyValue<float>(GetMaxAP);
            isDead = new LazyValue<bool>(() => false);
        }

        private void OnEnable()
        {
            baseStats.onLevelUp += PassLevelUpMessage;
            if (equipment != null)
            {
                equipment.equipmentUpdated += ReconcileHPAP;
            }
        }

        private void OnDisable()
        {
            baseStats.onLevelUp -= PassLevelUpMessage;
            if (equipment != null)
            {
                equipment.equipmentUpdated -= ReconcileHPAP;
            }
        }

        private void Start()
        {
            currentHP.ForceInit();
            currentAP.ForceInit();
            isDead.ForceInit();
            targetHP = currentHP.value;
        }

        private void Update()
        {
            if (!inCombat) { return; }
            if (CheckIfDead()) { return; }
            UpdateCooldown();
        }

        private void FixedUpdate()
        {
            UpdateDamageDelayedHealth();
        }
        #endregion

        #region PublicGetters
        // Reference Passing

        public BaseStats GetBaseStats()
        {
            return baseStats;
        }

        // Core Properties
        public string GetCombatName()
        {
            // Split apart name on lower case followed by upper case w/ or w/out underscores
            return baseStats.GetCharacterProperties().GetCharacterNamePretty();
        }

        public Sprite GetCombatSprite()
        {
            return combatSprite;
        }

        public MovingBackgroundProperties GetMovingBackgroundProperties()
        {
            return movingBackgroundProperties;
        }

        public AudioClip GetAudioClip()
        {
            return combatAudio;
        }

        public bool GetFriendly()
        {
            return friendly;
        }

        public bool HasLoot()
        {
            if (lootDispenser == null) { return false; }

            return lootDispenser.HasLootReward();
        }

        // Stats
        public int GetLevel()
        {
            return baseStats.GetLevel();
        }

        public float GetMaxHP()
        {
            return baseStats.GetStat(Stat.HP);
        }

        public float GetMaxAP()
        {
            if (!usesAP) { return Mathf.Infinity; }
            return baseStats.GetStat(Stat.AP);
        }

        public float GetExperienceReward()
        {
            return baseStats.GetStat(Stat.ExperienceReward);
        }

        public float GetBattleStartCooldown()
        {
            return battleStartCooldown * GetCooldownMultiplier();
        }

        public float GetCooldownMultiplier()
        {
            return baseStats.GetCalculatedStat(CalculatedStat.CooldownFraction);
        }

        // Combat State
        public bool IsInCombat()
        {
            return inCombat;
        }

        public bool IsInCooldown()
        {
            return inCooldown;
        }

        public float GetCooldown()
        {
            return cooldownTimer;
        }

        public bool IsDead()
        {
            return isDead.value;
        }

        public float GetHP()
        {
            return currentHP.value;
        }


        public bool HasAP(float points)
        {
            return currentAP.value >= points;
        }

        public float GetAP()
        {
            if (!usesAP) { return Mathf.Infinity; }
            return currentAP.value;
        }
        #endregion

        #region PublicUtility
        public void SetCombatActive(bool enable)
        {
            inCombat = enable;
            if (!enable){ HaltHPScroll(); }
            enterCombat?.Invoke(enable);
        }

        public void SetCooldown(float seconds)
        {
            inCooldown = true;
            cooldownTimer = seconds * GetCooldownMultiplier();
            stateAltered?.Invoke(this, new StateAlteredData(StateAlteredType.CooldownSet));
        }

        public void AdjustHP(float points)
        {
            AdjustHPQuietly(points);

            if (stateAltered != null)
            {
                if (points < 0) { stateAltered.Invoke(this, new StateAlteredData(StateAlteredType.DecreaseHP, points)); }
                else if (points > 0) { stateAltered.Invoke(this, new StateAlteredData(StateAlteredType.IncreaseHP, points)); }
            }
        }

        public void AdjustHPQuietly(float points)
        {
            if (isDead.value) { return; }

            if (friendly) // Damage dealt is delayed, occurs over damageTimeSpan seconds
            {
                float unsafeHP = currentHP.value + points;
                targetHP = Mathf.Min(unsafeHP, baseStats.GetStat(Stat.HP));
                deltaHPTimeFraction = (Time.deltaTime / damageTimeSpan);
            }
            else
            {
                float unsafeHP = currentHP.value + points;
                currentHP.value = Mathf.Clamp(unsafeHP, 0f, baseStats.GetStat(Stat.HP));
            }
        }

        public void HaltHPScroll()
        {
            if (targetHP > currentHP.value) { return; } // Allow healing to occur post-battle
            deltaHPTimeFraction = 0f;
            targetHP = currentHP.value;
        }

        public void AdjustAP(float points)
        {
            if (isDead.value) { return; }
            if (!usesAP) { return; }

            float unsafeAP = currentAP.value + points;
            currentAP.value = Mathf.Clamp(unsafeAP, 0f, baseStats.GetStat(Stat.AP));

            if (stateAltered != null)
            {
                if (points < 0) { stateAltered.Invoke(this, new StateAlteredData(StateAlteredType.DecreaseAP, points)); }
                else if (points > 0) { stateAltered.Invoke(this, new StateAlteredData(StateAlteredType.IncreaseAP, points)); }
            }
        }

        public void Revive(float hp)
        {
            isDead.value = false;
            currentHP.value = hp * fractionOfHPInstantOnRevival;
            targetHP = hp;
            cooldownTimer = 0f;
            stateAltered?.Invoke(this, new StateAlteredData(StateAlteredType.Resurrected));
            stateAltered?.Invoke(this, new StateAlteredData(StateAlteredType.IncreaseHP));
        }

        public void AnnounceStateUpdate(StateAlteredData stateAlteredData)
        {
            if (stateAlteredData == null) { return; }

            stateAltered?.Invoke(this, stateAlteredData);
        }
        #endregion

        #region PrivateUtility
        private bool CheckIfDead()
        {
            if ((Mathf.Approximately(currentHP.value, 0f) || currentHP.value < 0) && isDead.value != true)
            {
                currentHP.value = 0f;
                targetHP = 0f;
                isDead.value = true;

                stateAltered?.Invoke(this, new StateAlteredData(StateAlteredType.Dead));
            }

            if (isDead.value == true) { return true; }
            return false;
        }

        private void UpdateDamageDelayedHealth()
        {
            if (friendly && !Mathf.Approximately(currentHP.value, targetHP))
            {
                deltaHPTimeFraction += (Time.deltaTime / damageTimeSpan);
                float unsafeHP = Mathf.Lerp(currentHP.value, targetHP, deltaHPTimeFraction);
                currentHP.value = Mathf.Clamp(unsafeHP, 0f, baseStats.GetStat(Stat.HP));

                stateAltered?.Invoke(this, new StateAlteredData(StateAlteredType.AdjustHPNonSpecific));
            }
        }

        private void UpdateCooldown()
        {
            if (cooldownTimer > 0) { cooldownTimer -= Time.deltaTime; }
            if (inCooldown && cooldownTimer <= 0)
            {
                inCooldown = false;
                stateAltered?.Invoke(this, new StateAlteredData(StateAlteredType.CooldownExpired));
            }
        }

        private void PassLevelUpMessage(int level, Dictionary<Stat, float> levelUpSheet)
        {
            List<Tuple<string, int>> statNameValuePairs = new List<Tuple<string, int>>();
            foreach (KeyValuePair<Stat, float> entry in levelUpSheet)
            {
                if (entry.Key == Stat.HP) { AdjustHP(entry.Value); }
                if (entry.Key == Stat.AP) { AdjustAP(entry.Value); }

                Tuple<string, int> statNameValuePair = new Tuple<string, int>(entry.Key.ToString(), Mathf.RoundToInt(entry.Value));
                statNameValuePairs.Add(statNameValuePair);
            }

            characterLevelUp?.Invoke(this, level, statNameValuePairs);
        }

        private void ReconcileHPAP(EquipableItem equipableItem)
        {
            if (currentHP.value > baseStats.GetStat(Stat.HP))
            {
                currentHP.value = baseStats.GetStat(Stat.HP);
            }
            if (currentAP.value > baseStats.GetStat(Stat.AP))
            {
                if (!usesAP) { currentAP.value = Mathf.Infinity; }
                else { currentAP.value = baseStats.GetStat(Stat.AP); }
            }
        }
        #endregion

        #region Interfaces
        // Save State
        [System.Serializable]
        struct CombatParticipantSaveData
        {
            public bool isDead;
            public float currentHP;
            public float currentAP;
        }
        public LoadPriority GetLoadPriority()
        {
            return LoadPriority.ObjectProperty;
        }

        SaveState ISaveable.CaptureState()
        {
            CombatParticipantSaveData combatParticipantSaveData = new CombatParticipantSaveData
            {
                isDead = isDead.value,
                currentHP = currentHP.value,
                currentAP = currentAP.value
            };
            SaveState saveState = new SaveState(GetLoadPriority(), combatParticipantSaveData);

            return saveState;
        }

        void ISaveable.RestoreState(SaveState saveState)
        {
            CombatParticipantSaveData data = (CombatParticipantSaveData)saveState.GetState();
            isDead.value = data.isDead;
            currentHP.value = data.currentHP;
            currentAP.value = data.currentAP;
            targetHP = currentHP.value;
        }

        // Predicate Evaluation
        public bool? Evaluate(Predicate predicate)
        {
            PredicateCombatParticipant predicateCombatParticipant = predicate as PredicateCombatParticipant;
            return predicateCombatParticipant != null ? predicateCombatParticipant.Evaluate(this) : null;
        }
        #endregion
    }
}

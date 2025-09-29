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
        [SerializeField] BattleEntityType battleEntityType = BattleEntityType.Standard;
        [SerializeField] bool usesAP = true;
        [SerializeField] float battleStartCooldown = 1.0f;
        [SerializeField] float damageTimeSpan = 4.0f;
        [SerializeField] float fractionOfHPInstantOnRevival = 0.5f;

        // Cached References
        BaseStats baseStats = null;
        Equipment equipment = null;
        LootDispenser lootDispenser = null;

        // State
        bool awakeCalled = false;
        bool inCombat = false;
        float cooldownTimer = 0f;
        float targetHP = 1f;
        float deltaHPTimeFraction = 0.0f;
        LazyValue<bool> isDead;
        LazyValue<float> currentHP;
        LazyValue<float> currentAP;

        // Events
        public event Action<bool> enterCombat;
        public event Action<StateAlteredEvent> stateAltered;

        #region UnityMethods
        private void Awake()
        {
            awakeCalled = true;

            // Hard requirement
            baseStats = GetComponent<BaseStats>();
            // Not strictly necessary -- will fail elegantly
            equipment = GetComponent<Equipment>();
            lootDispenser = GetComponent<LootDispenser>();

            // State parameters
            currentHP = new LazyValue<float>(GetMaxHP);
            currentAP = new LazyValue<float>(GetMaxAP);
            isDead = new LazyValue<bool>(() => false);
        }

        private void OnEnable()
        {
            baseStats.onLevelUp += ParseLevelUpMessage;
            if (equipment != null)
            {
                equipment.equipmentUpdated += ReconcileHPAP;
            }
        }

        private void OnDisable()
        {
            baseStats.onLevelUp -= ParseLevelUpMessage;
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

        #region SimpleGetters
        public string GetCombatName() => baseStats.GetCharacterProperties().GetCharacterNamePretty();
            // Split apart name on lower case followed by upper case w/ or w/out underscores
        public Sprite GetCombatSprite() => combatSprite;
        public MovingBackgroundProperties GetMovingBackgroundProperties() => movingBackgroundProperties;
        public AudioClip GetAudioClip() => combatAudio;
        public bool GetFriendly() => friendly;
        public BattleEntityType GetBattleEntityType() => battleEntityType;
        public bool HasLoot() => lootDispenser == null ? false : lootDispenser.HasLootReward();
        #endregion

        #region StatsInterface
        public CharacterProperties GetCharacterProperties() => baseStats.GetCharacterProperties();
        public float GetStat(Stat stat) => baseStats.GetStat(stat);
        public float GetCalculatedStat(CalculatedStat calculatedStat) => baseStats.GetCalculatedStat(calculatedStat); // Simple no-contest
        public float GetCalculatedStat(CalculatedStat calculatedStat, CombatParticipant recipient) // Contested
        {
            if (!baseStats.GetStatForCalculatedStat(calculatedStat, out Stat stat)) { return 0f; }
            float statValue = baseStats.GetStat(stat);
            float opponentStatValue = recipient != null ? recipient.GetStat(stat) : 0f;
            return baseStats.GetCalculatedStat(calculatedStat, statValue, opponentStatValue);
        }
        public float GetMaxHP() => baseStats.GetStat(Stat.HP);
        public float GetMaxAP() => usesAP ? baseStats.GetStat(Stat.AP) : Mathf.Infinity;
        public int GetLevel() => baseStats.GetLevel();
        public float GetExperienceReward() => baseStats.GetStat(Stat.ExperienceReward);
        public float GetBattleStartCooldown() => battleStartCooldown * GetCooldownMultiplier();
        public float GetCooldownMultiplier() => baseStats.GetCalculatedStat(CalculatedStat.CooldownFraction);
        public float GetRunSpeed() => baseStats.GetCalculatedStat(CalculatedStat.RunSpeed);
        #endregion

        #region StateGetters
        public bool IsInCombat() => inCombat;
        public bool IsInCooldown() => cooldownTimer > 0f;
        public float GetCooldown() => cooldownTimer;
        public bool IsDead() => isDead.value;
        public float GetHP() => currentHP.value;
        public bool HasAP(float points) => currentAP.value >= points;
        public float GetAP() => usesAP ? currentAP.value : Mathf.Infinity;
        #endregion

        #region PublicUtility
        public void SetCombatActive(bool enable)
        {
            inCombat = enable;
            if (enable)
            {
                if (IsInCooldown()) { AnnounceStateUpdate(StateAlteredType.CooldownSet, cooldownTimer); }
            }
            else
            {
                HaltHPScroll();
                if (IsInCooldown()) { AnnounceStateUpdate(StateAlteredType.CooldownSet, Mathf.Infinity); }
            }
            
            enterCombat?.Invoke(enable);
        }

        public bool CheckIfDead() // Called via Unity Events
        {
            if ((Mathf.Approximately(currentHP.value, 0f) || currentHP.value < 0) && isDead.value != true)
            {
                currentHP.value = 0f;
                targetHP = 0f;
                isDead.value = true;

                AnnounceStateUpdate(StateAlteredType.Dead);
            }

            if (isDead.value == true) { return true; }
            return false;
        }

        public void SetCooldown(float seconds)
        {
            cooldownTimer = seconds * GetCooldownMultiplier();
            AnnounceStateUpdate(StateAlteredType.CooldownSet, cooldownTimer);
        }

        public void AdjustHP(float points)
        {
            AdjustHPQuietly(points);

            if (points < 0) { AnnounceStateUpdate(StateAlteredType.DecreaseHP, points); }
            else if (points > 0) { AnnounceStateUpdate(StateAlteredType.IncreaseHP, points); }
        }

        public void AdjustHPQuietly(float points)
        {
            if (isDead.value) { return; }

            if (friendly) // Damage dealt is delayed, occurs over damageTimeSpan seconds
            {
                float unsafeHP = targetHP + points;
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
            AdjustAPQuietly(points);

            if (points < 0) { AnnounceStateUpdate(StateAlteredType.DecreaseAP, points); }
            else if (points > 0) { AnnounceStateUpdate(StateAlteredType.IncreaseAP, points); }
        }

        public void AdjustAPQuietly(float points)
        {
            if (isDead.value) { return; }
            if (!usesAP) { return; }

            float unsafeAP = currentAP.value + points;
            currentAP.value = Mathf.Clamp(unsafeAP, 0f, baseStats.GetStat(Stat.AP));
            AnnounceStateUpdate(StateAlteredType.AdjustAPNonSpecific, points);
        }

        public void Kill()
        {
            AdjustHP(-baseStats.GetStat(Stat.HP) * 10f);
        }

        public void Revive(float hp)
        {
            isDead.value = false;
            currentHP.value = hp * fractionOfHPInstantOnRevival;
            targetHP = hp;
            cooldownTimer = 0f;
            AnnounceStateUpdate(StateAlteredType.Resurrected);
            AnnounceStateUpdate(StateAlteredType.IncreaseHP);
        }

        public void AnnounceStateUpdate(StateAlteredEvent stateAlteredEvent)
        {
            if (stateAlteredEvent == null) { return; }
            stateAltered?.Invoke(stateAlteredEvent);
        }

        public void AnnounceStateUpdate(StateAlteredType stateAlteredType)
        {
            StateAlteredEvent stateAlteredEvent = new StateAlteredEvent(this, stateAlteredType);
            AnnounceStateUpdate(stateAlteredEvent);
        }

        public void AnnounceStateUpdate(StateAlteredType stateAlteredType, float points)
        {
            StateAlteredEvent stateAlteredEvent = new StateAlteredEvent(this, stateAlteredType, points);
            AnnounceStateUpdate(stateAlteredEvent);
        }

        public void AnnounceStateUpdate(StateAlteredType stateAlteredType, PersistentStatus persistentStatus)
        {
            StateAlteredEvent stateAlteredEvent = new StateAlteredEvent(this, stateAlteredType, persistentStatus);
            AnnounceStateUpdate(stateAlteredEvent);
        }

        public void AnnounceStateUpdate(StateAlteredType stateAlteredType, Stat stat, float points)
        {
            StateAlteredEvent stateAlteredEvent = new StateAlteredEvent(this, stateAlteredType, stat, points);
            AnnounceStateUpdate(stateAlteredEvent);
        }
        #endregion

        #region PrivateUtility
        private void UpdateDamageDelayedHealth()
        {
            if (friendly && !Mathf.Approximately(currentHP.value, targetHP))
            {
                deltaHPTimeFraction += (Time.deltaTime / damageTimeSpan);
                float unsafeHP = Mathf.Lerp(currentHP.value, targetHP, deltaHPTimeFraction);
                currentHP.value = Mathf.Clamp(unsafeHP, 0f, baseStats.GetStat(Stat.HP));

                AnnounceStateUpdate(StateAlteredType.AdjustHPNonSpecific);
            }
        }

        private void UpdateCooldown()
        {
            if (IsInCooldown())
            {
                cooldownTimer -= Time.deltaTime;
                if (!IsInCooldown()) // Immediately after adjustment to check if cooldown flipped
                {
                    AnnounceStateUpdate(StateAlteredType.CooldownExpired);
                }
            }
        }

        private void ParseLevelUpMessage(BaseStats baseStats, int level, Dictionary<Stat, float> levelUpSheet)
        {
            foreach (KeyValuePair<Stat, float> entry in levelUpSheet)
            {
                if (entry.Key == Stat.HP) { AdjustHPQuietly(entry.Value); }
                if (entry.Key == Stat.AP) { AdjustAPQuietly(entry.Value); }
            }
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
        class CombatParticipantSaveData
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
            if (!awakeCalled) { Awake(); }

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
            CombatParticipantSaveData combatParticipantSaveData = saveState.GetState(typeof(CombatParticipantSaveData)) as CombatParticipantSaveData;
            if (combatParticipantSaveData == null) { return; }

            if (!awakeCalled) { Awake(); }
            isDead.value = combatParticipantSaveData.isDead;
            currentHP.value = combatParticipantSaveData.currentHP;
            currentAP.value = combatParticipantSaveData.currentAP;
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

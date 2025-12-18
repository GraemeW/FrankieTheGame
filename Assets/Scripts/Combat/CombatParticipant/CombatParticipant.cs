using System;
using System.Collections.Generic;
using UnityEngine;
using Frankie.Core;
using Frankie.Utils;
using Frankie.Stats;
using Frankie.Saving;
using Frankie.Inventory;

namespace Frankie.Combat
{
    [RequireComponent(typeof(BaseStats))]
    public class CombatParticipant : MonoBehaviour, ISaveable, IPredicateEvaluator
    {
        // Tunables
        [Header("Behavior, Hookups")]
        [SerializeField] private bool friendly = false;
        [SerializeField] private Sprite combatSprite;
        [SerializeField] private BattleEntityType battleEntityType = BattleEntityType.Standard;
        [SerializeField] [Range(0.2f, 2.0f)] private float spriteScaleFineTune = 1.0f;
        [SerializeField] private AudioClip combatAudio;
        [SerializeField] private MovingBackgroundProperties movingBackgroundProperties;

        [Header("Combat Properties")]
        [SerializeField] private BattleRow preferredBattleRow = BattleRow.Any;
        [SerializeField] private bool canRunFrom = true;
        [SerializeField] private bool usesAP = true;
        [SerializeField] private float damageTimeSpan = 4.0f;
        [SerializeField] private float fractionOfHPInstantOnRevival = 0.5f;
        [SerializeField] private bool shouldDestroySelfOnDeath = true;

        [Header("Cooldowns")]
        [SerializeField] private float cooldownMin = 0.2f;
        [SerializeField] private float cooldownMax = 10.0f;
        [SerializeField] private float cooldownAtBattleStartPlayer = 2.5f;
        [SerializeField] private float cooldownAtBattleStartAlt = 4.0f;
        [SerializeField] private float cooldownBattleAdvantageAdder = -4.0f;
        [SerializeField] private float cooldownBattleDisadvantageAdder = 4.0f;
        [SerializeField] private float cooldownRunFailAdder = 5.0f;

        // Cached References
        private BaseStats baseStats;
        private Equipment equipment;
        private LootDispenser lootDispenser;

        // State
        private bool awakeCalled = false;
        private bool inCombat = false;
        private float cooldownTimer;
        private float cooldownStore;
        private float targetHP = 1f;
        private float deltaHPTimeFraction;
        private LazyValue<bool> isDead;
        private LazyValue<float> currentHP;
        private LazyValue<float> currentAP;
        private readonly List<StateEvent> stateListeners = new();

        // Events
        public event Action enteredBattle;
        public delegate void StateEvent(StateAlteredInfo stateAlteredInfo);
        public event StateEvent stateAltered;

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
            if (equipment != null) { equipment.equipmentUpdated += ReconcileHPAP;}
        }

        private void OnDisable()
        {
            baseStats.onLevelUp -= ParseLevelUpMessage;
            if (equipment != null) { equipment.equipmentUpdated -= ReconcileHPAP; }
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
        public BattleEntityType GetBattleEntityType() => battleEntityType;
        public float GetSpriteScaleFineTune() => spriteScaleFineTune;
        public MovingBackgroundProperties GetMovingBackgroundProperties() => movingBackgroundProperties;
        public AudioClip GetAudioClip() => combatAudio;
        public bool GetFriendly() => friendly;
        public BattleRow GetPreferredBattleRow() => preferredBattleRow;
        public bool HasLoot() => lootDispenser != null && lootDispenser.HasLootReward();
        public bool ShouldDestroySelfOnDeath() => shouldDestroySelfOnDeath;
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
            int opponentLevel = recipient != null ? recipient.GetLevel() : 0;
            return baseStats.GetCalculatedStat(calculatedStat, GetLevel(), statValue, opponentLevel, opponentStatValue);
        }
        public float GetMaxHP() => baseStats.GetStat(Stat.HP);
        public float GetMaxAP() => usesAP ? baseStats.GetStat(Stat.AP) : Mathf.Infinity;
        public int GetLevel() => baseStats.GetLevel();
        public float GetExperienceReward() => baseStats.GetStat(Stat.ExperienceReward);
        public float GetCooldownMultiplier() => baseStats.GetCalculatedStat(CalculatedStat.CooldownFraction);
        public bool GetRunAwayable() => canRunFrom;
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
        public void SubscribeToBattleStateChanges(bool enable)
        {
            if (enable)
            {
                BattleEventBus<BattleStateChangedEvent>.SubscribeToEvent(HandleBattleStateChangedEvent);
                enteredBattle?.Invoke();
            }
            else { BattleEventBus<BattleStateChangedEvent>.UnsubscribeFromEvent(HandleBattleStateChangedEvent); }
        }

        public void SetupSelfDestroyOnBattleComplete()
        {
            if (!shouldDestroySelfOnDeath) { return; }

            BattleEventBus<BattleStateChangedEvent>.SubscribeToEvent(SelfDestroyOnBattleComplete);
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

            return isDead.value;
        }

        public void InitializeCooldown(bool isPlayer, bool? isBattleAdvantage)
        {
            cooldownStore = 0.0f;
            if (isBattleAdvantage == true) { cooldownStore += cooldownBattleAdvantageAdder; }
            else if (isBattleAdvantage == false) { cooldownStore += cooldownBattleDisadvantageAdder; }

            float initialCooldown = isPlayer ? cooldownAtBattleStartPlayer : cooldownAtBattleStartAlt;
            SetCooldown(initialCooldown);
        }

        public void SetCooldown(float seconds)
        {
            cooldownTimer = seconds * GetCooldownMultiplier();
            if (!float.IsPositiveInfinity(cooldownTimer)) { ReconcileCooldownStore(); }
            AnnounceStateUpdate(StateAlteredType.CooldownSet, cooldownTimer);
        }

        public void PauseCooldown()
        {
            SetCooldown(Mathf.Infinity);
        }

        public void IncrementCooldownStoreForRun()
        {
            cooldownStore += cooldownRunFailAdder;
            ReconcileCooldownStore();
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

        public void SelfImplode()
        {
            AdjustHP(-baseStats.GetStat(Stat.HP) * 10f);
        }

        public void Revive(float hp, bool announceStateUpdates = true)
        {
            isDead.value = false;
            currentHP.value = hp * fractionOfHPInstantOnRevival;
            targetHP = hp;
            cooldownTimer = 0f;

            if (announceStateUpdates)
            {
                AnnounceStateUpdate(StateAlteredType.Resurrected);
                AnnounceStateUpdate(StateAlteredType.IncreaseHP);
            }
        }

        public void Revive(bool announceStateUpdates = true)
        {
            Revive(GetMaxHP(), announceStateUpdates);
        }
        #endregion

        #region StateUpdates
        public void SubscribeToStateUpdates(StateEvent handler)
        {
            // Note:  Obviously do NOT double subscribe to both CombatParticipant and BattleEventBus
            if (stateListeners.Contains(handler)) { return; }

            stateListeners.Add(handler);
            stateAltered += handler;
        }

        public void UnsubscribeToStateUpdates(StateEvent handler)
        {
            stateListeners.Remove(handler);
            stateAltered -= handler;
        }

        public void AnnounceStateUpdate(StateAlteredInfo stateAlteredInfo)
        {
            if (stateAlteredInfo == null) { return; }

            stateAltered?.Invoke(stateAlteredInfo);
            BattleEventBus<StateAlteredInfo>.Raise(stateAlteredInfo); // Separate announce for generic BattleEventBus subscribers
        }

        public void AnnounceStateUpdate(StateAlteredType stateAlteredType)
        {
            var stateAlteredInfo = new StateAlteredInfo(this, stateAlteredType);
            AnnounceStateUpdate(stateAlteredInfo);
        }

        public void AnnounceStateUpdate(StateAlteredType stateAlteredType, float points)
        {
            var stateAlteredInfo = new StateAlteredInfo(this, stateAlteredType, points);
            AnnounceStateUpdate(stateAlteredInfo);
        }

        public void AnnounceStateUpdate(StateAlteredType stateAlteredType, PersistentStatus persistentStatus)
        {
            var stateAlteredInfo = new StateAlteredInfo(this, stateAlteredType, persistentStatus);
            AnnounceStateUpdate(stateAlteredInfo);
        }

        public void AnnounceStateUpdate(StateAlteredType stateAlteredType, Stat stat, float points)
        {
            var stateAlteredInfo = new StateAlteredInfo(this, stateAlteredType, stat, points);
            AnnounceStateUpdate(stateAlteredInfo);
        }
        #endregion

        #region PrivateUtility
        private void HandleBattleStateChangedEvent(BattleStateChangedEvent battleStateChangedEvent)
        {
            SetCombatActive(battleStateChangedEvent.battleState == BattleState.Combat);
            if (battleStateChangedEvent.battleState == BattleState.Outro) { SubscribeToBattleStateChanges(false); }
        }
        
        private void SetCombatActive(bool enable)
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
        }
        
        private void SelfDestroyOnBattleComplete(BattleStateChangedEvent battleStateChangedEvent)
        {
            if (battleStateChangedEvent.battleState == BattleState.Complete)
            {
                BattleEventBus<BattleStateChangedEvent>.UnsubscribeFromEvent(SelfDestroyOnBattleComplete);
                Destroy(gameObject);
            }
        }

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

        private void ReconcileCooldownStore()
        {
            if (cooldownStore == 0.0f) { return; }

            //Debug.Log($"Pre-Reconcile:  Cooldown @ {cooldownTimer}, Store @ {cooldownStore}");
            float tryCooldown = cooldownTimer + cooldownStore;
            if (cooldownStore < 0.0f)
            {
                if (tryCooldown < cooldownMin)
                {
                    float deltaCooldown = cooldownTimer - cooldownMin;
                    cooldownTimer = cooldownMin;
                    cooldownStore += deltaCooldown;
                    cooldownStore = Mathf.Min(cooldownStore, 0.0f);
                }
                else { cooldownTimer = tryCooldown; cooldownStore = 0.0f; }
            }
            else
            {
                if (tryCooldown > cooldownMax)
                {
                    float deltaCooldown = cooldownMax - cooldownTimer;
                    cooldownTimer = cooldownMax;
                    cooldownStore -= deltaCooldown;
                    cooldownStore = Mathf.Max(0.0f, cooldownStore);
                }
                else { cooldownTimer = tryCooldown; cooldownStore = 0.0f; }
            }
            //Debug.Log($"Post-Reconcile:  Cooldown @ {cooldownTimer}, Store @ {cooldownStore}");
        }
        
        private void ParseLevelUpMessage(BaseStats passBaseStats, int level, Dictionary<Stat, float> levelUpSheet)
        {
            foreach (KeyValuePair<Stat, float> entry in levelUpSheet)
            {
                if (entry.Key == Stat.HP) { AdjustHPQuietly(entry.Value); }
                if (entry.Key == Stat.AP) { AdjustAPQuietly(entry.Value); }
            }
        }
        
        private void HaltHPScroll()
        {
            if (targetHP > currentHP.value) { return; } // Allow healing to occur post-battle
            deltaHPTimeFraction = 0f;
            targetHP = currentHP.value;
        }

        private void ReconcileHPAP(EquipableItem equipableItem)
        {
            if (currentHP.value > baseStats.GetStat(Stat.HP))
            {
                currentHP.value = baseStats.GetStat(Stat.HP);
            }
            if (currentAP.value > baseStats.GetStat(Stat.AP))
            {
                currentAP.value = !usesAP ? Mathf.Infinity : baseStats.GetStat(Stat.AP);
            }
        }
        #endregion

        #region Interfaces
        // Save State
        [Serializable]
        class CombatParticipantSaveData
        {
            public bool isDead;
            public float currentHP;
            public float currentAP;
        }
        public LoadPriority GetLoadPriority() => LoadPriority.ObjectProperty;

        SaveState ISaveable.CaptureState()
        {
            if (!awakeCalled) { Awake(); }

            var combatParticipantSaveData = new CombatParticipantSaveData
            {
                isDead = isDead.value,
                currentHP = currentHP.value,
                currentAP = currentAP.value
            };
            var saveState = new SaveState(GetLoadPriority(), combatParticipantSaveData);

            return saveState;
        }

        void ISaveable.RestoreState(SaveState saveState)
        {
            if (saveState.GetState(typeof(CombatParticipantSaveData)) is not CombatParticipantSaveData combatParticipantSaveData) { return; }

            if (!awakeCalled) { Awake(); }
            isDead.value = combatParticipantSaveData.isDead;
            currentHP.value = combatParticipantSaveData.currentHP;
            currentAP.value = combatParticipantSaveData.currentAP;
            targetHP = currentHP.value;
        }

        // Predicate Evaluation
        public bool? Evaluate(Predicate predicate)
        {
            var predicateCombatParticipant = predicate as PredicateCombatParticipant;
            return predicateCombatParticipant != null ? predicateCombatParticipant.Evaluate(this) : null;
        }
        #endregion
    }
}

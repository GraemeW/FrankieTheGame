using UnityEngine;
using Frankie.Utils;
using Frankie.Stats;
using Frankie.Saving;
using System;

namespace Frankie.Combat
{
    [RequireComponent(typeof(BaseStats))]
    [RequireComponent(typeof(SkillHandler))]
    public class CombatParticipant : MonoBehaviour, ISaveable
    {
        // Tunables
        [Header("Behavior, Hookups")]
        [SerializeField] bool friendly = false;
        [SerializeField] Sprite combatSprite = null;

        [Header("Combat Properties")]
        [SerializeField] float damageTimeSpan = 4.0f;
        [SerializeField] float fractionOfHPInstantOnRevival = 0.5f;

        // Cached References
        BaseStats baseStats = null;

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

        private void Awake()
        {
            baseStats = GetComponent<BaseStats>();
            currentHP = new LazyValue<float>(GetMaxHP);
            currentAP = new LazyValue<float>(GetMaxAP);
            isDead = new LazyValue<bool>(SpawnAlive);
        }

        private void Start()
        {
            currentHP.ForceInit();
            currentAP.ForceInit();
            isDead.ForceInit();
            targetHP = currentHP.value;
        }

        private void OnEnable()
        {
            baseStats.onLevelUp += RestoreHPOnLevelUp;
            cooldownTimer = 0f;
        }

        private void OnDisable()
        {
            baseStats.onLevelUp -= RestoreHPOnLevelUp;
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

        public void SetCombatActive(bool enable)
        {
            inCombat = enable;
            if (!enable){ HaltHPScroll(); }
            if (enterCombat != null)
            {
                enterCombat.Invoke(enable);
            }
        }

        public bool IsInCombat()
        {
            return inCombat;
        }

        public void AdjustHP(float points)
        {
            if (isDead.value) { return; }

            if (friendly) // Damage dealt is delayed, occurs over damageTimeSpan seconds
            {
                targetHP += points;
                deltaHPTimeFraction = (Time.deltaTime / damageTimeSpan);
            }
            else
            {
                float unsafeHP = currentHP.value + points;
                currentHP.value = Mathf.Clamp(unsafeHP, 0f, baseStats.GetStat(Stat.HP));
            }

            if (stateAltered != null)
            {
                if (points < 0) { stateAltered.Invoke(this, new StateAlteredData(StateAlteredType.DecreaseHP, points)); }
                else if (points > 0) { stateAltered.Invoke(this, new StateAlteredData(StateAlteredType.IncreaseHP, points)); }
            }
        }

        public void HaltHPScroll()
        {
            deltaHPTimeFraction = 0f;
            targetHP = currentHP.value;
        }

        public void AdjustAP(float points)
        {
            if (isDead.value) { return; }

            float unsafeAP = currentAP.value + points;
            currentAP.value = Mathf.Clamp(unsafeAP, 0f, baseStats.GetStat(Stat.AP));

            if (stateAltered != null)
            {
                if (points < 0) { stateAltered.Invoke(this, new StateAlteredData(StateAlteredType.DecreaseAP, points)); }
                else if (points > 0) { stateAltered.Invoke(this, new StateAlteredData(StateAlteredType.IncreaseAP, points)); }
            }
        }

        public void ApplyStatusEffect(StatusEffect statusEffect)
        {
            ActiveStatusEffect activeStatusEffect = gameObject.AddComponent(typeof(ActiveStatusEffect)) as ActiveStatusEffect;
            activeStatusEffect.Setup(statusEffect);

            if (stateAltered != null)
            {
                stateAltered.Invoke(this, new StateAlteredData(StateAlteredType.StatusEffectApplied, statusEffect.statusEffectType));
            }
        }

        public void ResurrectCharacter(float hp)
        {
            isDead.value = false;
            currentHP.value = hp * fractionOfHPInstantOnRevival;
            targetHP = hp;
            cooldownTimer = 0f;
            if (stateAltered != null)
            {
                stateAltered.Invoke(this, new StateAlteredData(StateAlteredType.Resurrected));
            }
        }

        public void SetCooldown(float seconds)
        {
            inCooldown = true;
            cooldownTimer = seconds;
            if (stateAltered != null)
            {
                stateAltered.Invoke(this, new StateAlteredData(StateAlteredType.CooldownSet));
            }
        }

        public bool GetFriendly()
        {
            return friendly;
        }

        public float GetHP()
        {
            return currentHP.value;
        }

        public float GetAP()
        {
            return currentAP.value;
        }

        public Sprite GetCombatSprite()
        {
            return combatSprite;
        }

        public string GetCombatName()
        {
            // Split apart name on lower case followed by upper case w/ or w/out underscores
            return baseStats.GetCharacterProperties().GetCharacterNamePretty();
        }

        public bool IsDead()
        {
            return isDead.value;
        }

        public bool IsInCooldown()
        {
            return inCooldown;
        }

        public void SetMaxHP()
        {
            currentHP.value = baseStats.GetStat(Stat.HP);
        }

        public void RestoreHPOnLevelUp()
        {
            float maxHP = baseStats.GetStat(Stat.HP);
            float differenceToMaxHP = Mathf.Clamp((maxHP - currentHP.value), 0, maxHP);
            AdjustHP(differenceToMaxHP);
        }

        public float GetMaxHP()
        {
            return baseStats.GetStat(Stat.HP);
        }

        public float GetMaxAP()
        {
            return baseStats.GetStat(Stat.AP);
        }

        private bool SpawnAlive()
        {
            return false;
        }

        // Private functions
        private bool CheckIfDead()
        {
            if (Mathf.Approximately(currentHP.value, 0f) || currentHP.value < 0)
            {
                currentHP.value = 0f;
                targetHP = 0f;
                isDead.value = true;
                if (!friendly)
                {
                    AwardExperience();
                }
                if (stateAltered != null)
                {
                    stateAltered.Invoke(this, new StateAlteredData(StateAlteredType.Dead));
                }
            }
            if (isDead.value) { return true; }
            return false;
        }

        private void UpdateDamageDelayedHealth()
        {
            if (friendly && !Mathf.Approximately(currentHP.value, targetHP))
            {
                deltaHPTimeFraction += (Time.deltaTime / damageTimeSpan);
                float unsafeHP = Mathf.Lerp(currentHP.value, targetHP, deltaHPTimeFraction);
                currentHP.value = Mathf.Clamp(unsafeHP, 0f, baseStats.GetStat(Stat.HP));

                if (stateAltered != null)
                {
                     stateAltered.Invoke(this, new StateAlteredData(StateAlteredType.AdjustHPNonSpecific));
                }
            }
        }

        private void UpdateCooldown()
        {
            if (cooldownTimer > 0) { cooldownTimer -= Time.deltaTime; }
            if (inCooldown && cooldownTimer <= 0)
            {
                inCooldown = false;
                if (stateAltered != null)
                {
                    stateAltered.Invoke(this, new StateAlteredData(StateAlteredType.CooldownExpired));
                }
            }
        }

        private void AwardExperience()
        {
            // TODO:  Implement experience awards (requires first:  party concept)
        }

        // Save State
        [System.Serializable]
        struct CombatParticipantSaveData
        {
            public bool isDead;
            public float currentHP;
            public float currentAP;
        }

        public object CaptureState()
        {
            CombatParticipantSaveData combatParticipantSaveData = new CombatParticipantSaveData
            {
                isDead = isDead.value,
                currentHP = currentHP.value,
                currentAP = currentAP.value
            };
            return combatParticipantSaveData;
        }

        public void RestoreState(object state)
        {
            CombatParticipantSaveData data = (CombatParticipantSaveData)state;
            isDead.value = data.isDead;
            currentHP.value = data.currentHP;
            currentAP.value = data.currentAP;
        }
    }
}

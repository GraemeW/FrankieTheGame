using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frankie.Utils;
using Frankie.Stats;
using System.Text.RegularExpressions;
using System;

namespace Frankie.Combat
{
    [RequireComponent(typeof(BaseStats))]
    [RequireComponent(typeof(SkillHandler))]
    public class CombatParticipant : MonoBehaviour
    {
        // Tunables
        [SerializeField] bool friendly = false;
        [SerializeField] float damageTimeSpan = 4.0f;
        [SerializeField] Sprite combatSprite = null;

        // Cached References
        BaseStats baseStats = null;

        // State
        bool inCombat = false;
        bool isDead = false;
        bool inCooldown = false;
        float cooldownTimer = 0f;
        float targetHP = 1f;
        float deltaHPTimeFraction = 0.0f;
        LazyValue<float> currentHP;
        LazyValue<float> currentAP;
        List<ActiveStatusEffect> currentStatusEffects = new List<ActiveStatusEffect>();

        // Events
        public event Action<bool> enterCombat;
        public event Action<CombatParticipant, StateAlteredType, float> stateAltered;

        // Data Structures
        public enum StateAlteredType
        {
            DecreaseHP,
            IncreaseHP,
            AdjustHPNonSpecific,
            IncreaseAP,
            DecreaseAP,
            Dead,
            Resurrected,
            StatusEffectApplied,
            CooldownSet,
            CooldownExpired
        }


        private void Awake()
        {
            baseStats = GetComponent<BaseStats>();
            currentHP = new LazyValue<float>(GetMaxHP);
            currentAP = new LazyValue<float>(GetMaxAP);
        }

        private void Start()
        {
            currentHP.ForceInit();
            currentAP.ForceInit();
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
            // TODO:  Add logic for status effects (figure out how to handle various/many effects without this blowing up)
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
            if (isDead) { return; }

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
                if (points < 0) { stateAltered.Invoke(this, StateAlteredType.DecreaseHP, points); }
                else if (points > 0) { stateAltered.Invoke(this, StateAlteredType.IncreaseHP, points); }
            }
        }

        public void HaltHPScroll()
        {
            deltaHPTimeFraction = 0f;
            targetHP = currentHP.value;
        }

        public void AdjustAP(float points)
        {
            if (isDead) { return; }

            float unsafeAP = currentAP.value + points;
            currentAP.value = Mathf.Clamp(unsafeAP, 0f, baseStats.GetStat(Stat.AP));

            if (stateAltered != null)
            {
                if (points < 0) { stateAltered.Invoke(this, StateAlteredType.DecreaseAP, points); }
                else if (points > 0) { stateAltered.Invoke(this, StateAlteredType.IncreaseAP, points); }
            }
        }

        public void ApplyStatusEffect(ActiveStatusEffect newStatusEffect)
        {
            foreach (ActiveStatusEffect activeStatusEffect in currentStatusEffects)
            {
                if (activeStatusEffect.statusEffect == newStatusEffect.statusEffect)
                {
                    if (activeStatusEffect.value < newStatusEffect.value) { activeStatusEffect.value = newStatusEffect.value; } // Always take the higher value
                    if (activeStatusEffect.timer < newStatusEffect.timer) { activeStatusEffect.timer = newStatusEffect.timer; } // Refresh to the higher timeout
                    return;
                }
            }
            currentStatusEffects.Add(newStatusEffect);

            if (stateAltered != null)
            {
                // TODO:  elaborate status effect applied -- this isn't enough information to handle the animation or whatever
                // Alternatively status effect as linked list to grab last
                stateAltered.Invoke(this, StateAlteredType.StatusEffectApplied, 0f);
            }
        }

        public void ResurrectCharacter(float hp)
        {
            // TODO:  Proper implementation of revives -- need to think on what portion immediate vs. rolling
            isDead = false;
            currentHP.value = hp;
            targetHP = hp;
            cooldownTimer = 0f;
            if (stateAltered != null)
            {
                stateAltered.Invoke(this, StateAlteredType.Resurrected, 0f);
            }
        }

        public void SetCooldown(float seconds)
        {
            inCooldown = true;
            cooldownTimer = seconds;
            if (stateAltered != null)
            {
                stateAltered.Invoke(this, StateAlteredType.CooldownSet, 0f);
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
            return Regex.Replace(baseStats.GetCharacterName().ToString("G"), "([a-z])_?([A-Z])", "$1 $2");
        }

        public bool IsDead()
        {
            return isDead;
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

        // Private functions
        private bool CheckIfDead()
        {
            if (Mathf.Approximately(currentHP.value, 0f) || currentHP.value < 0)
            {
                currentHP.value = 0f;
                targetHP = 0f;
                isDead = true;
                if (!friendly)
                {
                    AwardExperience();
                }
                if (stateAltered != null)
                {
                    stateAltered.Invoke(this, StateAlteredType.Dead, 0f);
                }
            }
            if (isDead) { return true; }
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
                     stateAltered.Invoke(this, StateAlteredType.AdjustHPNonSpecific, 0f);
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
                    stateAltered.Invoke(this, StateAlteredType.CooldownExpired, 0f);
                }
            }
        }

        private void AwardExperience()
        {
            // TODO:  Implement experience awards (requires first:  party concept)
        }
    }
}

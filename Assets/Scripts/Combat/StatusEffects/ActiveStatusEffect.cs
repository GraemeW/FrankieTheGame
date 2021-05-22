using Frankie.Stats;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Frankie.Combat
{
    [RequireComponent(typeof(CombatParticipant))]
    public class ActiveStatusEffect : MonoBehaviour
    {
        // State
        StatusEffect statusEffect = null;
        bool active = false;
        float timer = 0f;
        float tickTimer = 0f;
        int tickCount = 1;
        bool queuedTick = false;

        // Cached References
        BattleController battleController = null;
        CombatParticipant combatParticipant = null;

        // ActiveStatusEffect -- Behavior Chart:
        // Frozen:  Slows combat participant 
        //    -- Applied instantly, multiplicative effect on cooldown, expires after time & reverts
        // Electrified:  Speeds combat participant, causes tick damage
        //    -- Speed applied instantly, multuplicative effect on cooldown (primary), tick damage (secondary), expires after time & reverts speed
        // Bleeding:  Causes tick damage that increases over time
        //    -- Tick increases by primary value for each iteration (1*primary, 2*primary, 3*primary, ...)
        // Burning:  Causes tick damage
        //    -- Tick at primary damage each hit


        private void Update()
        {
            UpdateTimers();
            HandleRecurringEffects();
        }

        private void OnEnable()
        {
            if (battleController != null)
            {
                battleController.battleStateChanged += HandleBattleState;
            }
        }

        private void OnDisable()
        {
            if (battleController != null)
            {
                battleController.battleStateChanged -= HandleBattleState;
            }
        }

        private void OnDestroy()
        {
            if (statusEffect.statusEffectType == StatusEffectType.Frozen)
            {
                combatParticipant.SetCooldownMultiplier(1 / statusEffect.primaryValue);
            }
            else if (statusEffect.statusEffectType == StatusEffectType.Electrified)
            {
                combatParticipant.SetCooldownMultiplier(1 / statusEffect.primaryValue);
            }
        }

        public void Setup(StatusEffect statusEffect, CombatParticipant combatParticipant)
        {
            this.statusEffect = statusEffect;
            this.combatParticipant = combatParticipant;
            active = true;
            battleController = GameObject.FindGameObjectWithTag("BattleController").GetComponent<BattleController>();
            battleController.battleStateChanged += HandleBattleState;

            HandleInstantEffects();
        }

        public StatusEffect GetStatusEffect()
        {
            return statusEffect;
        }

        public string GetEffectName()
        {
            // Split apart name on lower case followed by upper case w/ or w/out underscores
            return Regex.Replace(statusEffect.name, "([a-z])_?([A-Z])", "$1 $2");
        }

        private void UpdateTimers()
        {
            if (!active) { return; }

            timer += Time.deltaTime;
            tickTimer += Time.deltaTime;

            if (tickTimer > (statusEffect.duration / (statusEffect.numberOfTicks + 1))) // +1 -- to actually execute number of ticks;  otherwise will destroy self on timer
            {
                queuedTick = true;
                tickTimer = 0f;
            }

            if (timer > statusEffect.duration)
            {
                Destroy(this);
            }
        }

        private void HandleInstantEffects()
        {
            if (statusEffect.statusEffectType == StatusEffectType.Frozen)
            {
                combatParticipant.SetCooldownMultiplier(statusEffect.primaryValue);
            }
            else if (statusEffect.statusEffectType == StatusEffectType.Electrified)
            {
                combatParticipant.SetCooldownMultiplier(statusEffect.primaryValue);
            }
        }

        private void HandleRecurringEffects()
        {
            if (!queuedTick || !active) { return; }

            if (statusEffect.statusEffectType == StatusEffectType.Bleeding)
            {
                combatParticipant.AdjustHP(tickCount * statusEffect.primaryValue);
            }
            else if (statusEffect.statusEffectType == StatusEffectType.Burning)
            {
                combatParticipant.AdjustHP(statusEffect.primaryValue);
            }
            else if (statusEffect.statusEffectType == StatusEffectType.Electrified)
            {
                combatParticipant.AdjustHP(statusEffect.secondaryValue);
            }

            tickCount++;
            queuedTick = false;
        }

        private void HandleBattleState(BattleState battleState)
        {
            if (battleState != BattleState.Combat)
            {
                active = false;
            }
            else
            {
                active = true;
            }

            if (battleState == BattleState.Complete)
            {
                Destroy(this); // Default behavior -- remove buffs/debuffs after combat
            }
        }
    }   
}

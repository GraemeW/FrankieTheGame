using Frankie.Control;
using Frankie.Stats;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Frankie.Combat
{
    // TODO:  Remove the guts, rely on the effect strategy to control
    [RequireComponent(typeof(CombatParticipant))]
    public class ActiveStatusEffect : MonoBehaviour
    {
        // State
        EffectStrategy statusEffect = null;
        bool active = false;
        bool persistAfterBattle = false;
        float timer = 0f;
        float tickTimer = 0f;
        int tickCount = 1;
        bool queuedTick = false;

        // Cached References
        PlayerStateHandler playerStateHandler = null;
        BattleController battleController = null;
        CombatParticipant combatParticipant = null;


        private void Update()
        {
            UpdateTimers();
            HandleRecurringEffects();
        }

        private void OnEnable()
        {
            if (playerStateHandler != null) { playerStateHandler.playerStateChanged += HandlePlayerState; }
            if (battleController != null) { battleController.battleStateChanged += HandleBattleState; }
        }

        private void OnDisable()
        {
            if (playerStateHandler != null) { playerStateHandler.playerStateChanged -= HandlePlayerState; }
            if (battleController != null) { battleController.battleStateChanged -= HandleBattleState; }
        }

        private void OnDestroy()
        {

        }

        public void Setup(EffectStrategy statusEffect, CombatParticipant combatParticipant, bool persistAfterBattle = false)
        {
            this.statusEffect = statusEffect;
            this.combatParticipant = combatParticipant;
            this.persistAfterBattle = persistAfterBattle;
            SetupPlayerStateHandler();
            SetupBattleController();

            HandleInstantEffects();
        }

        private void SetupPlayerStateHandler()
        {
            playerStateHandler = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerStateHandler>();
            playerStateHandler.playerStateChanged += HandlePlayerState;
        }

        private void SetupBattleController()
        {
            if (battleController != null) { return; }

            battleController = GameObject.FindGameObjectWithTag("BattleController").GetComponent<BattleController>();
            if (battleController != null)
            {
                if (battleController.GetBattleState() != BattleState.Combat)
                {
                    active = false;
                }
                else
                {
                    active = true;
                }
                battleController.battleStateChanged += HandleBattleState;
            }
            else
            {
                if (persistAfterBattle) { active = true; }
                else { Destroy(this); } // Should not be called
            }
        }

        public string GetEffectName()
        {
            // Split apart name on lower case followed by upper case w/ or w/out underscores
            return Regex.Replace(statusEffect.name, "([a-z])_?([A-Z])", "$1 $2");
        }

        private void UpdateTimers()
        {
            /*
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
            }*/
        }

        private void HandleInstantEffects()
        {
            /*
            if (statusEffect.statusEffectType == StatusEffectType.Frozen)
            {
                combatParticipant.SetCooldownMultiplier(statusEffect.primaryValue);
            }
            else if (statusEffect.statusEffectType == StatusEffectType.Electrified)
            {
                combatParticipant.SetCooldownMultiplier(statusEffect.primaryValue);
            }
            */
        }

        private void HandleRecurringEffects()
        {
            /*
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
            */
        }

        private void HandleBattleState(BattleState battleState)
        {
            if (battleState != BattleState.Combat)
            {
                active = false;
            }

            if (battleState == BattleState.Complete)
            {
                battleController.battleStateChanged -= HandleBattleState;
                battleController = null;

                if (persistAfterBattle)
                {
                    active = true;
                }
                else
                {
                    Destroy(this); // Default behavior -- remove buffs/debuffs after combat
                }
            }
        }

        private void HandlePlayerState(PlayerState playerState)
        {
            if (playerState == PlayerState.inBattle)
            {
                SetupBattleController();
            }
        }
    }   
}

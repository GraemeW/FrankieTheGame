using Frankie.Control;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Combat
{
    [RequireComponent(typeof(CombatParticipant))]
    public class PersistentStatus : MonoBehaviour
    {
        // Tunables
        protected StatusType statusEffectType = default;
        protected float duration = Mathf.Infinity;
        protected bool persistAfterBattle = false;

        // State
        protected bool active = false;

        // Cached References
        protected PlayerStateHandler playerStateHandler = null;
        protected BattleController battleController = null;

        protected virtual void Update()
        {
            UpdateTimers();
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

        protected void Setup(StatusType statusEffectType, float duration, bool persistAfterBattle = false)
        {
            this.statusEffectType = statusEffectType;
            this.duration = duration;
            this.persistAfterBattle = persistAfterBattle;

            SyncToPlayerStateHandler();
            SyncToBattleController();
        }

        protected void SyncToPlayerStateHandler()
        {
            playerStateHandler = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerStateHandler>();
            playerStateHandler.playerStateChanged += HandlePlayerState;
        }

        protected void SyncToBattleController()
        {
            if (battleController != null) { return; }

            battleController = GameObject.FindGameObjectWithTag("BattleController").GetComponent<BattleController>();
            if (battleController != null)
            {
                HandleBattleState(battleController.GetBattleState());
                battleController.battleStateChanged += HandleBattleState;
            }
            else
            {
                if (persistAfterBattle) { active = true; }
                else { Destroy(this); }
            }
        }

        protected virtual void UpdateTimers()
        {
            if (!active) { return; }

            duration -= Time.deltaTime;
            if (duration <= 0)
            {
                Destroy(this);
            }
        }

        private void HandleBattleState(BattleState battleState)
        {
            if (battleState == BattleState.Combat)
            {
                active = true;
            }
            else
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
                SyncToBattleController();
            }
        }
    }
}

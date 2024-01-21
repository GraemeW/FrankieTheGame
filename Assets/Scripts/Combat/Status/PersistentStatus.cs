using Frankie.Control;
using Frankie.Stats;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Combat
{
    [RequireComponent(typeof(CombatParticipant))]
    public abstract class PersistentStatus : MonoBehaviour
    {
        // State
        protected bool active = false;
        protected float duration = Mathf.Infinity;
        protected bool persistAfterBattle = false;

        protected Stat statusEffectType = default; // Default, override in override methods
        protected bool isIncrease = false; // Default, override in override methods

        // Cached References
        CombatParticipant combatParticipant = null;
        protected PlayerStateMachine playerStateHandler = null;
        protected BattleController battleController = null;

        // Events
        public event Action persistentStatusTimedOut;

        #region UnityMethods
        private void Awake()
        {
            combatParticipant = GetComponent<CombatParticipant>();
        }

        protected virtual void Update()
        {
            UpdateTimers();
        }

        private void OnEnable()
        {
            combatParticipant.stateAltered += HandleCombatState;

            if (playerStateHandler != null) { playerStateHandler.playerStateChanged += HandlePlayerState; }
            if (battleController != null) { battleController.battleStateChanged += HandleBattleState; }
        }

        private void OnDisable()
        {
            combatParticipant.stateAltered -= HandleCombatState;

            if (playerStateHandler != null) { playerStateHandler.playerStateChanged -= HandlePlayerState; }
            if (battleController != null) { battleController.battleStateChanged -= HandleBattleState; }
        }

        private void OnDestroy()
        {
            persistentStatusTimedOut?.Invoke();
        }
        #endregion

        #region PublicMethods
        public Stat GetStatusEffectType() => statusEffectType;
        public bool IsIncrease() => isIncrease;
        #endregion

        #region PrivateProtectedMethods
        protected void Setup(float duration, bool persistAfterBattle = false)
        {
            this.duration = duration;
            this.persistAfterBattle = persistAfterBattle;

            SyncToPlayerStateHandler();
            SyncToBattleController();
        }

        protected void SyncToPlayerStateHandler()
        {
            playerStateHandler = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerStateMachine>();
            playerStateHandler.playerStateChanged += HandlePlayerState;
        }

        protected void SyncToBattleController()
        {
            if (battleController != null) { return; }

            battleController = GameObject.FindGameObjectWithTag("BattleController")?.GetComponent<BattleController>();
            if (battleController != null)
            {
                HandleBattleState(battleController.GetBattleState(), BattleOutcome.Undetermined);
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

        private void HandleBattleState(BattleState battleState, BattleOutcome battleOutcome)
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

        private void HandlePlayerState(PlayerStateType playerState)
        {
            if (playerState == PlayerStateType.inBattle)
            {
                SyncToBattleController();
            }
        }

        private void HandleCombatState(CombatParticipant combatParticipant, StateAlteredData stateAlteredData)
        {
            if (stateAlteredData.stateAlteredType == StateAlteredType.Dead)
            {
                Destroy(this);
            }
        }
        #endregion
    }
}

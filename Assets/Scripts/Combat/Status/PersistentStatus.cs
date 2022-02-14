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
        protected StatusType statusType = default;
        protected float duration = Mathf.Infinity;
        protected bool persistAfterBattle = false;

        // State
        protected bool active = false;

        // Cached References
        CombatParticipant combatParticipant = null;
        protected PlayerStateHandler playerStateHandler = null;
        protected BattleController battleController = null;

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

        public StatusType GetStatusType()
        {
            return statusType;
        }

        protected void Setup(StatusType statusType, float duration, bool persistAfterBattle = false)
        {
            this.statusType = statusType;
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

            battleController = GameObject.FindGameObjectWithTag("BattleController")?.GetComponent<BattleController>();
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
    }
}

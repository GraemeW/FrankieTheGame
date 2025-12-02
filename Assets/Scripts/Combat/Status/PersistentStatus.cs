using System;
using UnityEngine;
using Frankie.Control;
using Frankie.Core;
using Frankie.Stats;

namespace Frankie.Combat
{
    [RequireComponent(typeof(CombatParticipant))]
    public abstract class PersistentStatus : MonoBehaviour
    {
        // State
        private string effectGUID;
        protected bool active;
        private float duration = Mathf.Infinity;
        private bool persistAfterBattle;

        protected Stat statAffected = default; // Default, override in override methods
        protected bool isIncrease = false; // Default, override in override methods

        // Cached References
        private CombatParticipant combatParticipant;
        private PlayerStateMachine playerStateMachine;

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
            combatParticipant.SubscribeToStateUpdates(HandleCombatState);

            if (playerStateMachine != null) { playerStateMachine.playerStateChanged += HandlePlayerState; }
            if (BattleEventBus.inBattle) { BattleEventBus<BattleStateChangedEvent>.SubscribeToEvent(HandleBattleState); }
        }

        private void OnDisable()
        {
            combatParticipant.UnsubscribeToStateUpdates(HandleCombatState);

            if (playerStateMachine != null) { playerStateMachine.playerStateChanged -= HandlePlayerState; }
            BattleEventBus<BattleStateChangedEvent>.UnsubscribeFromEvent(HandleBattleState);
        }
        #endregion

        #region PublicMethods
        public string GetEffectGUID() => effectGUID;
        public Stat GetStatusEffectType() => statAffected;
        public bool IsIncrease() => isIncrease;
        #endregion

        #region PrivateProtectedMethods
        protected void Setup(string setEffectGUID, float setDuration, bool setPersistAfterBattle = false)
        {
            effectGUID = setEffectGUID;
            duration = setDuration;
            persistAfterBattle = setPersistAfterBattle;

            SyncToPlayerStateHandler();
            SyncToBattle();
        }
        
        protected float GetDuration() => duration;

        protected void ResetDuration(float amount)
        {
            duration = Mathf.Max(duration, amount);
        }

        private void SyncToPlayerStateHandler()
        {
            playerStateMachine = Player.FindPlayerStateMachine();
            playerStateMachine.playerStateChanged += HandlePlayerState;
        }

        private void SyncToBattle()
        {
            if (!BattleEventBus.inBattle)
            {
                if (persistAfterBattle) { active = true; }
                else { Destroy(this); }
                return;
            }

            HandleBattleState(BattleEventBus.battleState); // Sync state immediately
            BattleEventBus<BattleStateChangedEvent>.SubscribeToEvent(HandleBattleState);
        }

        protected virtual void UpdateTimers()
        {
            if (!active) { return; }

            duration -= Time.deltaTime;
            if (duration <= 0)
            {
                persistentStatusTimedOut?.Invoke();
                Destroy(this);
            }
        }

        private void HandleBattleState(BattleStateChangedEvent battleStateChangedEvent)
        {
            BattleState battleState = battleStateChangedEvent.battleState;
            HandleBattleState(battleState);
        }

        private void HandleBattleState(BattleState battleState)
        {
            active = battleState == BattleState.Combat;

            if (battleState == BattleState.Complete)
            {
                BattleEventBus<BattleStateChangedEvent>.UnsubscribeFromEvent(HandleBattleState);
                if (persistAfterBattle) { active = true; }
                else { Destroy(this); } // Default behavior -- remove buffs/debuffs after combat
            }
        }

        private void HandlePlayerState(PlayerStateType playerState)
        {
            if (playerState == PlayerStateType.inBattle)
            {
                SyncToBattle();
            }
        }

        private void HandleCombatState(StateAlteredInfo stateAlteredInfo)
        {
            if (stateAlteredInfo.stateAlteredType == StateAlteredType.Dead)
            {
                Destroy(this);
            }
        }
        #endregion
    }
}

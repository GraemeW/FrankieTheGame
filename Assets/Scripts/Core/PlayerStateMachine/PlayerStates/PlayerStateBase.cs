namespace Frankie.Core.PlayerStates
{
    public abstract class PlayerStateBase : IPlayerState
    {
        public abstract PlayerStateType playerStateType { get; }
        public virtual void EnterCombat(IPlayerStateContext playerStateContext) => playerStateContext.QueueActionUnderConsideration();
        public virtual void EnterDialogue(IPlayerStateContext playerStateContext) => playerStateContext.QueueActionUnderConsideration();
        public virtual void EnterCutScene(IPlayerStateContext playerStateContext) => playerStateContext.QueueActionUnderConsideration();
        public virtual void EnterOptions(IPlayerStateContext playerStateContext) { } // Ignore
        public virtual void EnterTrade(IPlayerStateContext playerStateContext) { } // Ignore

        public virtual void EnterTransition(IPlayerStateContext playerStateContext)
        {
            // Force state to transition, going to get pulled to a new scene
            if (playerStateContext.InZoneTransition()) { playerStateContext.SetPlayerState(PlayerStateMachine.transitionState); }
        }

        public virtual void EnterWorld(IPlayerStateContext playerStateContext)
        {
            playerStateContext.ClearPlayerStateMemory();
            playerStateContext.SetPlayerState(PlayerStateMachine.worldState);
        }
    }
}

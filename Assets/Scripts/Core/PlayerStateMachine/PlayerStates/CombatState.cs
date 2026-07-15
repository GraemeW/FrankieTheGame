namespace Frankie.Core.PlayerStates
{
    public class CombatState : PlayerStateBase
    {
        public override PlayerStateType playerStateType => PlayerStateType.InBattle;

        // Ignore - go to immunity post-combat, so cannot queue
        public override void EnterCombat(IPlayerStateContext playerStateContext) { }

        public override void EnterTransition(IPlayerStateContext playerStateContext)
        {
            if (playerStateContext.InBattleExitTransition())
            {
                playerStateContext.SetPlayerState(PlayerStateMachine.transitionState);
                if (!playerStateContext.EndBattleSequence())  // State change from Transition to World handled by coroutine
                {
                    EnterWorld(playerStateContext); // Protection to default back to world on fail to exit battle
                }
            }
            else if (playerStateContext.InZoneTransition())
            {
                playerStateContext.SetPlayerState(PlayerStateMachine.transitionState); // Force state to transition, going to get pulled to a new scene
            }
        }
    }
}

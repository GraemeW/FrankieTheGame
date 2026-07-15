namespace Frankie.Core.PlayerStates
{
    public class CutSceneState : PlayerStateBase
    {
        public override PlayerStateType playerStateType => PlayerStateType.InCutScene;
        
        public override void EnterCutScene(IPlayerStateContext playerStateContext)
        {
            // Queue then kick to world to bump next cutscene immediately
            playerStateContext.QueueActionUnderConsideration();
            EnterWorld(playerStateContext);
        }

        public override void EnterTransition(IPlayerStateContext playerStateContext)
        {
            if (!playerStateContext.InZoneTransition()) { return; }
            
            // Force state to transition, going to get pulled to a new scene
            playerStateContext.TogglePlayerVisibility(true);
            playerStateContext.SetPlayerState(PlayerStateMachine.transitionState); 
        }

        public override void EnterWorld(IPlayerStateContext playerStateContext)
        {
            playerStateContext.TogglePlayerVisibility(true);
            playerStateContext.ClearPlayerStateMemory();
            playerStateContext.SetPlayerState(PlayerStateMachine.worldState);
        }
    }
}

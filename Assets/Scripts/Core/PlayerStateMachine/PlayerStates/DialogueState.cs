namespace Frankie.Core.PlayerStates
{
    public class DialogueState : PlayerStateBase
    {
        public override PlayerStateType playerStateType => PlayerStateType.InDialogue;
        public override void EnterTrade(IPlayerStateContext playerStateContext) => playerStateContext.QueueActionUnderConsideration();
    }
}

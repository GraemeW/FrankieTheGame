namespace Frankie.Combat
{
    public class BattleStateChangedEvent : IBattleEvent
    {
        public BattleEventType battleEventType => BattleEventType.BattleStateChanged;

        public BattleState battleState { get; private set; }
        public BattleOutcome battleOutcome { get; private set; }

        public BattleStateChangedEvent(BattleState battleState, BattleOutcome battleOutcome)
        {
            this.battleState = battleState;
            this.battleOutcome = battleOutcome;
        }
    }
}

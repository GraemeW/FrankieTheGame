namespace Frankie.Combat
{
    public class BattleActionSelectedEvent : IBattleEvent
    {
        public BattleEventType battleEventType => BattleEventType.BattleActionSelected;

        public readonly IBattleActionSuper battleActionSuper;

        public BattleActionSelectedEvent(IBattleActionSuper battleActionSuper)
        {
            this.battleActionSuper = battleActionSuper;
        }
    }
}

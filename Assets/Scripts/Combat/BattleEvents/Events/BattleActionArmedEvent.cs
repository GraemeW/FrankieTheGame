namespace Frankie.Combat
{
    public struct BattleActionArmedEvent : IBattleEvent
    {
        public BattleEventType battleEventType => BattleEventType.BattleActionArmed;

        public readonly IBattleActionSuper battleActionSuper;
        public readonly bool isArmed;

        public BattleActionArmedEvent(IBattleActionSuper battleActionSuper, bool isArmed)
        {
            this.battleActionSuper = battleActionSuper;
            this.isArmed = isArmed;
        }
    }
}

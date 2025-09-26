namespace Frankie.Combat
{
    public struct BattleExitEvent : IBattleEvent
    {
        public BattleEventType battleEventType => BattleEventType.BattleExit;
    }
}

namespace Frankie.Combat
{
    public class BattleExitEvent : IBattleEvent
    {
        public BattleEventType battleEventType => BattleEventType.BattleExit;

        public BattleExitEvent()
        {
            
        }
    }
}

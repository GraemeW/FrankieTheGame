using System.Collections.Generic;

namespace Frankie.Combat
{
    public class BattleQueueAddAttemptEvent : IBattleEvent
    {
        public BattleEventType battleEventType => BattleEventType.BattleQueueAddAttemptEvent;
        public IList<BattleEntity> targets { get; private set; }
        
        public BattleQueueAddAttemptEvent(IList<BattleEntity> targets)
        {
            this.targets = targets;
        }
    }
}

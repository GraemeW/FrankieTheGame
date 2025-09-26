using System.Collections.Generic;

namespace Frankie.Combat
{
    public class BattleEnterEvent : IBattleEvent
    {
        public BattleEventType battleEventType => BattleEventType.BattleEnter;

        public List<BattleEntity> playerEntities { get; private set; }
        public List<BattleEntity> enemyEntities { get; private set; }

        public BattleEnterEvent(List<BattleEntity> playerEntities, List<BattleEntity> enemyEntities)
        {
            this.playerEntities = playerEntities;
            this.enemyEntities = enemyEntities;
        }
    }
}

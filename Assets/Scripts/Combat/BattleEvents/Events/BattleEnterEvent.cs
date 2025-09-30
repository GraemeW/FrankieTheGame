using System.Collections.Generic;
using Frankie.ZoneManagement;

namespace Frankie.Combat
{
    public class BattleEnterEvent : IBattleEvent
    {
        public BattleEventType battleEventType => BattleEventType.BattleEnter;

        public List<BattleEntity> playerEntities { get; private set; }
        public List<BattleEntity> enemyEntities { get; private set; }
        public TransitionType transitionType;

        public BattleEnterEvent(List<BattleEntity> playerEntities, List<BattleEntity> enemyEntities, TransitionType transitionType)
        {
            this.playerEntities = playerEntities;
            this.enemyEntities = enemyEntities;
            this.transitionType = transitionType;
        }
    }
}

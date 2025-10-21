using System.Collections.Generic;
using Frankie.ZoneManagement;

namespace Frankie.Combat
{
    public class BattleEnterEvent : IBattleEvent
    {
        public BattleEventType battleEventType => BattleEventType.BattleEnter;

        public IList<BattleEntity> playerEntities { get; private set; }
        public IList<BattleEntity> enemyEntities { get; private set; }
        public TransitionType transitionType;

        public BattleEnterEvent(IList<BattleEntity> playerEntities, IList<BattleEntity> enemyEntities, TransitionType transitionType)
        {
            this.playerEntities = playerEntities;
            this.enemyEntities = enemyEntities;
            this.transitionType = transitionType;
        }
    }
}

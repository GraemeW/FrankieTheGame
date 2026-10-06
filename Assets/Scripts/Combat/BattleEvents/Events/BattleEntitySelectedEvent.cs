using System.Collections.Generic;

namespace Frankie.Combat
{
    public class BattleEntitySelectedEvent : IBattleEvent
    {
        public BattleEventType battleEventType => BattleEventType.BattleEntitySelected;

        public readonly BattleEntitySelectionType selectionType;
        public readonly List<BattleEntity> battleEntities;

        public BattleEntitySelectedEvent(BattleEntitySelectionType selectionType, IList<BattleEntity> battleEntities)
        {
            this.selectionType = selectionType;
            this.battleEntities = new List<BattleEntity>(battleEntities);
        }
    }
}

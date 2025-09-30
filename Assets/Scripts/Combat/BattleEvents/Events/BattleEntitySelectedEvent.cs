using System.Collections.Generic;

namespace Frankie.Combat
{
    public class BattleEntitySelectedEvent : IBattleEvent
    {
        public BattleEventType battleEventType => BattleEventType.BattleEntitySelected;

        public CombatParticipantType combatParticipantType;
        public List<BattleEntity> battleEntities;

        public BattleEntitySelectedEvent(CombatParticipantType combatParticipantType, List<BattleEntity> battleEntities)
        {
            this.combatParticipantType = combatParticipantType;
            this.battleEntities = battleEntities;
        }
    }
}

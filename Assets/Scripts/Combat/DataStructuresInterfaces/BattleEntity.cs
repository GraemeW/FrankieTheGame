using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Combat
{
    public class BattleEntity
    {
        public CombatParticipant combatParticipant;
        public BattleEntityType battleEntityType;
        public int row;
        public int column;
        public bool isCharacter = false;
        public bool isAssistCharacter = false;

        // Character type instantiation
        public BattleEntity(CombatParticipant combatParticipant, bool isAssistCharacter = false)
        {
            this.combatParticipant = combatParticipant;
            this.isCharacter = true;
            this.isAssistCharacter = isAssistCharacter;
            this.battleEntityType = BattleEntityType.Standard;
            this.row = 0;
            this.column = 0;
        }

        // Enemy type instantiation
        public BattleEntity(CombatParticipant combatParticipant, BattleEntityType battleEntityType, int row, int column)
        {
            this.combatParticipant = combatParticipant;
            this.battleEntityType = battleEntityType;
            this.isCharacter = false;
            this.isAssistCharacter = false;
            this.row = row;
            this.column = column;
        }
    }
}

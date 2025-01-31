using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Combat
{
    public class BattleEntity
    {
        // Attributes
        public CombatParticipant combatParticipant;
        public BattleEntityType battleEntityType;
        public int row;
        public int column;
        public bool isCharacter = false;
        public bool isAssistCharacter = false;

        // Events
        public event Action<BattleEntity, int, int> removedFromCombat;

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

            combatParticipant.stateAltered += HandleStateChange;
        }

        // Specific event handling
        private void HandleStateChange(CombatParticipant combatParticipant, StateAlteredData state)
        {
            if (state.stateAlteredType != StateAlteredType.Dead) { return; }

            combatParticipant.stateAltered -= HandleStateChange;
            removedFromCombat?.Invoke(this, row, column);
        }
    }
}

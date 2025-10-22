namespace Frankie.Combat
{
    public class BattleEntity
    {
        // Attributes
        public readonly CombatParticipant combatParticipant;
        public readonly BattleEntityType battleEntityType;
        public BattleRow row;
        public int column;
        public readonly bool isCharacter;
        public readonly bool isAssistCharacter;

        // Character type instantiation
        public BattleEntity(CombatParticipant combatParticipant, bool isAssistCharacter = false)
        {
            this.combatParticipant = combatParticipant;
            this.isAssistCharacter = isAssistCharacter;
            isCharacter = true;
            battleEntityType = BattleEntityType.Standard;
            row = BattleRow.Middle;
            column = 0;
        }

        // Enemy type instantiation
        public BattleEntity(CombatParticipant combatParticipant, BattleEntityType battleEntityType, BattleRow row, int column)
        {
            this.combatParticipant = combatParticipant;
            this.battleEntityType = battleEntityType;
            this.row = row;
            this.column = column;
            isCharacter = false;
            isAssistCharacter = false;

            combatParticipant.SubscribeToStateUpdates(HandleStateChange);
        }

        // Specific event handling
        private void HandleStateChange(StateAlteredInfo stateAlteredInfo)
        {
            if (stateAlteredInfo.stateAlteredType != StateAlteredType.Dead) { return; }

            combatParticipant.UnsubscribeToStateUpdates(HandleStateChange);
            BattleEventBus<BattleEntityRemovedFromBoardEvent>.Raise(new BattleEntityRemovedFromBoardEvent(this, row, column));
        }
    }
}

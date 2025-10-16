namespace Frankie.Combat
{
    public class BattleEntity
    {
        // Attributes
        public CombatParticipant combatParticipant;
        public BattleEntityType battleEntityType;
        public BattleRow row;
        public int column;
        public readonly bool isCharacter;
        public readonly bool isAssistCharacter;

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
        public BattleEntity(CombatParticipant combatParticipant, BattleEntityType battleEntityType, BattleRow row, int column)
        {
            this.combatParticipant = combatParticipant;
            this.battleEntityType = battleEntityType;
            this.isCharacter = false;
            this.isAssistCharacter = false;
            this.row = row;
            this.column = column;

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

using Frankie.Stats;

namespace Frankie.Combat
{
    public class StateAlteredEvent : IBattleEvent
    {
        public BattleEventType battleEventType => BattleEventType.BattleEntityStateAltered;

        public CombatParticipant combatParticipant;
        public StateAlteredType stateAlteredType;
        public float points;
        public PersistentStatus persistentStatus;
        public Stat stat;

        public StateAlteredEvent(CombatParticipant combatParticipant, StateAlteredType stateAlteredType)
        {
            this.combatParticipant = combatParticipant;
            this.stateAlteredType = stateAlteredType;
        }

        public StateAlteredEvent(CombatParticipant combatParticipant, StateAlteredType stateAlteredType, float points)
        {
            this.combatParticipant = combatParticipant;
            this.stateAlteredType = stateAlteredType;
            this.points = points;
        }

        public StateAlteredEvent(CombatParticipant combatParticipant, StateAlteredType stateAlteredType, PersistentStatus persistentStatus)
        {
            this.combatParticipant = combatParticipant;
            this.stateAlteredType = stateAlteredType;
            this.persistentStatus = persistentStatus;
        }

        public StateAlteredEvent(CombatParticipant combatParticipant, StateAlteredType stateAlteredType, Stat stat, float points)
        {
            this.combatParticipant = combatParticipant;
            this.stateAlteredType = stateAlteredType;
            this.stat = stat;
        }
    }
}

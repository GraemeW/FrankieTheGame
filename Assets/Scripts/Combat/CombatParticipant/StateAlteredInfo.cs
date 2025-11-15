using Frankie.Stats;

namespace Frankie.Combat
{
    public class StateAlteredInfo : IBattleEvent
    {
        public BattleEventType battleEventType => BattleEventType.BattleEntityStateAltered;

        public readonly CombatParticipant combatParticipant;
        public readonly StateAlteredType stateAlteredType;
        public readonly float points;
        public readonly PersistentStatus persistentStatus;
        public readonly Stat stat;

        public StateAlteredInfo(CombatParticipant combatParticipant, StateAlteredType stateAlteredType)
        {
            this.combatParticipant = combatParticipant;
            this.stateAlteredType = stateAlteredType;
        }

        public StateAlteredInfo(CombatParticipant combatParticipant, StateAlteredType stateAlteredType, float points)
        {
            this.combatParticipant = combatParticipant;
            this.stateAlteredType = stateAlteredType;
            this.points = points;
        }

        public StateAlteredInfo(CombatParticipant combatParticipant, StateAlteredType stateAlteredType, PersistentStatus persistentStatus)
        {
            this.combatParticipant = combatParticipant;
            this.stateAlteredType = stateAlteredType;
            this.persistentStatus = persistentStatus;
        }

        public StateAlteredInfo(CombatParticipant combatParticipant, StateAlteredType stateAlteredType, Stat stat, float points)
        {
            this.combatParticipant = combatParticipant;
            this.stateAlteredType = stateAlteredType;
            this.stat = stat;
            this.points = points;
        }
    }
}

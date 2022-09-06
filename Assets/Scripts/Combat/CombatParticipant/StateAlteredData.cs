using Frankie.Stats;

namespace Frankie.Combat
{
    public class StateAlteredData
    {
        public StateAlteredType stateAlteredType;
        public float points;
        public PersistentStatus persistentStatus;
        public Stat stat;

        public StateAlteredData(StateAlteredType stateAlteredType)
        {
            this.stateAlteredType = stateAlteredType;
        }

        public StateAlteredData(StateAlteredType stateAlteredType, float points)
        {
            this.stateAlteredType = stateAlteredType;
            this.points = points;
        }

        public StateAlteredData(StateAlteredType stateAlteredType, PersistentStatus persistentStatus)
        {
            this.stateAlteredType = stateAlteredType;
            this.persistentStatus = persistentStatus;
        }

        public StateAlteredData(StateAlteredType stateAlteredType, Stat stat)
        {
            this.stateAlteredType = stateAlteredType;
            this.stat = stat;
        }
    }
}
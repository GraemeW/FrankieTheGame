namespace Frankie.Combat
{
    public class StateAlteredData
    {
        public StateAlteredType stateAlteredType;
        public float points;
        public StatusEffectType statusEffectType;

        public StateAlteredData(StateAlteredType stateAlteredType)
        {
            this.stateAlteredType = stateAlteredType;
        }

        public StateAlteredData(StateAlteredType stateAlteredType, float points)
        {
            this.stateAlteredType = stateAlteredType;
            this.points = points;
        }

        public StateAlteredData(StateAlteredType stateAlteredType, StatusEffectType statusEffectType)
        {
            this.stateAlteredType = stateAlteredType;
            this.statusEffectType = statusEffectType;
        }
    }
}
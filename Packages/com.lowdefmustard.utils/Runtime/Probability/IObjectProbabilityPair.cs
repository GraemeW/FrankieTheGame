namespace LowDefMustard.Utils
{
    public interface IObjectProbabilityPair<out T>
    {
        // Notes:
        //  - Weight is relative, not a true probability (e.g. weights 3 and 1 roll 75 / 25)
        //  - Entries with a null value or a weight at or below 0 are never picked
        
        public T value { get; }
        public int weight { get; }
    }
}

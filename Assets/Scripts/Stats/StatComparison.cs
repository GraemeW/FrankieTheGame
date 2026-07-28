namespace Frankie.Stats
{
    public struct StatComparison
    {
        public Stat stat { get; private set; }
        public float oldValue { get; private set; }
        public float newValue { get; private set; }

        public StatComparison(Stat stat, float oldValue, float newValue)
        {
            this.stat = stat;
            this.oldValue = oldValue;
            this.newValue = newValue;
        }
    }
}

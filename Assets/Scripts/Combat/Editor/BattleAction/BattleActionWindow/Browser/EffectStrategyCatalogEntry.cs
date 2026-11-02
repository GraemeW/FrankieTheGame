namespace Frankie.Combat.Editor
{
    public readonly struct EffectStrategyCatalogEntry
    {
        public readonly EffectStrategy strategy;
        public readonly EffectStrategyCategory category;
        public readonly int subOrder;
        public readonly float sortValue;
        public readonly string summary;

        public EffectStrategyCatalogEntry(EffectStrategy strategy, EffectStrategyCategory category, int subOrder, float sortValue, string summary)
        {
            this.strategy = strategy;
            this.category = category;
            this.subOrder = subOrder;
            this.sortValue = sortValue;
            this.summary = summary;
        }

        public bool isHeader => strategy == null; // A category header has no strategy (its summary is the header text)

        public static EffectStrategyCatalogEntry Header(EffectStrategyCategory category, string text) => new(null, category, 0, 0f, text);
    }
}

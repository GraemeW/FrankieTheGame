namespace Frankie.Stats.UI
{
    public readonly struct StatLine
    {
        public readonly string name;
        public readonly string value;

        public StatLine(string name, string value)
        {
            this.name = name;
            this.value = value;
        }
    }
}

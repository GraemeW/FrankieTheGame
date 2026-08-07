namespace Frankie.Saving
{
    public readonly struct PrefsKeyInfo
    {
        public readonly string key;
        public readonly PrefsValueType type;

        public PrefsKeyInfo(string key, PrefsValueType type)
        {
            this.key = key;
            this.type = type;
        }
    }
}

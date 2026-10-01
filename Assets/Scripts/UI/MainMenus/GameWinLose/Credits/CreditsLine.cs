namespace Frankie.Menu.UI
{
    // Display-ready credits entry (i.e. role resolved to its localized title)
    public readonly struct CreditsLine
    {
        public readonly string title;
        public readonly string name;

        public CreditsLine(string title, string name)
        {
            this.title = title;
            this.name = name;
        }
    }
}

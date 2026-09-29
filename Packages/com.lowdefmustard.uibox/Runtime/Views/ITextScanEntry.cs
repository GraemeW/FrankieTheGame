namespace LowDefMustard.UIBox
{
    public interface ITextScanEntry
    {
        bool isAlive { get; }
        bool canDisplayText { get; }
        void Reveal();
        void SetText(string text);
        void Remove();
    }
}
using UnityEngine;

namespace LowDefMustard.UIBox
{
    // Legacy uGUI entry:  wraps an instantiated prefab (SimpleTextLink for text, UIChoice for choices)
    internal class UGUITextScanEntry : ITextScanEntry
    {
        // Note:  Internal for test visibility
        internal readonly GameObject gameObject;
        private readonly SimpleTextLink simpleTextLink;

        public UGUITextScanEntry(GameObject gameObject)
        {
            this.gameObject = gameObject;
            if (gameObject != null) { gameObject.TryGetComponent(out simpleTextLink); }
        }

        public bool isAlive => gameObject != null;
        public bool canDisplayText => simpleTextLink != null;

        public void Reveal()
        {
            if (gameObject != null) { gameObject.SetActive(true); }
        }

        public void SetText(string text)
        {
            if (simpleTextLink != null) { simpleTextLink.Setup(text); }
        }

        public void Remove()
        {
            if (gameObject != null) { Object.Destroy(gameObject); }
        }
    }

    internal sealed class UGUITextScanChoiceEntry : UGUITextScanEntry, ITextScanChoiceEntry
    {
        private readonly UIChoice uiChoice;

        public UGUITextScanChoiceEntry(GameObject gameObject, UIChoice uiChoice) : base(gameObject)
        {
            this.uiChoice = uiChoice;
        }

        public void Highlight(bool enable)
        {
            if (uiChoice != null) { uiChoice.Highlight(enable); }
        }
    }
}

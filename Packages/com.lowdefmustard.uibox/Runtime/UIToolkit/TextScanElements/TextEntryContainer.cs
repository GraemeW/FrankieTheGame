using UnityEngine.UIElements;

namespace LowDefMustard.UIBox
{
    [UxmlElement]
    public sealed partial class TextEntryContainer : VisualElement
    {
        // Const Tunables
        private const string _ussClassName = "text-entries";

        public TextEntryContainer()
        {
            AddToClassList(_ussClassName);
        }
    }
}

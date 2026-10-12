using UnityEngine.UIElements;

namespace LowDefMustard.UIBox
{
    [UxmlElement]
    public sealed partial class TextEntryContainer : VisualElement
    {

        public TextEntryContainer()
        {
            AddToClassList(USSClassNames.TextEntries.block);
        }
    }
}

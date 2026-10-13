using UnityEngine.UIElements;
using Frankie.Utils.UI;

namespace Frankie.Menu.UI
{
    public static class ConfirmCardParts
    {
        // Const Tunables
        private const string _boxUssClassName = "uibox";
        private const string _backingUssClassName = "uibox__backing";
        private const string _frameUssClassName = "uibox__frame";
        private const string _ussClassName = "confirm-card";

        public static VisualElement CreateCard(string modifierUssClassName)
        {
            var card = new VisualElement { pickingMode = PickingMode.Ignore };
            card.AddToClassList(_boxUssClassName);
            card.AddToClassList(_ussClassName);
            card.AddToClassList(modifierUssClassName);

            var backing = new VisualElement { pickingMode = PickingMode.Ignore };
            backing.AddToClassList(_backingUssClassName);
            card.Add(backing);

            var frame = new FrameElement();
            frame.AddToClassList(_frameUssClassName);
            card.Add(frame);
            return card;
        }

        public static Label AddLabel(VisualElement card, string text, string ussClassName)
        {
            var label = new Label(text) { pickingMode = PickingMode.Ignore };
            label.AddToClassList(ussClassName);
            card.Add(label);
            return label;
        }
    }
}

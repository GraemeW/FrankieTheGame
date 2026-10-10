using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;
using LowDefMustard.UIBox;

namespace Frankie.Menu.UI
{
    [UxmlElement]
    public sealed partial class FrameFlavourChoiceElement : Button
    {
        // Note: UxmlAttributes exist for UI Builder previews only - bound model values override them at runtime

        // Const Tunables
        private const string _ussClassName = "frame-flavour-choice";

        // State
        private bool internalHighlighted = false;
        private Color internalFlavourColour = Color.white;

        // Cached References
        private readonly Label label;

        // Bound Properties
        [CreateProperty, UxmlAttribute] public bool highlighted
        {
            get => internalHighlighted;
            set
            {
                internalHighlighted = value;
                ChoiceRowParts.SetHighlighted(this, value);
                RefreshLabelColour();
            }
        }

        [CreateProperty, UxmlAttribute] public Color flavourColour
        {
            get => internalFlavourColour;
            set
            {
                internalFlavourColour = value;
                RefreshLabelColour();
            }
        }

        [UxmlAttribute] public string labelText
        {
            get => label.text;
            set => label.text = value;
        }

        // Constructor
        public FrameFlavourChoiceElement()
        {
            focusable = false; // Keyboard/gamepad input handled via IInputReceiver - avoid UITK navigation double-firing
            ChoiceRowParts.Initialize(this);
            AddToClassList(_ussClassName);
            label = ChoiceRowParts.AddLabel(this, nameof(FrameFlavourChoiceModel.text));

            SetBinding(nameof(highlighted), UIToolkitBindings.ToTarget(nameof(FrameFlavourChoiceModel.isHighlighted)));
            SetBinding(nameof(flavourColour), UIToolkitBindings.ToTarget(nameof(FrameFlavourChoiceModel.colour)));
        }

        #region PrivateMethods
        private void RefreshLabelColour()
        {
            label.style.color = internalHighlighted ? internalFlavourColour : new StyleColor(StyleKeyword.Null);
        }
        #endregion
    }
}

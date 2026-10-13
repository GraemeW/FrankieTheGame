using Unity.Properties;
using UnityEngine.UIElements;
using LowDefMustard.UIBox;

namespace Frankie.Inventory.UI
{
    [UxmlElement]
    public sealed partial class InventorySlotElement : Button
    {
        // Note: UxmlAttributes exist for UI Builder previews only - bound model values override them at runtime

        // Const Tunables
        private const string _ussClassName = "inventory-slot";
        private const string _equippedUssClassName = _ussClassName + "--equipped";
        private const string _dimmedUssClassName = _ussClassName + "--dimmed";
        private const string _equippedMarkerUssClassName = _ussClassName + "__equipped-marker";
        private const string _equippedMarker = "e";

        // State
        private bool internalHighlighted = false;
        private bool internalEquipped = false;
        private bool internalDimmed = false;
        private bool internalShown = true;

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
            }
        }

        [CreateProperty, UxmlAttribute] public bool equipped
        {
            get => internalEquipped;
            set
            {
                internalEquipped = value;
                EnableInClassList(_equippedUssClassName, value);
            }
        }

        [CreateProperty, UxmlAttribute] public bool dimmed
        {
            get => internalDimmed;
            set
            {
                internalDimmed = value;
                EnableInClassList(_dimmedUssClassName, value);
            }
        }

        [CreateProperty] public bool shown
        {
            get => internalShown;
            set
            {
                internalShown = value;
                EnableInClassList(USSClassNames.ChoiceEntry.hidden, !value);
            }
        }

        [UxmlAttribute] public string labelText
        {
            get => label.text;
            set => label.text = value;
        }

        // Constructor
        public InventorySlotElement()
        {
            focusable = false; // Keyboard/gamepad input handled via IInputReceiver - avoid UITK navigation double-firing
            ChoiceRowParts.Initialize(this);
            AddToClassList(_ussClassName);

            var equippedMarkerLabel = new Label(_equippedMarker);
            equippedMarkerLabel.AddToClassList(_equippedMarkerUssClassName);
            Add(equippedMarkerLabel);
            label = ChoiceRowParts.AddLabel(this, nameof(InventorySlotModel.text));

            SetBinding(nameof(highlighted), UIToolkitBindings.ToTarget(nameof(InventorySlotModel.isHighlighted)));
            SetBinding(nameof(equipped), UIToolkitBindings.ToTarget(nameof(InventorySlotModel.isEquipped)));
            SetBinding(nameof(dimmed), UIToolkitBindings.ToTarget(nameof(InventorySlotModel.isDimmed)));
            SetBinding(nameof(shown), UIToolkitBindings.ToTarget(nameof(InventorySlotModel.isShown)));
        }
    }
}

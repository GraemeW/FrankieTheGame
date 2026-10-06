using Unity.Properties;
using UnityEngine.UIElements;
using LowDefMustard.UIBox;

namespace Frankie.Inventory.UI
{
    [UxmlElement]
    public sealed partial class ShopStockElement : Button
    {
        // Note: UxmlAttributes exist for UI Builder previews only - bound model values override them at runtime

        // Const Tunables
        private const string _ussClassName = "shop-stock";
        private const string _nameUssClassName = _ussClassName + "__name";
        private const string _priceUssClassName = _ussClassName + "__price";

        // State
        private bool internalHighlighted = false;

        // Cached References
        private readonly Label nameLabel;
        private readonly Label priceLabel;

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

        [UxmlAttribute] public string nameText
        {
            get => nameLabel.text;
            set => nameLabel.text = value;
        }

        [UxmlAttribute] public string priceText
        {
            get => priceLabel.text;
            set => priceLabel.text = value;
        }

        // Constructor
        public ShopStockElement()
        {
            focusable = false; // Keyboard/gamepad input handled via IInputReceiver - avoid UITK navigation double-firing
            ChoiceRowParts.Initialize(this);
            AddToClassList(_ussClassName);

            nameLabel = ChoiceRowParts.AddLabel(this, nameof(ShopStockModel.itemName));
            nameLabel.AddToClassList(_nameUssClassName);
            priceLabel = ChoiceRowParts.AddLabel(this, nameof(ShopStockModel.priceText));
            priceLabel.AddToClassList(_priceUssClassName);

            SetBinding(nameof(highlighted), UIToolkitBindings.ToTarget(nameof(ShopStockModel.isHighlighted)));
        }
    }
}

using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;
using LowDefMustard.UIBox;

namespace Frankie.Utils.UI
{
    [UxmlElement]
    public sealed partial class SpriteElement : VisualElement
    {
        // Note:
        //  - Size + fit are defined in USS (.sprite)
        //  - Animation handled via AnimatorSpriteSource (loads sprite model w/ relevant animation frame)
        
        // Const Tunables
        private const string _ussClassName = "sprite";

        // State
        private Sprite internalSprite;

        [CreateProperty] public Sprite sprite
        {
            get => internalSprite;
            set
            {
                internalSprite = value;
                style.backgroundImage = value != null ? new StyleBackground(value) : new StyleBackground(StyleKeyword.None);
            }
        }

        public SpriteElement()
        {
            AddToClassList(_ussClassName);
            pickingMode = PickingMode.Ignore;
            SetBinding(nameof(sprite), UIToolkitBindings.ToTarget(nameof(SpriteModel.sprite)));
        }
    }
}

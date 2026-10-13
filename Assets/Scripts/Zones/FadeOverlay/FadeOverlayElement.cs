using Unity.Properties;
using UnityEngine.UIElements;
using LowDefMustard.UIBox;

namespace Frankie.Zones.UI
{
    public abstract class FadeOverlayElement : VisualElement
    {
        // Note:
        //  - Look is defined in USS (.fade-overlay) - this element only follows its bound fade opacity
        //  - Blocks pointer input while fading, then it's removed from layout (and the pointer's way) once fully faded out

        // Const
        private const string _ussClassName = "fade-overlay";

        // State
        private float internalFadeOpacity;

        [CreateProperty] public float fadeOpacity
        {
            get => internalFadeOpacity;
            set
            {
                internalFadeOpacity = value;
                ApplyFadeOpacity();
            }
        }

        protected FadeOverlayElement(string sourcePropertyName)
        {
            AddToClassList(_ussClassName);
            ApplyFadeOpacity();
            SetBinding(nameof(fadeOpacity), UIToolkitBindings.ToTarget(sourcePropertyName));
        }

        private void ApplyFadeOpacity()
        {
            style.opacity = internalFadeOpacity;
            style.display = internalFadeOpacity > 0f ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}

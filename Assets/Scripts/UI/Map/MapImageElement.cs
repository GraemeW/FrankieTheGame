using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;
using LowDefMustard.UIBox;

namespace Frankie.Zones.UI
{
    [UxmlElement]
    public sealed partial class MapImageElement : VisualElement
    {
        // Note:  Size + fit are defined in USS - this element only shows the model's map snapshot

        // State
        private RenderTexture internalMapTexture;

        [CreateProperty] public RenderTexture mapTexture
        {
            get => internalMapTexture;
            set
            {
                internalMapTexture = value;
                style.backgroundImage = value != null ? new StyleBackground(Background.FromRenderTexture(value)) : new StyleBackground(StyleKeyword.None);
            }
        }

        public MapImageElement()
        {
            pickingMode = PickingMode.Ignore;
            SetBinding(nameof(mapTexture), UIToolkitBindings.ToTarget(nameof(MapModel.mapTexture)));
        }
    }
}

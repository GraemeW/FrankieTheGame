using Unity.Properties;
using UnityEngine;
using LowDefMustard.UIBox;

namespace Frankie.Utils.UI
{
    public sealed class SpriteModel : BindableModel
    {
        // State
        private Sprite internalSprite;

        [CreateProperty] public Sprite sprite
        {
            get => internalSprite;
            set => SetProperty(ref internalSprite, value);
        }
    }
}

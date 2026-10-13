using Unity.Properties;
using UnityEngine;
using LowDefMustard.UIBox;

namespace Frankie.Zones.UI
{
    public sealed class MapModel : BindableModel
    {
        // State
        private string internalFlavourTopText = "";
        private string internalFlavourBottomText = "";
        private RenderTexture internalMapTexture;

        [CreateProperty] public string flavourTopText
        {
            get => internalFlavourTopText;
            set => SetProperty(ref internalFlavourTopText, value);
        }

        [CreateProperty] public string flavourBottomText
        {
            get => internalFlavourBottomText;
            set => SetProperty(ref internalFlavourBottomText, value);
        }

        [CreateProperty] public RenderTexture mapTexture
        {
            get => internalMapTexture;
            set => SetProperty(ref internalMapTexture, value);
        }
    }
}

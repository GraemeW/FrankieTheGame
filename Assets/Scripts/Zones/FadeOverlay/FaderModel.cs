using Unity.Properties;
using LowDefMustard.UIBox;

namespace Frankie.Zones.UI
{
    public sealed class FaderModel : BindableModel
    {
        // State
        private float internalZoneFadeOpacity;
        private float internalBattleFadeOpacity;

        [CreateProperty] public float zoneFadeOpacity
        {
            get => internalZoneFadeOpacity;
            set => SetProperty(ref internalZoneFadeOpacity, value);
        }

        [CreateProperty] public float battleFadeOpacity
        {
            get => internalBattleFadeOpacity;
            set => SetProperty(ref internalBattleFadeOpacity, value);
        }
    }
}

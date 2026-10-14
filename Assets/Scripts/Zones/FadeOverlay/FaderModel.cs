using Unity.Properties;
using LowDefMustard.UIBox;

namespace Frankie.Zones.UI
{
    public sealed class FaderModel : BindableModel
    {
        // State
        private float internalZoneFadeOpacity;
        private float internalBattleFadeOpacity;
        private bool internalIsBattleEntryActive;

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

        [CreateProperty] public bool isBattleEntryActive
        {
            get => internalIsBattleEntryActive;
            set => SetProperty(ref internalIsBattleEntryActive, value);
        }
    }
}

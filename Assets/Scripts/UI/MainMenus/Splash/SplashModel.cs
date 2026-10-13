using Unity.Properties;
using LowDefMustard.UIBox;

namespace Frankie.Menu.UI
{
    public sealed class SplashModel : BindableModel
    {
        // State
        private SplashStage internalShownStage = SplashStage.None;
        private string internalStudioName = "";
        private string internalGameTitle = "";
        private float internalStageFadeSeconds;

        [CreateProperty] public SplashStage shownStage
        {
            get => internalShownStage;
            set => SetProperty(ref internalShownStage, value);
        }

        [CreateProperty] public float stageFadeSeconds
        {
            get => internalStageFadeSeconds;
            set => SetProperty(ref internalStageFadeSeconds, value);
        }

        [CreateProperty] public string studioName
        {
            get => internalStudioName;
            set => SetProperty(ref internalStudioName, value);
        }

        [CreateProperty] public string gameTitle
        {
            get => internalGameTitle;
            set => SetProperty(ref internalGameTitle, value);
        }
    }
}

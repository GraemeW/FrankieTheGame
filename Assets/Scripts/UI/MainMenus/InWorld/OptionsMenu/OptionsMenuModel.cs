using Unity.Properties;
using LowDefMustard.UIBox;

namespace Frankie.Menu.UI
{
    public sealed class OptionsMenuModel : BindableModel
    {
        // State
        private string internalHeaderText = "";
        private string internalResolutionsHeaderText = "";
        private string internalLanguageHeaderText = "";

        [CreateProperty] public string headerText
        {
            get => internalHeaderText;
            set => SetProperty(ref internalHeaderText, value);
        }

        [CreateProperty] public string resolutionsHeaderText
        {
            get => internalResolutionsHeaderText;
            set => SetProperty(ref internalResolutionsHeaderText, value);
        }

        [CreateProperty] public string languageHeaderText
        {
            get => internalLanguageHeaderText;
            set => SetProperty(ref internalLanguageHeaderText, value);
        }
    }
}

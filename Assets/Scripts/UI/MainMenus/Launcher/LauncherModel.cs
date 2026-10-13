using Unity.Properties;
using LowDefMustard.UIBox;

namespace Frankie.Menu.UI
{
    public class LauncherModel : BindableModel
    {
        // State
        private string internalTitleText = "";
        private string internalNameText = "";

        [CreateProperty] public string titleText
        {
            get => internalTitleText;
            set => SetProperty(ref internalTitleText, value);
        }

        [CreateProperty] public string nameText
        {
            get => internalNameText;
            set => SetProperty(ref internalNameText, value);
        }
    }
}

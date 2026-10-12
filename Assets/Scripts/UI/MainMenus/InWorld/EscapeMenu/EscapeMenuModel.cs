using Unity.Properties;
using LowDefMustard.UIBox;

namespace Frankie.Menu.UI
{
    public sealed class EscapeMenuModel : BindableModel
    {
        // State
        private string internalHeaderText = "";

        [CreateProperty] public string headerText
        {
            get => internalHeaderText;
            set => SetProperty(ref internalHeaderText, value);
        }
    }
}

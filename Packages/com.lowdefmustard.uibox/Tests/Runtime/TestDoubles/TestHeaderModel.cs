using Unity.Properties;

namespace LowDefMustard.UIBox.Tests
{
    public sealed class TestHeaderModel : BindableModel
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

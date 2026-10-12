using Unity.Properties;
using LowDefMustard.UIBox;

namespace Frankie.Menu.UI
{
    public abstract class KeyboardCaseKeys : KeyboardKeySet
    {
        // Const Tunables
        private const string _hiddenUssClassName = ussClassName + "--hidden";

        // State
        private readonly bool isUpperCase;
        private bool internalIsUpperActive = false;

        [CreateProperty] public bool isUpperActive
        {
            get => internalIsUpperActive;
            set
            {
                internalIsUpperActive = value;
                RefreshShown();
            }
        }

        protected KeyboardCaseKeys(bool isUpperCase) : base(nameof(NamingPanelModel.letterColumns), NamingKeyboardLayout.defaultLetterColumns)
        {
            this.isUpperCase = isUpperCase;
            RefreshShown();
            SetBinding(nameof(isUpperActive), UIToolkitBindings.ToTarget(nameof(NamingPanelModel.isUpper)));
        }

        private void RefreshShown() => EnableInClassList(_hiddenUssClassName, internalIsUpperActive != isUpperCase);
    }
}

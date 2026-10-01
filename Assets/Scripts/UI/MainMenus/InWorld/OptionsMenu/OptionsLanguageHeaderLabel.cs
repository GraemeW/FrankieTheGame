using UnityEngine.UIElements;
using LowDefMustard.UIBox;

namespace Frankie.Menu.UI
{
    [UxmlElement]
    public sealed partial class OptionsLanguageHeaderLabel : ModelBoundLabel
    {
        public OptionsLanguageHeaderLabel() : base(nameof(OptionsMenuModel.languageHeaderText)) { }
    }
}

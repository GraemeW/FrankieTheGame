using UnityEngine.UIElements;
using LowDefMustard.UIBox;

namespace Frankie.Menu.UI
{
    [UxmlElement]
    public sealed partial class OptionsHeaderLabel : ModelBoundLabel
    {
        public OptionsHeaderLabel() : base(nameof(OptionsMenuModel.headerText)) { }
    }
}

using UnityEngine.UIElements;
using LowDefMustard.UIBox;

namespace Frankie.Menu.UI
{
    [UxmlElement]
    public sealed partial class OptionsResolutionsHeaderLabel : ModelBoundLabel
    {
        public OptionsResolutionsHeaderLabel() : base(nameof(OptionsMenuModel.resolutionsHeaderText)) { }
    }
}

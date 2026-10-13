using UnityEngine.UIElements;
using LowDefMustard.UIBox;

namespace Frankie.Menu.UI
{
    [UxmlElement]
    public sealed partial class LoadGameHeaderLabel : ModelBoundLabel
    {
        public LoadGameHeaderLabel() : base(nameof(LoadGameMenuModel.headerText)) { }
    }
}

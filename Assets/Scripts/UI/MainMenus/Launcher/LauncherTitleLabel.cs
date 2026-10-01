using UnityEngine.UIElements;
using LowDefMustard.UIBox;

namespace Frankie.Menu.UI
{
    [UxmlElement]
    public sealed partial class LauncherTitleLabel : ModelBoundLabel
    {
        public LauncherTitleLabel() : base(nameof(LauncherModel.titleText)) { }
    }
}

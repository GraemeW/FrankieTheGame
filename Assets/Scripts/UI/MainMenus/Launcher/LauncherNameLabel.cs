using UnityEngine.UIElements;
using LowDefMustard.UIBox;

namespace Frankie.Menu.UI
{
    [UxmlElement]
    public sealed partial class LauncherNameLabel : ModelBoundLabel
    {
        public LauncherNameLabel() : base(nameof(LauncherModel.nameText)) { }
    }
}

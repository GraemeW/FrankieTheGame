using UnityEngine.UIElements;
using LowDefMustard.UIBox;

namespace Frankie.Menu.UI
{
    [UxmlElement]
    public sealed partial class EscapeMenuHeaderLabel : ModelBoundLabel
    {
        public EscapeMenuHeaderLabel() : base(nameof(EscapeMenuModel.headerText)) { }
    }
}

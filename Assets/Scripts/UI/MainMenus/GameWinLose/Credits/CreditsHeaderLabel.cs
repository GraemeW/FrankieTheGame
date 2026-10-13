using UnityEngine.UIElements;
using LowDefMustard.UIBox;

namespace Frankie.Menu.UI
{
    [UxmlElement]
    public sealed partial class CreditsHeaderLabel : ModelBoundLabel
    {
        public CreditsHeaderLabel() : base(nameof(GameWinMenuModel.creditsHeaderText)) { }
    }
}

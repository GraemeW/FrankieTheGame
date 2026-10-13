using UnityEngine.UIElements;
using LowDefMustard.UIBox;

namespace Frankie.Menu.UI
{
    [UxmlElement]
    public sealed partial class SplashTitleLabel : ModelBoundLabel
    {
        public SplashTitleLabel() : base(nameof(SplashModel.gameTitle)) { }
    }
}

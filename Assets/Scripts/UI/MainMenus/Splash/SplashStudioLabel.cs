using UnityEngine.UIElements;
using LowDefMustard.UIBox;

namespace Frankie.Menu.UI
{
    [UxmlElement]
    public sealed partial class SplashStudioLabel : ModelBoundLabel
    {
        public SplashStudioLabel() : base(nameof(SplashModel.studioName)) { }
    }
}

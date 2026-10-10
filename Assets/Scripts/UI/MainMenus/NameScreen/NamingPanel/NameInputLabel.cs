using UnityEngine.UIElements;
using LowDefMustard.UIBox;

namespace Frankie.Menu.UI
{
    [UxmlElement]
    public sealed partial class NameInputLabel : ModelBoundLabel
    {
        public NameInputLabel() : base(nameof(NamingPanelModel.inputText)) { }
    }
}

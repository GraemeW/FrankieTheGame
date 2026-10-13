using UnityEngine.UIElements;
using LowDefMustard.UIBox;

namespace Frankie.Menu.UI
{
    [UxmlElement]
    public sealed partial class FrameFlavourQuestionLabel : ModelBoundLabel
    {
        public FrameFlavourQuestionLabel() : base(nameof(FrameFlavourPanelModel.questionText)) { }
    }
}

using UnityEngine.UIElements;

namespace LowDefMustard.UIBox.Tests
{
    [UxmlElement]
    public sealed partial class TestHeaderLabel : ModelBoundLabel
    {
        public TestHeaderLabel() : base(nameof(TestHeaderModel.headerText)) { }
    }
}

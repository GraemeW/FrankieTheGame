using LowDefMustard.Control;

namespace LowDefMustard.UIBox.Tests
{
    public class TestBaseController : BaseController
    {
        public int onNoReceiversIdentifiedCount;
        protected override void OnNoReceiversIdentified() => onNoReceiversIdentifiedCount++;
    }
}

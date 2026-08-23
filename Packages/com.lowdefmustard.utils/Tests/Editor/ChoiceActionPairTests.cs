using NUnit.Framework;

namespace LowDefMustard.Utils.Tests.Editor
{
    public class ChoiceActionPairTests
    {
        [Test]
        public void Constructor_StoresChoiceAndAction()
        {
            int callCount = 0;

            var pair = new ChoiceActionPair("Attack", Action);

            Assert.AreEqual("Attack", pair.choice);
            pair.action.Invoke();
            Assert.AreEqual(1, callCount);
            return;

            // Local Functions
            void Action() => callCount++;
        }
    }
}

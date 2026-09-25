using NUnit.Framework;

namespace LowDefMustard.Zones.Tests.Editor
{
    public class FaderEventTriggersTests
    {
        // State
        private enum TestTransitionType { None, Fade }

        //Tests
        [Test]
        public void Constructor_AssignsAllDelegates()
        {
            bool fadeInCalled = false;
            bool fadePeakCalled = false;
            bool fadeOutCalled = false;
            bool fadeCompleteCalled = false;

            var triggers = new FaderEventTriggers<TestTransitionType>(
                _ => fadeInCalled = true,
                () => fadePeakCalled = true,
                () => fadeOutCalled = true,
                () => fadeCompleteCalled = true);

            triggers.onFadeIn.Invoke(TestTransitionType.Fade);
            triggers.onFadePeak.Invoke();
            triggers.onFadeOut.Invoke();
            triggers.onFadeComplete.Invoke();

            Assert.IsTrue(fadeInCalled);
            Assert.IsTrue(fadePeakCalled);
            Assert.IsTrue(fadeOutCalled);
            Assert.IsTrue(fadeCompleteCalled);
        }

        [Test]
        public void DefaultInstance_AllDelegatesNull()
        {
            var triggers = default(FaderEventTriggers<TestTransitionType>);

            Assert.IsNull(triggers.onFadeIn);
            Assert.IsNull(triggers.onFadePeak);
            Assert.IsNull(triggers.onFadeOut);
            Assert.IsNull(triggers.onFadeComplete);
        }
    }
}

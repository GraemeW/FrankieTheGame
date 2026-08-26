using NUnit.Framework;
using LowDefMustard.Utils.Editor;

namespace LowDefMustard.Utils.Tests.Editor
{
    public class OverrideConfigurationIsOverrideMatchTests
    {
        [Test]
        public void IsOverrideMatch_StandStillRequested_StandStillSlot_ReturnsTrue()
        {
            bool result = OverrideConfiguration.IsOverrideMatch("CharacterStandStill", action: null, isIdle: false, isStandStill: true);

            Assert.IsTrue(result);
        }

        [Test]
        public void IsOverrideMatch_StandStillRequested_NonStandStillSlot_ReturnsFalse()
        {
            bool result = OverrideConfiguration.IsOverrideMatch("CharacterDown", action: null, isIdle: false, isStandStill: true);

            Assert.IsFalse(result);
        }

        [Test]
        public void IsOverrideMatch_StandStillSlot_NeverMatchesMovementRequest()
        {
            bool result = OverrideConfiguration.IsOverrideMatch("CharacterStandStill", action: "Down", isIdle: false, isStandStill: false);

            Assert.IsFalse(result);
        }

        [Test]
        public void IsOverrideMatch_StandStillSlot_NeverMatchesIdleRequest()
        {
            bool result = OverrideConfiguration.IsOverrideMatch("CharacterStandStill", action: "Down", isIdle: true, isStandStill: false);

            Assert.IsFalse(result);
        }

        [Test]
        public void IsOverrideMatch_IdleRequested_IdleSlotWithMatchingAction_ReturnsTrue()
        {
            bool result = OverrideConfiguration.IsOverrideMatch("CharacterIdleDown", action: "Down", isIdle: true, isStandStill: false);

            Assert.IsTrue(result);
        }

        [Test]
        public void IsOverrideMatch_IdleRequested_NonIdleSlot_ReturnsFalse()
        {
            bool result = OverrideConfiguration.IsOverrideMatch("CharacterDown", action: "Down", isIdle: true, isStandStill: false);

            Assert.IsFalse(result);
        }

        [Test]
        public void IsOverrideMatch_MovementRequested_IdleSlot_ReturnsFalse()
        {
            bool result = OverrideConfiguration.IsOverrideMatch("CharacterIdleDown", action: "Down", isIdle: false, isStandStill: false);

            Assert.IsFalse(result);
        }

        [Test]
        public void IsOverrideMatch_ActionDoesNotMatchSlotSuffix_ReturnsFalse()
        {
            bool result = OverrideConfiguration.IsOverrideMatch("CharacterDown", action: "Up", isIdle: false, isStandStill: false);

            Assert.IsFalse(result);
        }

        [TestCase(null)]
        [TestCase("")]
        public void IsOverrideMatch_NullOrEmptyAction_ReturnsFalse(string action)
        {
            bool result = OverrideConfiguration.IsOverrideMatch("CharacterDown", action, isIdle: false, isStandStill: false);

            Assert.IsFalse(result);
        }

        [Test]
        public void IsOverrideMatch_ActionMatchIsCaseInsensitive()
        {
            bool result = OverrideConfiguration.IsOverrideMatch("Characterdown", action: "Down", isIdle: false, isStandStill: false);

            Assert.IsTrue(result);
        }
    }
}

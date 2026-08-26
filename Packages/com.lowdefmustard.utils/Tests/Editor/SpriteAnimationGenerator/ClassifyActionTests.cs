using NUnit.Framework;
using LowDefMustard.Utils.Editor;

namespace LowDefMustard.Utils.Tests.Editor
{
    public class ClassifyActionTests
    {
        [TestCase("Down", "Down")]
        [TestCase("Front", "Down")]
        [TestCase("Up", "Up")]
        [TestCase("Back", "Up")]
        [TestCase("Left", "Left")]
        [TestCase("Right", "Right")]
        [TestCase("DownLeft", "DownLeft")]
        [TestCase("FrontLeft", "DownLeft")]
        [TestCase("BackRight", "UpRight")]
        public void ClassifyAction_RecognizedDirection_ReturnsCanonicalDirection(string raw, string expectedCanonical)
        {
            var result = SpriteAnimationGenerator.ClassifyAction(raw);

            Assert.AreEqual(expectedCanonical, result.resolvedAction);
            Assert.IsTrue(result.isRecognized);
            Assert.IsFalse(result.isIdleSource);
            Assert.IsFalse(result.isStandStillSource);
        }

        [Test]
        public void ClassifyAction_DirectionAliasIsCaseInsensitive()
        {
            var result = SpriteAnimationGenerator.ClassifyAction("front");

            Assert.AreEqual("Down", result.resolvedAction);
            Assert.IsTrue(result.isRecognized);
        }

        [TestCase("IdleDown", "Down")]
        [TestCase("Down Idle", "Down")]
        [TestCase("IdleBack", "Up")]
        public void ClassifyAction_IdleWithRecognizedDirectionRemainder_ReturnsIdleSourceWithCanonicalDirection(string raw, string expectedCanonical)
        {
            var result = SpriteAnimationGenerator.ClassifyAction(raw);

            Assert.AreEqual(expectedCanonical, result.resolvedAction);
            Assert.IsTrue(result.isIdleSource);
            Assert.IsFalse(result.isStandStillSource);
            Assert.IsTrue(result.isRecognized);
        }

        [Test]
        public void ClassifyAction_IdleWithUnrecognizedRemainder_FallsThroughToUnrecognized()
        {
            // "Idle" is found and stripped, but "Dance" isn't a direction alias
            // Falls through all the way to the final unrecognized-passthrough branch
            var result = SpriteAnimationGenerator.ClassifyAction("IdleDance");

            Assert.IsFalse(result.isIdleSource);
            Assert.IsFalse(result.isRecognized);
            Assert.AreEqual("IdleDance", result.resolvedAction);
        }

        [TestCase("Static")]
        [TestCase("STATIC")]
        [TestCase("CharacterStatic")]
        public void ClassifyAction_ContainsStandStillToken_ReturnsStandStillClassification(string raw)
        {
            var result = SpriteAnimationGenerator.ClassifyAction(raw);

            Assert.IsTrue(result.isStandStillSource);
            Assert.IsFalse(result.isIdleSource);
            Assert.IsTrue(result.isRecognized);
            Assert.AreEqual(raw, result.resolvedAction); // StandStill passes the raw string through unchanged
        }

        [Test]
        public void ClassifyAction_StandStillTakesPriorityOverIdle()
        {
            // Contains both tokens - StandStill check runs first in the source
            var result = SpriteAnimationGenerator.ClassifyAction("IdleStatic");

            Assert.IsTrue(result.isStandStillSource);
            Assert.IsFalse(result.isIdleSource);
        }

        [Test]
        public void ClassifyAction_CompletelyUnrecognizedAction_ReturnsPassthrough()
        {
            var result = SpriteAnimationGenerator.ClassifyAction("Dance");

            Assert.IsFalse(result.isRecognized);
            Assert.IsFalse(result.isIdleSource);
            Assert.IsFalse(result.isStandStillSource);
            Assert.AreEqual("Dance", result.resolvedAction);
        }
    }
}

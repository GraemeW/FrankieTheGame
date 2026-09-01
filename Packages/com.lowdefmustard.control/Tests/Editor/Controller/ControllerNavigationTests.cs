using NUnit.Framework;
using UnityEngine;

namespace LowDefMustard.Control.Tests.Editor
{
    // Covered: BaseController's static navigation helpers
    // Not Covered: receiver-stack lifecycle, VerifyUnique, polling/destroy queue - see ControllerReceiverStackTests triage note in README
    
    public class ControllerNavigationTests
    {
        [TestCase(0f, 1f, ControllerInputType.NavigateUp)]
        [TestCase(0f, -1f, ControllerInputType.NavigateDown)]
        [TestCase(-1f, 0f, ControllerInputType.NavigateLeft)]
        [TestCase(1f, 0f, ControllerInputType.NavigateRight)]
        public void ParseDirectionalInput_CardinalDirections_ResolvesExpectedType(float x, float y, ControllerInputType expected)
        {
            var vector = new Vector2(x, y);
            BaseController.ParseDirectionalInput(vector, ControllerInputType.DefaultNone, out ControllerInputType result);
            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void ParseDirectionalInput_ZeroVector_ResolvesDefaultNone()
        {
            BaseController.ParseDirectionalInput(Vector2.zero, ControllerInputType.NavigateUp, out ControllerInputType result);
            Assert.That(result, Is.EqualTo(ControllerInputType.DefaultNone));
        }

        [Test]
        public void ParseDirectionalInput_EqualMagnitudeDiagonal_ResolvesDefaultNone()
        {
            // Vertical and horizontal magnitudes tie, so vectorSelect == 0 falls to DefaultNone
            BaseController.ParseDirectionalInput(new Vector2(1f, 1f), ControllerInputType.NavigateUp, out ControllerInputType result);
            Assert.That(result, Is.EqualTo(ControllerInputType.DefaultNone));
        }

        [Test]
        public void ParseDirectionalInput_SameResolvedType_ReturnsFalse()
        {
            bool changed = BaseController.ParseDirectionalInput(Vector2.up, ControllerInputType.NavigateUp, out ControllerInputType result);
            Assert.That(changed, Is.False);
            Assert.That(result, Is.EqualTo(ControllerInputType.NavigateUp));
        }

        [Test]
        public void ParseDirectionalInput_DifferentResolvedType_ReturnsTrue()
        {
            bool changed = BaseController.ParseDirectionalInput(Vector2.up, ControllerInputType.NavigateDown, out ControllerInputType result);
            Assert.That(changed, Is.True);
            Assert.That(result, Is.EqualTo(ControllerInputType.NavigateUp));
        }

        [TestCase(ControllerInputType.NavigateUp)]
        [TestCase(ControllerInputType.NavigateDown)]
        [TestCase(ControllerInputType.NavigateLeft)]
        [TestCase(ControllerInputType.NavigateRight)]
        public void TryInputTypeToNavigationVector_NavigationType_ReturnsTrueAndVector(ControllerInputType inputType)
        {
            bool found = BaseController.TryInputTypeToNavigationVector(inputType, out Vector2 vector);
            Assert.That(found, Is.True);
            Assert.That(vector, Is.Not.EqualTo(Vector2.zero));
        }

        [TestCase(ControllerInputType.DefaultNone)]
        [TestCase(ControllerInputType.Execute)]
        [TestCase(ControllerInputType.Cancel)]
        [TestCase(ControllerInputType.Option)]
        [TestCase(ControllerInputType.Escape)]
        public void TryInputTypeToNavigationVector_NonNavigationType_ReturnsFalseAndZero(ControllerInputType inputType)
        {
            bool found = BaseController.TryInputTypeToNavigationVector(inputType, out Vector2 vector);
            Assert.That(found, Is.False);
            Assert.That(vector, Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void TryInputTypeToNavigationVector_RoundTripsWithNavigationVectorToInputType()
        {
            // Confirms the two conversions are inverses of each other for all four cardinal directions
            foreach (ControllerInputType direction in new[]
                     {
                         ControllerInputType.NavigateUp, ControllerInputType.NavigateDown,
                         ControllerInputType.NavigateLeft, ControllerInputType.NavigateRight
                     })
            {
                BaseController.TryInputTypeToNavigationVector(direction, out Vector2 vector);
                BaseController.ParseDirectionalInput(vector, ControllerInputType.DefaultNone, out ControllerInputType roundTripped);
                Assert.That(roundTripped, Is.EqualTo(direction));
            }
        }
    }
}

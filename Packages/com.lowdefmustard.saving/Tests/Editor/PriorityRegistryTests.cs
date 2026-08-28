using LowDefMustard.Saving.Editor;
using NUnit.Framework;

namespace LowDefMustard.Saving.Tests.Editor
{
    public class PriorityRegistryTests
    {
        [Test]
        public void GetPriority_NoRulesRegistered_ReturnsMaxValue()
        {
            var registry = new PriorityRegistry<string>();
            Assert.AreEqual(int.MaxValue, registry.GetPriority("anything"));
        }

        [Test]
        public void GetPriority_SingleMatchingRule_ReturnsItsPriority()
        {
            var registry = new PriorityRegistry<string>();
            registry.Register(item => item == "player", 0);

            Assert.AreEqual(0, registry.GetPriority("player"));
        }

        [Test]
        public void GetPriority_NonMatchingItem_FallsThroughToMaxValue()
        {
            var registry = new PriorityRegistry<string>();
            registry.Register(item => item == "player", 0);

            Assert.AreEqual(int.MaxValue, registry.GetPriority("enemy"));
        }

        [Test]
        public void GetPriority_MultipleRules_FirstRegisteredMatchWins()
        {
            var registry = new PriorityRegistry<string>();
            registry.Register(item => item.StartsWith("p"), 5);
            registry.Register(item => item == "player", 0);
                // Second rule also matches "player" -- first-registered rule should win

            Assert.AreEqual(5, registry.GetPriority("player"));
        }
    }
}

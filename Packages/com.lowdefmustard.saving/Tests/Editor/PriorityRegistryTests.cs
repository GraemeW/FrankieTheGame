using NUnit.Framework;
using LowDefMustard.Saving.Editor;

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
                // Second rule also matches "player" - first-registered rule should win

            Assert.AreEqual(5, registry.GetPriority("player"));
        }

        [Test]
        public void Unregister_RemovesOnlyTheGivenRule()
        {
            var registry = new PriorityRegistry<string>();
            registry.Register(PlayerMatch, 0);
            registry.Register(EnemyMatch, 1);
            registry.Unregister(PlayerMatch);

            Assert.AreEqual(int.MaxValue, registry.GetPriority("player"));
            Assert.AreEqual(1, registry.GetPriority("enemy"));
            return;
            
            // Local Functions
            bool PlayerMatch(string item) => item == "player";
            bool EnemyMatch(string item) => item == "enemy";
        }

        [Test]
        public void Unregister_UnknownRule_IsANoOp()
        {
            var registry = new PriorityRegistry<string>();
            registry.Register(item => item == "player", 0);

            Assert.DoesNotThrow(() => registry.Unregister(item => item == "neverRegistered"));
            Assert.AreEqual(0, registry.GetPriority("player"));
        }
    }
}

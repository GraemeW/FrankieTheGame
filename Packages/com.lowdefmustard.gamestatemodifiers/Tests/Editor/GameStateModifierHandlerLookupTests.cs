using NUnit.Framework;

namespace LowDefMustard.GameStateModifiers.Tests.Editor
{
    public class GameStateModifierHandlerLookupTests
    {
        [Test]
        public void AddHandler_IndexesZoneHandlerAndLinks()
        {
            var index = new GameStateModifierHandlerLookup();

            index.AddHandler("Zone", "handler-1", new[] { "modifier-1", "modifier-2" });

            Assert.IsTrue(index.HasZone("Zone"));
            Assert.IsTrue(index.HasHandler("Zone", "handler-1"));
            Assert.IsTrue(index.IsLinked("Zone", "handler-1", "modifier-2"));
            Assert.IsFalse(index.IsLinked("Zone", "handler-1", "modifier-3"));
        }

        [Test]
        public void AddHandler_SameGuidInAnotherZone_IsKeptSeparate()
        {
            var index = new GameStateModifierHandlerLookup();

            index.AddHandler("ZoneA", "handler-1", new[] { "modifier-1" });
            index.AddHandler("ZoneB", "handler-1", new[] { "modifier-2" });

            Assert.IsFalse(index.IsLinked("ZoneA", "handler-1", "modifier-2"));
            Assert.IsTrue(index.IsLinked("ZoneB", "handler-1", "modifier-2"));
        }

        [Test]
        public void AddHandler_SameHandlerTwice_MergesLinks()
        {
            var index = new GameStateModifierHandlerLookup();

            index.AddHandler("Zone", "handler-1", new[] { "modifier-1" });
            index.AddHandler("Zone", "handler-1", new[] { "modifier-2" });

            Assert.AreEqual(1, index.handlerCount);
            Assert.IsTrue(index.IsLinked("Zone", "handler-1", "modifier-1"));
            Assert.IsTrue(index.IsLinked("Zone", "handler-1", "modifier-2"));
        }

        [Test]
        public void AddHandler_BlankGuidsAndNullLinks_AreIgnored()
        {
            var index = new GameStateModifierHandlerLookup();

            index.AddHandler("Zone", "", new[] { "modifier-1" });
            index.AddHandler("Zone", "handler-1", null);
            index.AddHandler("Zone", "handler-2", new[] { "", null, "modifier-1" });

            Assert.AreEqual(2, index.handlerCount);
            Assert.IsTrue(index.HasHandler("Zone", "handler-1"));
            Assert.IsTrue(index.IsLinked("Zone", "handler-2", "modifier-1"));
            Assert.IsFalse(index.IsLinked("Zone", "handler-2", ""));
        }

        [Test]
        public void AddZone_WithoutHandlers_IsIndexedButEmpty()
        {
            var index = new GameStateModifierHandlerLookup();

            index.AddZone("EmptyZone");

            Assert.IsTrue(index.HasZone("EmptyZone"));
            Assert.IsFalse(index.HasHandler("EmptyZone", "handler-1"));
            Assert.IsFalse(index.HasZone("OtherZone"));
        }
    }
}

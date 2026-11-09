using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Frankie.Core;

namespace Frankie.Tests.Editor
{
    public class PlayerStateRequestDispatcherTests
    {
        // Covers PlayerStateRequestDispatcher in isolation (plain C# / no player state machine required)
        //  - Immediate vs deferred runs, FIFO drain, one-level inline scope, exception isolation, drain cap

        // State
        private PlayerStateRequestDispatcher dispatcher;
        private List<string> calls;

        #region Setup
        [SetUp]
        public void SetUp()
        {
            dispatcher = new PlayerStateRequestDispatcher();
            calls = new List<string>();
        }
        #endregion

        #region Tests
        [Test]
        public void Run_AtDepthZero_RunsImmediately()
        {
            dispatcher.Run("Outer", () => calls.Add("Outer"));

            CollectionAssert.AreEqual(new[] { "Outer" }, calls);
            Assert.IsFalse(dispatcher.IsBusy());
        }

        [Test]
        public void Run_Nested_DefersInFifoOrderUntilOutermostReturns()
        {
            int deferredCountInsideOuter = -1;
            dispatcher.Run("Outer", () =>
            {
                calls.Add("Outer");
                dispatcher.Run("First", () => calls.Add("First"));
                dispatcher.Run("Second", () => calls.Add("Second"));
                calls.Add("OuterEnd");
                deferredCountInsideOuter = dispatcher.GetDeferredCount();
            });

            Assert.AreEqual(2, deferredCountInsideOuter);
            CollectionAssert.AreEqual(new[] { "Outer", "OuterEnd", "First", "Second" }, calls);
            Assert.AreEqual(0, dispatcher.GetDeferredCount());
        }

        [Test]
        public void Run_NestedInsideDrainedRequest_DrainsAfterIt()
        {
            dispatcher.Run("Outer", () =>
            {
                dispatcher.Run("First", () =>
                {
                    calls.Add("First");
                    dispatcher.Run("Third", () => calls.Add("Third"));
                });
                dispatcher.Run("Second", () => calls.Add("Second"));
            });

            CollectionAssert.AreEqual(new[] { "First", "Second", "Third" }, calls);
        }

        [Test]
        public void RunInline_CoversExactlyOneLevel()
        {
            dispatcher.Run("Outer", () =>
            {
                dispatcher.RunInline(() => dispatcher.Run("Inline", () =>
                {
                    calls.Add("Inline");
                    dispatcher.Run("Nested", () => calls.Add("Nested"));
                    calls.Add("InlineEnd");
                }));
                calls.Add("OuterEnd");
            });

            CollectionAssert.AreEqual(new[] { "Inline", "InlineEnd", "OuterEnd", "Nested" }, calls);
        }

        [Test]
        public void RunInline_AfterScope_NestedRequestsDeferAgain()
        {
            dispatcher.Run("Outer", () =>
            {
                dispatcher.RunInline(() => { });
                dispatcher.Run("After", () => calls.Add("After"));
                calls.Add("OuterEnd");
            });

            CollectionAssert.AreEqual(new[] { "OuterEnd", "After" }, calls);
        }

        [Test]
        public void Run_ThrowingRequest_RestoresDepthAndStillDrains()
        {
            Assert.Throws<InvalidOperationException>(() => dispatcher.Run("Outer", () =>
            {
                dispatcher.Run("Deferred", () => calls.Add("Deferred"));
                throw new InvalidOperationException("Outer failed");
            }));

            CollectionAssert.AreEqual(new[] { "Deferred" }, calls);
            Assert.IsFalse(dispatcher.IsBusy());

            dispatcher.Run("Next", () => calls.Add("Next"));
            CollectionAssert.AreEqual(new[] { "Deferred", "Next" }, calls);
        }

        [Test]
        public void Drain_ThrowingDeferredRequest_LogsExceptionAndRunsTheRest()
        {
            LogAssert.Expect(LogType.Exception, new Regex("Deferred failed"));

            dispatcher.Run("Outer", () =>
            {
                dispatcher.Run("Throwing", () => throw new InvalidOperationException("Deferred failed"));
                dispatcher.Run("After", () => calls.Add("After"));
            });

            CollectionAssert.AreEqual(new[] { "After" }, calls);
            Assert.IsFalse(dispatcher.IsBusy());
        }

        [Test]
        public void Drain_RequestLoop_WarnsAtCapAndClearsQueue()
        {
            int runCount = 0;
            void Loop() => dispatcher.Run("Loop", () => { runCount++; Loop(); });

            LogAssert.Expect(LogType.Warning, new Regex(@"Dropped 1 deferred player state requests due to possible request loop\.$"));
            Loop();

            Assert.AreEqual(17, runCount); // The immediate run + the 16-request drain cap
            Assert.AreEqual(0, dispatcher.GetDeferredCount());
            Assert.IsFalse(dispatcher.IsBusy());
        }
        #endregion
    }
}

using NUnit.Framework;

namespace LowDefMustard.Utils.Tests.Editor
{
    public class LazyValueTests
    {
        [Test]
        public void Value_BeforeFirstAccess_DoesNotCallInitializer()
        {
            int callCount = 0;
            var lazy = new LazyValue<int>(() =>
            {
                callCount++;
                return 42;
            });

            Assert.AreEqual(0, callCount);
        }

        [Test]
        public void Value_FirstAccess_CallsInitializerAndCachesResult()
        {
            int callCount = 0;
            var lazy = new LazyValue<int>(() =>
            {
                callCount++;
                return 42;
            });

            int result = lazy.value;

            Assert.AreEqual(42, result);
            Assert.AreEqual(1, callCount);
        }

        [Test]
        public void Value_RepeatedAccess_InitializerRunsOnlyOnce()
        {
            int callCount = 0;
            var lazy = new LazyValue<int>(() =>
            {
                callCount++;
                return callCount;
            });

            _ = lazy.value;
            _ = lazy.value;
            int result = lazy.value;

            Assert.AreEqual(1, callCount);
            Assert.AreEqual(1, result);
        }

        [Test]
        public void Setter_MarksInitialized_SoInitializerNeverRuns()
        {
            int callCount = 0;
            var lazy = new LazyValue<int>(() =>
            {
                callCount++;
                return 42;
            })
            {
                value = 7
            };

            int result = lazy.value;

            Assert.AreEqual(7, result);
            Assert.AreEqual(0, callCount);
        }

        [Test]
        public void ReInitLazyValue_WhenCachedValueIsStillNotNull_DoesNotReInitialize()
        {
            int callCount = 0;
            var lazy = new ReInitLazyValue<object>(() =>
            {
                callCount++;
                return new object();
            });

            _ = lazy.value;
            _ = lazy.value;

            Assert.AreEqual(1, callCount);
        }

        [Test]
        public void ReInitLazyValue_WhenCachedValueBecomesNull_ReInitializesOnNextAccess()
        {
            // Models the Unity "destroyed reference" scenario ReInitLazyValue is meant for:
            // The cached object becomes null behind the scenes, and access should trigger a fresh Initialize() call rather than returning the stale null
            int callCount = 0;
            var lazy = new ReInitLazyValue<object>(() =>
            {
                callCount++;
                return new object();
            });

            _ = lazy.value; // first init, callCount == 1
            lazy.value = null; // simulate the cached reference becoming null

            object result = lazy.value;

            Assert.IsNotNull(result);
            Assert.AreEqual(2, callCount);
        }
    }
}

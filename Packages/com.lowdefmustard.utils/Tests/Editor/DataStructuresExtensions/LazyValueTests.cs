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
        public void TryGetSafely_FirstAccess_InitializesAndReturnsTrue()
        {
            int callCount = 0;
            var lazy = new ReInitLazyValue<object>(() =>
            {
                callCount++;
                return new object();
            });

            bool result = lazy.TryGetSafely(out var passValue);

            Assert.IsTrue(result);
            Assert.IsNotNull(passValue);
            Assert.AreEqual(1, callCount);
        }

        [Test]
        public void TryGetSafely_InitializerReturnsNull_ReturnsFalse()
        {
            var lazy = new ReInitLazyValue<object>(() => null);

            bool result = lazy.TryGetSafely(out var passValue);

            Assert.IsFalse(result);
            Assert.IsNull(passValue);
        }

        [Test]
        public void TryGetSafely_CachedValueStillValid_DoesNotReInitialize()
        {
            int callCount = 0;
            var lazy = new ReInitLazyValue<object>(() =>
            {
                callCount++;
                return new object();
            });

            _ = lazy.TryGetSafely(out _);
            bool result = lazy.TryGetSafely(out var passValue);

            Assert.IsTrue(result);
            Assert.IsNotNull(passValue);
            Assert.AreEqual(1, callCount);
        }

        [Test]
        public void TryGetSafely_AllowReInitFalse_NeverInitialized_ReturnsFalseWithoutCallingInitializer()
        {
            int callCount = 0;
            var lazy = new ReInitLazyValue<object>(() =>
            {
                callCount++;
                return new object();
            });

            bool result = lazy.TryGetSafely(out var passValue, allowReInit: false);

            Assert.IsFalse(result);
            Assert.IsNull(passValue);
            Assert.AreEqual(0, callCount); // i.e. never touches the initializer
        }

        [Test]
        public void TryGetSafely_AllowReInitFalse_CachedValueStillValid_ReturnsTrueWithoutReInitializing()
        {
            int callCount = 0;
            var lazy = new ReInitLazyValue<object>(() =>
            {
                callCount++;
                return new object();
            });
            _ = lazy.TryGetSafely(out var firstValue); // establish a valid cached value first

            bool result = lazy.TryGetSafely(out var passValue, allowReInit: false);

            Assert.IsTrue(result);
            Assert.AreSame(firstValue, passValue);
            Assert.AreEqual(1, callCount); // still just the one call from establishing the cache
        }

        [Test]
        public void TryGetSafely_AllowReInitFalse_CachedValueIsRealNull_ReturnsFalseWithoutCallingInitializer()
        {
            int callCount = 0;
            var lazy = new ReInitLazyValue<object>(() =>
            {
                callCount++;
                return new object();
            });
            lazy.value = null; // simulate a value that was explicitly cleared

            bool result = lazy.TryGetSafely(out var passValue, allowReInit: false);

            Assert.IsFalse(result);
            Assert.IsNull(passValue);
            Assert.AreEqual(0, callCount);
        }

        [Test]
        public void ReInitLazyValue_WhenCachedValueBecomesNull_ReInitializesOnNextAccess()
        {
            // Covers the plain-C#-null case only: a real null reference should trigger re-initialization
            // The Unity "destroyed reference" (fake-null) case can't be modelled with a plain object
            // See the Play Mode test ReInitLazyValue_AfterCachedUnityObjectIsDestroyed_ReInitializesOnForceInit for that scenario, which needs a real Destroy() to exercise
            int callCount = 0;
            var lazy = new ReInitLazyValue<object>(() =>
            {
                callCount++;
                return new object();
            });

            _ = lazy.value; // first init, callCount == 1
            lazy.value = null; // simulate the cached reference becoming null

            var result = lazy.value;

            Assert.IsNotNull(result);
            Assert.AreEqual(2, callCount);
        }
    }
}

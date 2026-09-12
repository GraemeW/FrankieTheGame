using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace LowDefMustard.Localization.Tests.Editor
{
    public class LocalizableClassTableTypeRegistryTests
    {
        // Data Structures
        private class MarkerTypeA { }
        private class MarkerTypeB { }

        // Tests
        [Test]
        public void Register_ThenGetTableType_RoundTrips()
        {
            LocalizableClassTableTypeRegistry.Register(typeof(MarkerTypeA), TestTableType.Core);

            Assert.AreEqual(TestTableType.Core, LocalizableClassTableTypeRegistry.GetTableType(typeof(MarkerTypeA)));
        }

        [Test]
        public void Register_CalledTwiceForSameType_LastValueWins()
        {
            LocalizableClassTableTypeRegistry.Register(typeof(MarkerTypeB), TestTableType.Core);
            LocalizableClassTableTypeRegistry.Register(typeof(MarkerTypeB), TestTableType.UI);

            Assert.AreEqual(TestTableType.UI, LocalizableClassTableTypeRegistry.GetTableType(typeof(MarkerTypeB)));
        }

        [Test]
        public void GetTableType_UnregisteredType_ReturnsNull()
        {
            Assert.IsNull(LocalizableClassTableTypeRegistry.GetTableType(typeof(string)));
        }

        [Test]
        public void GetTableType_NullType_ReturnsNull()
        {
            Assert.IsNull(LocalizableClassTableTypeRegistry.GetTableType(null));
        }

        [Test]
        public void Register_NullOwningType_LogsWarningAndDoesNotThrow()
        {
            LogAssert.Expect(LogType.Warning, "LocalizableClassTableTypeRegistry.Register called with a null owningType.");
            Assert.DoesNotThrow(() => LocalizableClassTableTypeRegistry.Register(null, TestTableType.Core));
        }

        [Test]
        public void Register_NullTableType_LogsWarningAndDoesNotThrow()
        {
            LogAssert.Expect(LogType.Warning, $"LocalizableClassTableTypeRegistry.Register called with a null tableType for '{nameof(MarkerTypeA)}'.");
            Assert.DoesNotThrow(() => LocalizableClassTableTypeRegistry.Register(typeof(MarkerTypeA), null));
        }
    }
}

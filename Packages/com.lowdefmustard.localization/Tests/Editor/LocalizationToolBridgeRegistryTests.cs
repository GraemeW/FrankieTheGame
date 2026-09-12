using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace LowDefMustard.Localization.Tests.Editor
{
    public class LocalizationToolBridgeRegistryTests
    {
        // Setup
        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            TestLocalizationTool.RegisterTableCollectionNames(new Dictionary<TestTableType, string>());
        }

        // Tests
        [Test]
        public void TryGetBridge_RegisteredType_ReturnsTrueAndBridge()
        {
            bool found = LocalizationToolBridgeRegistry.TryGetBridge(typeof(TestTableType), out ILocalizationToolBridge bridge);

            Assert.IsTrue(found);
            Assert.IsNotNull(bridge);
        }

        [Test]
        public void TryGetBridge_UnregisteredType_ReturnsFalse()
        {
            bool found = LocalizationToolBridgeRegistry.TryGetBridge(typeof(DateTime), out ILocalizationToolBridge bridge);

            Assert.IsFalse(found);
            Assert.IsNull(bridge);
        }

        [Test]
        public void TryGetBridge_NullType_ReturnsFalse()
        {
            bool found = LocalizationToolBridgeRegistry.TryGetBridge(null, out ILocalizationToolBridge bridge);

            Assert.IsFalse(found);
            Assert.IsNull(bridge);
        }
    }
}

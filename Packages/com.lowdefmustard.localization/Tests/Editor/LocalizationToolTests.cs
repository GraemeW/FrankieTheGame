using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.Localization;

namespace LowDefMustard.Localization.Tests.Editor
{
    public class LocalizationToolTests
    {
        [SetUp]
        public void SetUp()
        {
            TestLocalizationTool.RegisterTableCollectionNames(new Dictionary<TestTableType, string>
            {
                { TestTableType.Core, "TestCore" },
                { TestTableType.UI, "TestUI" }
            });
        }

        [Test]
        public void MakeLocalizedString_CoreTable_SetsExpectedTableAndKey()
        {
            LocalizedString localizedString = TestLocalizationTool.MakeLocalizedString(TestTableType.Core, "SomeKey");

            Assert.AreEqual("TestCore", localizedString.TableReference.TableCollectionName);
            Assert.AreEqual("SomeKey", localizedString.TableEntryReference.Key);
        }

        [Test]
        public void MakeLocalizedString_UITable_SetsExpectedTableAndKey()
        {
            LocalizedString localizedString = TestLocalizationTool.MakeLocalizedString(TestTableType.UI, "Button.Confirm");

            Assert.AreEqual("TestUI", localizedString.TableReference.TableCollectionName);
            Assert.AreEqual("Button.Confirm", localizedString.TableEntryReference.Key);
        }
    }
}

using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.TestTools;

namespace LowDefMustard.Localization.Tests.Editor
{
    // Testing Notes:
    //  - Since we're calling Unity's LocalizationSettings directly in LocalizationLocale, testing is done on a real game surface
    //      - Setup/Teardown are nominally used to prevent test data leaking
    //  - This package assumes english language set up as "en", most tests will fail otherwise
    
    public class LocalizationLocaleTests
    {
        // State
        private Locale originalSelectedLocale;

        #region Setup
        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            LocalizationLocale.TriggerLocalizationSettingsInitialization();
        }

        [SetUp]
        public void SetUp()
        {
            originalSelectedLocale = LocalizationSettings.SelectedLocale;
        }

        [TearDown]
        public void TearDown()
        {
            LocalizationSettings.SelectedLocale = originalSelectedLocale;
        }
        #endregion

        #region Tests
        [Test]
        public void TriggerLocalizationSettingsInitialization_DoesNotThrow()
        {
            Assert.DoesNotThrow(LocalizationLocale.TriggerLocalizationSettingsInitialization);
        }

        [Test]
        public void GetSupportedLocaleCodes_IncludesEnglish()
        {
            CollectionAssert.Contains(LocalizationLocale.GetSupportedLocaleCodes(), "en");
        }

        [Test]
        public void SetLocale_ValidCode_UpdatesSelectedLocaleAndCurrentLocaleCode()
        {
            LocalizationLocale.SetLocale("en");

            Assert.AreEqual("en", LocalizationSettings.SelectedLocale.Identifier.Code);
            Assert.AreEqual("en", LocalizationLocale.GetCurrentLocaleCode());
        }

        [Test]
        public void SetLocale_InvalidCode_FallsBackToProjectLocaleAndLogsWarning()
        {
            string projectLocaleCode = LocalizationSettings.ProjectLocale.Identifier.Code;

            LogAssert.Expect(LogType.Warning, $"No locale found for zz-Invalid - setting to fallback {projectLocaleCode}");
            LocalizationLocale.SetLocale("zz-Invalid");

            Assert.AreEqual(projectLocaleCode, LocalizationSettings.SelectedLocale.Identifier.Code);
        }

        [Test]
        public void InitializeDefaultLocale_Forced_SetsSelectedLocaleToProjectLocale()
        {
            string projectLocaleCode = LocalizationSettings.ProjectLocale.Identifier.Code;
            // Force a locale different from the project default first, so the assertion below can't pass by coincidence
            //  - falls back to skipping this step if the project only has one locale configured
            string differentCode = LocalizationLocale.GetSupportedLocaleCodes().FirstOrDefault(code => code != projectLocaleCode);
            if (differentCode != null) { LocalizationLocale.SetLocale(differentCode); }

            LocalizationLocale.InitializeDefaultLocale(forceInitialization: true);

            Assert.AreEqual(projectLocaleCode, LocalizationSettings.SelectedLocale.Identifier.Code);
        }
        #endregion
    }
}

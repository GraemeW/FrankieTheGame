using System;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace LowDefMustard.Core.Tests.Editor
{
    public class LDMLogTests
    {
        // State
        private LDMLogSettings settings;

        #region Setup
        [SetUp]
        public void SetUp()
        {
            settings = ScriptableObject.CreateInstance<LDMLogSettings>();
            LDMLog.UseSettings(settings);
        }

        [TearDown]
        public void TearDown()
        {
            LDMLog.Reload();
            Object.DestroyImmediate(settings);
        }
        #endregion

        #region Tests
        [Test]
        public void Info_EmitsWithColoredCategoryPrefix()
        {
            LDMLog.Info(LDMLogCategory.Combat, "info message");

            LogAssert.Expect(LogType.Log, new Regex(@"^<color=#[0-9A-F]{6}>\[Combat\]</color> info message$"));
        }

        [Test]
        public void Info_SuppressedWhenCategoryDisabled()
        {
            settings.SetCategoryEnabled(LDMLogCategory.Combat, false);

            LDMLog.Info(LDMLogCategory.Combat, "info message");

            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void Important_EmitsWhenCategoryEnabled()
        {
            LDMLog.Important(LDMLogCategory.Saving, "important message");

            LogAssert.Expect(LogType.Log, new Regex(@"\[Saving\]</color> important message$"));
        }

        [Test]
        public void Important_SuppressedWhenCategoryDisabled()
        {
            settings.SetCategoryEnabled(LDMLogCategory.Saving, false);

            LDMLog.Important(LDMLogCategory.Saving, "important message");

            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void Important_MultiFlagEmitsWhenAnyFlagEnabled()
        {
            settings.SetCategoryEnabled(LDMLogCategory.Combat, false);

            LDMLog.Important(LDMLogCategory.Combat | LDMLogCategory.Stats, "multi message");

            LogAssert.Expect(LogType.Log, new Regex(@"\[Combat, Stats\]</color> multi message$"));
        }

        [Test]
        public void WarningAndError_EmitEvenWhenCategoryDisabled()
        {
            settings.SetCategoryEnabled(LDMLogCategory.Zones, false);

            LDMLog.Warning(LDMLogCategory.Zones, "warning message");
            LDMLog.Error(LDMLogCategory.Zones, "error message");

            LogAssert.Expect(LogType.Warning, new Regex(@"\[Zones\]</color> warning message$"));
            LogAssert.Expect(LogType.Error, new Regex(@"\[Zones\]</color> error message$"));
        }

        [Test]
        public void Exception_PassesThrough()
        {
            LDMLog.Exception(new InvalidOperationException("exception message"));

            LogAssert.Expect(LogType.Exception, new Regex("InvalidOperationException: exception message"));
        }

        [Test]
        public void IsEnabled_FollowsSettings()
        {
            settings.SetCategoryEnabled(LDMLogCategory.UI, false);

            Assert.IsFalse(LDMLog.IsEnabled(LDMLogCategory.UI));
            Assert.IsTrue(LDMLog.IsEnabled(LDMLogCategory.Control));
        }

        [Test]
        public void NoSettings_EverythingEnabledWithPlainPrefix()
        {
            LDMLog.UseSettings(null);

            Assert.IsTrue(LDMLog.IsEnabled(LDMLogCategory.Combat));
            Assert.IsFalse(LDMLog.IsEnabled(LDMLogCategory.None));
            LDMLog.Info(LDMLogCategory.Combat, "plain message");
            LogAssert.Expect(LogType.Log, "[Combat] plain message");
        }
        #endregion
    }
}

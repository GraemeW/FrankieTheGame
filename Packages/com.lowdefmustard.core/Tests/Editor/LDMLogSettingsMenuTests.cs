using System.Collections.Generic;
using LowDefMustard.Core.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace LowDefMustard.Core.Tests.Editor
{
    public class LDMLogSettingsMenuTests
    {
        // Const Tunables
        private const string _firstAssetPath = "Assets/LDMLogSettingsMenuTests_First.asset";
        private const string _secondAssetPath = "Assets/LDMLogSettingsMenuTests_Second.asset";

        // State
        private Object[] originalPreloadedAssets;
        private LDMLogSettings originalSettings;
        private LDMLogSettings firstSettings;
        private LDMLogSettings secondSettings;

        #region Setup
        [SetUp]
        public void SetUp()
        {
            originalPreloadedAssets = PlayerSettings.GetPreloadedAssets();
            originalSettings = LDMLogSettingsMenu.GetRegisteredSettings();
            firstSettings = CreateAsset(_firstAssetPath);
            secondSettings = CreateAsset(_secondAssetPath);
        }

        [TearDown]
        public void TearDown()
        {
            PlayerSettings.SetPreloadedAssets(originalPreloadedAssets);
            if (originalSettings != null) { EditorBuildSettings.AddConfigObject(LDMLog.editorConfigKey, originalSettings, true); }
            else { EditorBuildSettings.RemoveConfigObject(LDMLog.editorConfigKey); }
            LDMLog.Reload();

            AssetDatabase.DeleteAsset(_firstAssetPath);
            AssetDatabase.DeleteAsset(_secondAssetPath);
        }
        #endregion

        #region PrivateMethods
        private static LDMLogSettings CreateAsset(string path)
        {
            var settings = ScriptableObject.CreateInstance<LDMLogSettings>();
            AssetDatabase.CreateAsset(settings, path);
            return settings;
        }

        private static int CountPreloaded(LDMLogSettings settings)
        {
            int count = 0;
            foreach (Object asset in new List<Object>(PlayerSettings.GetPreloadedAssets())) { if (asset == settings) { count++; } }
            return count;
        }
        #endregion
        
        #region Tests
        [Test]
        public void Register_SetsEditorConfigObject()
        {
            LDMLogSettingsMenu.Register(firstSettings);

            Assert.AreEqual(firstSettings, LDMLogSettingsMenu.GetRegisteredSettings());
        }

        [Test]
        public void Register_AddsToPreloadedAssetsOnce()
        {
            LDMLogSettingsMenu.Register(firstSettings);
            LDMLogSettingsMenu.Register(firstSettings);

            Assert.AreEqual(1, CountPreloaded(firstSettings));
        }

        [Test]
        public void Register_ReplacesOtherPreloadedSettings()
        {
            LDMLogSettingsMenu.Register(firstSettings);
            LDMLogSettingsMenu.Register(secondSettings);

            Assert.AreEqual(0, CountPreloaded(firstSettings));
            Assert.AreEqual(1, CountPreloaded(secondSettings));
        }

        [Test]
        public void Register_KeepsUnrelatedPreloadedAssets()
        {
            LDMLogSettingsMenu.Register(firstSettings);

            foreach (Object asset in originalPreloadedAssets)
            {
                if (asset is LDMLogSettings) { continue; }
                Assert.Contains(asset, PlayerSettings.GetPreloadedAssets());
            }
        }

        [Test]
        public void Register_SwitchesLDMLogToRegisteredSettings()
        {
            firstSettings.SetCategoryEnabled(LDMLogCategory.Combat, false);

            LDMLogSettingsMenu.Register(firstSettings);

            Assert.IsFalse(LDMLog.IsEnabled(LDMLogCategory.Combat));
        }
        #endregion
    }
}

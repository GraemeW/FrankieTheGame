using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using Object = UnityEngine.Object;

namespace LowDefMustard.Localization.Tests.Editor
{
    public class ILocalizableCoreInstanceMethodTests
    {
        // Const Tunables
        private const string _scratchTableName = "ScratchTest_ILocalizable_SafeToDelete";
        private const string _scratchTablePath = "Assets/Localization/Table_" + _scratchTableName;

        // State
        private TestLocalizableTarget testLocalizableTarget;

        #region DataStructures
        // Minimal ILocalizableCore implementer - iCachedName uses a real backing field (via auto-property)
        private sealed class TestLocalizableTarget : ScriptableObject, ILocalizableCore
        {
            public string iCachedName { get; set; }
            public Enum localizationTableTypeValue => TestTableType.ScratchAssetLocalizable;
            public List<TableEntryReference> GetLocalizationEntries() => new();
        }
        #endregion

        #region Setup
        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            TestLocalizationTool.RegisterTableCollectionNames(new Dictionary<TestTableType, string>
            {
                { TestTableType.ScratchAssetLocalizable, _scratchTableName }
            });

            bool created = TestLocalizationTool.GetOrMakeTableCollection(TestTableType.ScratchAssetLocalizable, out _);
            Assert.IsTrue(created, "Fixture setup failed to create the scratch table collection");
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            if (AssetDatabase.IsValidFolder(_scratchTablePath))
            {
                AssetDatabase.DeleteAsset(_scratchTablePath);
            }
        }

        [TearDown]
        public void TearDown()
        {
            if (testLocalizableTarget != null) { Object.DestroyImmediate(testLocalizableTarget); }
        }
        #endregion

        #region PrivateMethods
        private TestLocalizableTarget CreateTarget(string targetName)
        {
            testLocalizableTarget = ScriptableObject.CreateInstance<TestLocalizableTarget>();
            testLocalizableTarget.name = targetName;
            return testLocalizableTarget;
        }
        #endregion

        #region TryLocalizeStandardEntries
        [Test]
        public void TryLocalizeStandardEntries_SetToName_UsesTargetObjectNameAsValue()
        {
            TestLocalizableTarget target = CreateTarget("Goblin");
            var standardEntries = new List<(string propertyName, LocalizedString localizedString, bool setToName)>
            {
                ("localizedName", new LocalizedString(), true)
            };

            ILocalizableCore.TryLocalizeStandardEntries(target, target, standardEntries);

            string key = ILocalizableCore.GetStandardLocalizationKey("Goblin", nameof(TestLocalizableTarget), "localizedName");
            Assert.AreEqual("Goblin", TestLocalizationTool.GetEnglishEntry(TestTableType.ScratchAssetLocalizable, key));
        }

        [Test]
        public void TryLocalizeStandardEntries_NotSetToName_CreatesPlaceholderEntry()
        {
            TestLocalizableTarget target = CreateTarget("Skeleton");
            var standardEntries = new List<(string propertyName, LocalizedString localizedString, bool setToName)>
            {
                ("localizedDescription", new LocalizedString(), false)
            };

            ILocalizableCore.TryLocalizeStandardEntries(target, target, standardEntries);

            string key = ILocalizableCore.GetStandardLocalizationKey("Skeleton", nameof(TestLocalizableTarget), "localizedDescription");
            Assert.IsTrue(TestLocalizationTool.HasEnglishEntry(TestTableType.ScratchAssetLocalizable, key));
        }

        [Test]
        public void TryLocalizeStandardEntries_EntryAlreadyExists_DoesNotOverwrite()
        {
            TestLocalizableTarget target = CreateTarget("Orc");
            string key = ILocalizableCore.GetStandardLocalizationKey("Orc", nameof(TestLocalizableTarget), "localizedName");
            TestLocalizationTool.AddUpdateEnglishEntry(TestTableType.ScratchAssetLocalizable, key, "Custom Value");

            var standardEntries = new List<(string propertyName, LocalizedString localizedString, bool setToName)>
            {
                ("localizedName", new LocalizedString(), true)
            };
            ILocalizableCore.TryLocalizeStandardEntries(target, target, standardEntries);

            Assert.AreEqual("Custom Value", TestLocalizationTool.GetEnglishEntry(TestTableType.ScratchAssetLocalizable, key));
        }
        #endregion

        #region ReconcileCachedName
        [Test]
        public void ReconcileCachedName_TargetRenamed_RenamesExistingKeyAndInvokesCallback()
        {
            TestLocalizableTarget target = CreateTarget("NewName");
            target.iCachedName = "OldName";
            string oldKey = ILocalizableCore.GetStandardLocalizationKey("OldName", nameof(TestLocalizableTarget), "localizedName");
            string newKey = ILocalizableCore.GetStandardLocalizationKey("NewName", nameof(TestLocalizableTarget), "localizedName");
            TestLocalizationTool.AddUpdateEnglishEntry(TestTableType.ScratchAssetLocalizable, oldKey, "Value");

            bool renameCallbackInvoked = false;
            var standardEntries = new List<(string propertyName, LocalizedString _, bool __)>
            {
                ("localizedName", null, false)
            };
            ILocalizableCore.ReconcileCachedName(target, target, standardEntries, () => renameCallbackInvoked = true);

            Assert.IsFalse(TestLocalizationTool.HasTableEntry(TestTableType.ScratchAssetLocalizable, oldKey));
            Assert.IsTrue(TestLocalizationTool.HasTableEntry(TestTableType.ScratchAssetLocalizable, newKey));
            Assert.AreEqual("NewName", target.iCachedName);
            Assert.IsTrue(renameCallbackInvoked);
        }

        [Test]
        public void ReconcileCachedName_NameUnchanged_DoesNothing()
        {
            TestLocalizableTarget target = CreateTarget("SameName");
            target.iCachedName = "SameName";

            bool renameCallbackInvoked = false;
            var standardEntries = new List<(string propertyName, LocalizedString _, bool __)>
            {
                ("localizedName", null, false)
            };
            ILocalizableCore.ReconcileCachedName(target, target, standardEntries, () => renameCallbackInvoked = true);

            Assert.IsFalse(renameCallbackInvoked);
        }
        #endregion
    }
}

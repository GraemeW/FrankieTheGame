using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

namespace LowDefMustard.Localization.Tests.Editor
{
    // Test Notes:
    //  - A StringTableCollection is created once for the whole fixture and torn down once
    //  - This test assumes the project's LocalizationSettings already has an English locale in AvailableLocales
    
    public class LocalizationToolEditorAssetTests
    {
        // Const Tunables
        private const string _scratchTableName = "ScratchTest_LocToolEditor_SafeToDelete";
        private const string _scratchTablePath = "Assets/Localization/Table_" + _scratchTableName;

        #region Setup
        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            TestLocalizationTool.RegisterTableCollectionNames(new Dictionary<TestTableType, string>
            {
                { TestTableType.ScratchAssetEditor, _scratchTableName }
            });

            bool created = TestLocalizationTool.GetOrMakeTableCollection(TestTableType.ScratchAssetEditor, out _);
            Assert.IsTrue(created, "Fixture setup failed to create the scratch table collection");
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            if (AssetDatabase.IsValidFolder(_scratchTablePath))
            {
                AssetDatabase.DeleteAsset(_scratchTablePath);
            }
            TestLocalizationTool.RefreshTableCache();
        }
        #endregion

        #region Getters
        [Test]
        public void HasTableEntry_MissingKey_ReturnsFalse()
        {
            Assert.IsFalse(TestLocalizationTool.HasTableEntry(TestTableType.ScratchAssetEditor, "Never.Added"));
        }

        [Test]
        public void HasEnglishEntry_MissingKey_ReturnsFalse()
        {
            Assert.IsFalse(TestLocalizationTool.HasEnglishEntry(TestTableType.ScratchAssetEditor, "Never.Added"));
        }

        [Test]
        public void GetEnglishEntry_MissingKey_ReturnsEmptyString()
        {
            Assert.AreEqual("", TestLocalizationTool.GetEnglishEntry(TestTableType.ScratchAssetEditor, "Never.Added"));
        }
        
        [Test]
        public void GetTableEntryReferencedByID_NameReference_ResolvesToIdReference()
        {
            TestLocalizationTool.AddUpdateEnglishEntry(TestTableType.ScratchAssetEditor, "ResolveById.Key", "Value");

            TableEntryReference resolved = TestLocalizationTool.GetTableEntryReferencedByID(TestTableType.ScratchAssetEditor, (TableEntryReference)"ResolveById.Key");

            Assert.AreEqual(TableEntryReference.Type.Id, resolved.ReferenceType);
        }
        #endregion
        
        #region AddUpdateEntry
        [Test]
        public void AddUpdateEnglishEntry_NewKey_CreatesEntryReadableViaGetEnglishEntry()
        {
            bool added = TestLocalizationTool.AddUpdateEnglishEntry(TestTableType.ScratchAssetEditor, "AddUpdate.ByName", "Hello");

            Assert.IsTrue(added);
            Assert.IsTrue(TestLocalizationTool.HasEnglishEntry(TestTableType.ScratchAssetEditor, "AddUpdate.ByName"));
            Assert.AreEqual("Hello", TestLocalizationTool.GetEnglishEntry(TestTableType.ScratchAssetEditor, "AddUpdate.ByName"));
        }

        [Test]
        public void AddUpdateEnglishEntry_ExistingKey_UpdatesValue()
        {
            TestLocalizationTool.AddUpdateEnglishEntry(TestTableType.ScratchAssetEditor, "AddUpdate.Overwrite", "Original");
            bool updated = TestLocalizationTool.AddUpdateEnglishEntry(TestTableType.ScratchAssetEditor, "AddUpdate.Overwrite", "Replaced");

            Assert.IsTrue(updated);
            Assert.AreEqual("Replaced", TestLocalizationTool.GetEnglishEntry(TestTableType.ScratchAssetEditor, "AddUpdate.Overwrite"));
        }
        #endregion

        #region RemoveEntry
        [Test]
        public void RemoveEntry_ExistingKey_RemovesIt()
        {
            TestLocalizationTool.AddUpdateEnglishEntry(TestTableType.ScratchAssetEditor, "Remove.Me", "Temp");
            Assert.IsTrue(TestLocalizationTool.HasTableEntry(TestTableType.ScratchAssetEditor, "Remove.Me"));

            bool removed = TestLocalizationTool.RemoveEntry(TestTableType.ScratchAssetEditor, "Remove.Me");

            Assert.IsTrue(removed);
            Assert.IsFalse(TestLocalizationTool.HasTableEntry(TestTableType.ScratchAssetEditor, "Remove.Me"));
        }
        #endregion

        #region MakeOrRenameKey
        [Test]
        public void MakeOrRenameKey_EmptyReference_CreatesNewKey()
        {
            bool created = TestLocalizationTool.MakeOrRenameKey(TestTableType.ScratchAssetEditor, new TableEntryReference(), "MakeOrRename.NewKey");

            Assert.IsTrue(created);
            Assert.IsTrue(TestLocalizationTool.HasTableEntry(TestTableType.ScratchAssetEditor, "MakeOrRename.NewKey"));
        }

        [Test]
        public void MakeOrRenameKey_ExistingKey_RenamesIt()
        {
            TestLocalizationTool.AddUpdateEnglishEntry(TestTableType.ScratchAssetEditor, "MakeOrRename.OldName", "Value");

            bool renamed = TestLocalizationTool.MakeOrRenameKey(TestTableType.ScratchAssetEditor, (TableEntryReference)"MakeOrRename.OldName", "MakeOrRename.NewName");

            Assert.IsTrue(renamed);
            Assert.IsFalse(TestLocalizationTool.HasTableEntry(TestTableType.ScratchAssetEditor, "MakeOrRename.OldName"));
            Assert.IsTrue(TestLocalizationTool.HasTableEntry(TestTableType.ScratchAssetEditor, "MakeOrRename.NewName"));
        }
        #endregion

        #region ResolveKeyName
        [Test]
        public void ResolveKeyName_LocalizedStringPointingAtRealEntry_ReturnsKeyName()
        {
            TestLocalizationTool.AddUpdateEnglishEntry(TestTableType.ScratchAssetEditor, "ResolveKeyName.Key", "Value");
            LocalizedString localizedString = TestLocalizationTool.MakeLocalizedString(TestTableType.ScratchAssetEditor, "ResolveKeyName.Key");

            string keyName = TestLocalizationTool.ResolveKeyName(TestTableType.ScratchAssetEditor, localizedString, out TableEntryReference tableEntryReference);

            Assert.AreEqual("ResolveKeyName.Key", keyName);
            Assert.AreEqual(TableEntryReference.Type.Id, tableEntryReference.ReferenceType);
        }

        [Test]
        public void ResolveKeyName_EmptyLocalizedString_ReturnsEmptyStringAndEmptyReference()
        {
            string keyName = TestLocalizationTool.ResolveKeyName(TestTableType.ScratchAssetEditor, new LocalizedString(), out TableEntryReference tableEntryReference);

            Assert.AreEqual("", keyName);
            Assert.AreEqual(TableEntryReference.Type.Empty, tableEntryReference.ReferenceType);
        }
        #endregion

        #region InitializeLocalEntry
        [Test]
        public void InitializeLocalEntry_NewKey_CreatesEntryAndReturnsTrue()
        {
            bool initialized = TestLocalizationTool.InitializeLocalEntry(TestTableType.ScratchAssetEditor, null, "InitLocal.NewKey");

            Assert.IsTrue(initialized);
            Assert.IsTrue(TestLocalizationTool.HasTableEntry(TestTableType.ScratchAssetEditor, "InitLocal.NewKey"));
        }

        [Test]
        public void InitializeLocalEntry_AlreadyPopulatedEntry_ReturnsFalse()
        {
            TestLocalizationTool.AddUpdateEnglishEntry(TestTableType.ScratchAssetEditor, "InitLocal.Existing", "Already has text");
            LocalizedString localizedString = TestLocalizationTool.MakeLocalizedString(TestTableType.ScratchAssetEditor, "InitLocal.Existing");

            bool initialized = TestLocalizationTool.InitializeLocalEntry(TestTableType.ScratchAssetEditor, localizedString, "InitLocal.Existing");

            Assert.IsFalse(initialized);
        }
        #endregion

        #region TryLocalizeEntry
        [Test]
        public void TryLocalizeEntry_NewValue_UpdatesEntryAndReturnsTrue()
        {
            LocalizedString localizedString = TestLocalizationTool.MakeLocalizedString(TestTableType.ScratchAssetEditor, "TryLocalize.Key");

            bool localized = TestLocalizationTool.TryLocalizeEntry(TestTableType.ScratchAssetEditor, localizedString, "TryLocalize.Key", "New Value");

            Assert.IsTrue(localized);
            Assert.AreEqual("New Value", TestLocalizationTool.GetEnglishEntry(TestTableType.ScratchAssetEditor, "TryLocalize.Key"));
        }

        [Test]
        public void TryLocalizeEntry_SameValueAlreadyPresent_ReturnsFalse()
        {
            TestLocalizationTool.AddUpdateEnglishEntry(TestTableType.ScratchAssetEditor, "TryLocalize.Unchanged", "Same Value");
            LocalizedString localizedString = TestLocalizationTool.MakeLocalizedString(TestTableType.ScratchAssetEditor, "TryLocalize.Unchanged");

            bool localized = TestLocalizationTool.TryLocalizeEntry(TestTableType.ScratchAssetEditor, localizedString, "TryLocalize.Unchanged", "Same Value");

            Assert.IsFalse(localized);
        }
        #endregion

        #region SafelyUpdateReference
        [Test]
        public void SafelyUpdateReference_ExistingKey_PointsLocalizedStringAtItById()
        {
            TestLocalizationTool.AddUpdateEnglishEntry(TestTableType.ScratchAssetEditor, "SafelyUpdate.Key", "Value");
            var localizedString = new LocalizedString();

            bool updated = TestLocalizationTool.SafelyUpdateReference(TestTableType.ScratchAssetEditor, localizedString, "SafelyUpdate.Key");

            Assert.IsTrue(updated);
            Assert.AreEqual(TableEntryReference.Type.Id, localizedString.TableEntryReference.ReferenceType);
        }

        [Test]
        public void SafelyUpdateReference_MissingKey_ReturnsFalse()
        {
            var localizedString = new LocalizedString();

            bool updated = TestLocalizationTool.SafelyUpdateReference(TestTableType.ScratchAssetEditor, localizedString, "Never.Added");

            Assert.IsFalse(updated);
        }
        #endregion
    }
}

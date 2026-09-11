using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization.Tables;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace LowDefMustard.Localization.Tests.Editor
{
    // Test Notes:
    //  - LocalizationDeletionHandler's subscribes to ILocalizableCore.onBeforeDestroyedInEditor event, so event firing is used to exercise its logic
    //  - Unity guarantees that constructor has already run by the time any test executes in the same editor session
    
    public class LocalizationDeletionHandlerTests
    {
        // Const Tunables
        private const string _scratchTableName = "ScratchTest_Deletion_SafeToDelete";
        private const string _scratchTablePath = "Assets/Localization/Table_" + _scratchTableName;

        // State
        private readonly List<Object> createdObjects = new();

        #region DataStructures
        private sealed class TestDeletionLocalizableTarget : ScriptableObject, ILocalizableCore
        {
            public string iCachedName { get; set; }
            public Enum tableTypeOverride;
            public List<TableEntryReference> entriesToReturn = new();
            public Enum localizationTableTypeValue => tableTypeOverride;
            public List<TableEntryReference> GetLocalizationEntries() => entriesToReturn;
        }

        private sealed class TestDeletionLocalizableMonoBehaviour : MonoBehaviour, ILocalizableCore
        {
            public string iCachedName { get; set; }
            public List<TableEntryReference> entriesToReturn = new();
            public Enum localizationTableTypeValue => TestTableType.ScratchAssetDeletion;
            public List<TableEntryReference> GetLocalizationEntries() => entriesToReturn;
        }
        #endregion

        #region Setup
        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            TestLocalizationTool.RegisterTableCollectionNames(new Dictionary<TestTableType, string>
            {
                { TestTableType.ScratchAssetDeletion, _scratchTableName }
            });

            bool created = TestLocalizationTool.GetOrMakeTableCollection(TestTableType.ScratchAssetDeletion, out _);
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

        [TearDown]
        public void TearDown()
        {
            foreach (Object createdObject in createdObjects.Where(createdObject => createdObject != null))
            {
                Object.DestroyImmediate(createdObject);
            }
            createdObjects.Clear();
        }
        #endregion

        #region PrivateMethods
        private TestDeletionLocalizableTarget CreateScriptableObjectTarget()
        {
            var target = ScriptableObject.CreateInstance<TestDeletionLocalizableTarget>();
            target.tableTypeOverride = TestTableType.ScratchAssetDeletion;
            createdObjects.Add(target);
            return target;
        }
        #endregion

        #region OnBeforeDestroyedInvocations
        [Test]
        public void OnBeforeDestroyedInEditor_ScriptableObjectWithEntries_RemovesEveryStandardEntry()
        {
            TestLocalizationTool.AddUpdateEnglishEntry(TestTableType.ScratchAssetDeletion, "Deletion.SO.KeyA", "Value A");
            TestLocalizationTool.AddUpdateEnglishEntry(TestTableType.ScratchAssetDeletion, "Deletion.SO.KeyB", "Value B");
            TestDeletionLocalizableTarget target = CreateScriptableObjectTarget();
            target.entriesToReturn = new List<TableEntryReference> { "Deletion.SO.KeyA", "Deletion.SO.KeyB" };

            ILocalizableCore.TriggerOnBeforeDestroyedInEditor(target.localizationTableTypeValue, target, target);

            Assert.IsFalse(TestLocalizationTool.HasTableEntry(TestTableType.ScratchAssetDeletion, "Deletion.SO.KeyA"));
            Assert.IsFalse(TestLocalizationTool.HasTableEntry(TestTableType.ScratchAssetDeletion, "Deletion.SO.KeyB"));
        }

        [Test]
        public void OnBeforeDestroyedInEditor_NoEntries_EarlyReturnsWithoutTouchingUnrelatedEntries()
        {
            TestLocalizationTool.AddUpdateEnglishEntry(TestTableType.ScratchAssetDeletion, "Deletion.Untouched", "Should survive");
            TestDeletionLocalizableTarget target = CreateScriptableObjectTarget();
            target.entriesToReturn = new List<TableEntryReference>();

            ILocalizableCore.TriggerOnBeforeDestroyedInEditor(target.localizationTableTypeValue, target, target);

            Assert.IsTrue(TestLocalizationTool.HasTableEntry(TestTableType.ScratchAssetDeletion, "Deletion.Untouched"));
        }

        [Test]
        public void OnBeforeDestroyedInEditor_NullTableType_LogsWarningAndDoesNotThrow()
        {
            TestLocalizationTool.AddUpdateEnglishEntry(TestTableType.ScratchAssetDeletion, "Deletion.NullTableType", "Should survive");
            TestDeletionLocalizableTarget target = CreateScriptableObjectTarget();
            target.tableTypeOverride = null;
            target.entriesToReturn = new List<TableEntryReference> { "Deletion.NullTableType" };

            LogAssert.Expect(LogType.Warning, "TableType or LocalizationToolBridge could not be not found");
            Assert.DoesNotThrow(() => ILocalizableCore.TriggerOnBeforeDestroyedInEditor(null, target, target));

            Assert.IsTrue(TestLocalizationTool.HasTableEntry(TestTableType.ScratchAssetDeletion, "Deletion.NullTableType"));
        }

        [Test]
        public void OnBeforeDestroyedInEditor_MonoBehaviourNotPartOfAPrefabInstance_RemovesEveryStandardEntry()
        {
            TestLocalizationTool.AddUpdateEnglishEntry(TestTableType.ScratchAssetDeletion, "Deletion.MB.Key", "Value");
            var gameObject = new GameObject("DeletionTestTarget");
            createdObjects.Add(gameObject);
            TestDeletionLocalizableMonoBehaviour target = gameObject.AddComponent<TestDeletionLocalizableMonoBehaviour>();
            target.entriesToReturn = new List<TableEntryReference> { "Deletion.MB.Key" };

            ILocalizableCore.TriggerOnBeforeDestroyedInEditor(target.localizationTableTypeValue, gameObject, target);

            Assert.IsFalse(TestLocalizationTool.HasTableEntry(TestTableType.ScratchAssetDeletion, "Deletion.MB.Key"));
        }
        #endregion
    }
}

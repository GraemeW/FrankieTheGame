using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace LowDefMustard.Localization.Tests.Editor
{
    // Test Notes:
    //  - Must use a UnityEditor.UIElements.InspectorElement (i.e. instead of .PropertyField) in order to trigger CreatePropertyGUI()
    
    public class SimpleLocalizedStringDrawerTests
    {
        // Const Tunables
        private const string _scratchTableName = "ScratchTest_Drawer_SafeToDelete";
        private const string _scratchTablePath = "Assets/Localization/Table_" + _scratchTableName;

        // State
        private HeadlessEditorWindowTestHelper window;
        private TestDrawerTarget target;
        private SerializedObject serializedObject;

        #region DataStructures
        private class TestDrawerTarget : ScriptableObject
        {
            [SimpleLocalizedString(TestTableType.ScratchAssetDrawer, true)] public LocalizedString localizedField;
        }

        private readonly struct DrawerElements
        {
            public readonly TextField keyField;
            public readonly TextField contentsField;
            public readonly Toggle lockToggle;
            public readonly Button newKeyButton;
            public readonly Button renameKeyButton;
            public readonly Button deleteKeyButton;

            public DrawerElements(VisualElement root)
            {
                keyField = root.Query<TextField>("keyField");
                contentsField = root.Query<TextField>("contentField");
                lockToggle = root.Query<Toggle>("lockToggle");
                newKeyButton = root.Query<Button>("newKeyButton");
                renameKeyButton = root.Query<Button>("renameKeyButton");
                deleteKeyButton = root.Query<Button>("deleteKeyButton");
            }
        }
        #endregion

        #region Setup
        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            TestLocalizationTool.RegisterTableCollectionNames(new Dictionary<TestTableType, string>
            {
                { TestTableType.ScratchAssetDrawer, _scratchTableName }
            });
            
            bool created = TestLocalizationTool.GetOrMakeTableCollection(TestTableType.ScratchAssetDrawer, out _);
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

        [SetUp]
        public void SetUp()
        {
            window = new HeadlessEditorWindowTestHelper();
            target = ScriptableObject.CreateInstance<TestDrawerTarget>();
        }

        [TearDown]
        public void TearDown()
        {
            window?.Close();
            if (target != null) { Object.DestroyImmediate(target); }
        }
        #endregion

        #region PrivateMethods
        private DrawerElements BuildDrawerUI()
        {
            serializedObject = new SerializedObject(target);
            var inspectorElement = new InspectorElement(serializedObject);
            window.root.Add(inspectorElement);
            return new DrawerElements(window.root);
        }

        private static void Click(Button button)
        {
            using ClickEvent clickEvent = ClickEvent.GetPooled();
            clickEvent.target = button;
            button.SendEvent(clickEvent);
        }

        private static void Unlock(DrawerElements elements)
        {
            elements.lockToggle.value = true;
        }
        #endregion

        #region Tests
        [Test]
        public void CreatePropertyGUI_AttributedField_BuildsRealDrawerUIRatherThanDefaultField()
        {
            BuildDrawerUI();

            Assert.IsEmpty(window.root.Query<HelpBox>().ToList(), "Drawer rendered an error box instead of its normal UI");
            Assert.AreEqual(2, window.root.Query<TextField>().ToList().Count, "Expected exactly the key and contents text fields");
            Assert.AreEqual(3, window.root.Query<Button>().ToList().Count, "Expected exactly the new/rename/delete key buttons");
            Assert.AreEqual(1, window.root.Query<Toggle>().ToList().Count, "Expected exactly the lock toggle");
        }

        [Test]
        public void LockToggle_InitialState_KeyManagementButtonsDisabled()
        {
            DrawerElements elements = BuildDrawerUI();

            Assert.IsFalse(elements.lockToggle.value);
            Assert.IsFalse(elements.newKeyButton.enabledInHierarchy);
            Assert.IsFalse(elements.renameKeyButton.enabledInHierarchy);
            Assert.IsFalse(elements.deleteKeyButton.enabledInHierarchy);
        }

        [Test]
        public void LockToggle_Unlocked_EnablesNewKeyButtonButNotRenameOrDeleteWhileKeyIsEmpty()
        {
            DrawerElements elements = BuildDrawerUI();

            Unlock(elements);

            Assert.IsTrue(elements.newKeyButton.enabledInHierarchy);
            Assert.IsFalse(elements.renameKeyButton.enabledInHierarchy, "Rename should stay disabled until a key exists");
            Assert.IsFalse(elements.deleteKeyButton.enabledInHierarchy, "Delete should stay disabled until a key exists");
        }

        [Test]
        public void NewKeyButton_Click_CreatesEntryAndEnablesRenameAndDelete()
        {
            DrawerElements elements = BuildDrawerUI();
            Unlock(elements);

            Click(elements.newKeyButton);

            string generatedKey = elements.keyField.value;
            Assert.IsFalse(string.IsNullOrWhiteSpace(generatedKey), "Expected a generated key to appear in the key field");
            Assert.IsTrue(TestLocalizationTool.HasTableEntry(TestTableType.ScratchAssetDrawer, generatedKey));
            Assert.IsTrue(elements.renameKeyButton.enabledInHierarchy);
            Assert.IsTrue(elements.deleteKeyButton.enabledInHierarchy);
        }

        [Test]
        public void ContentsField_ValueChanged_UpdatesEnglishEntryForCurrentKey()
        {
            DrawerElements elements = BuildDrawerUI();
            Unlock(elements);
            Click(elements.newKeyButton);
            string key = elements.keyField.value;

            elements.contentsField.value = "New Contents";

            Assert.AreEqual("New Contents", TestLocalizationTool.GetEnglishEntry(TestTableType.ScratchAssetDrawer, key));
        }

        [Test]
        public void KeyField_ValueChangedToNewName_RenamesEntryAndUpdatesReference()
        {
            DrawerElements elements = BuildDrawerUI();
            Unlock(elements);
            Click(elements.newKeyButton);
            string oldKey = elements.keyField.value;
            elements.contentsField.value = "Some Text";

            elements.keyField.value = "Drawer.Renamed.Key";

            Assert.IsFalse(TestLocalizationTool.HasTableEntry(TestTableType.ScratchAssetDrawer, oldKey));
            Assert.IsTrue(TestLocalizationTool.HasTableEntry(TestTableType.ScratchAssetDrawer, "Drawer.Renamed.Key"));
            Assert.AreEqual("Some Text", TestLocalizationTool.GetEnglishEntry(TestTableType.ScratchAssetDrawer, "Drawer.Renamed.Key"));

            var localizedString = serializedObject.FindProperty(nameof(TestDrawerTarget.localizedField)).boxedValue as LocalizedString;
            Assert.AreEqual(TableEntryReference.Type.Id, localizedString.TableEntryReference.ReferenceType);
        }

        [Test]
        public void RenameKeyButton_Click_PopulatesKeyFieldWithANewGeneratedName()
        {
            DrawerElements elements = BuildDrawerUI();
            Unlock(elements);
            Click(elements.newKeyButton);
            string oldKey = elements.keyField.value;

            Click(elements.renameKeyButton);

            Assert.AreNotEqual(oldKey, elements.keyField.value);
            Assert.IsFalse(string.IsNullOrWhiteSpace(elements.keyField.value));
        }

        [Test]
        public void DeleteKeyButton_Click_RemovesEntryAndClearsFields()
        {
            DrawerElements elements = BuildDrawerUI();
            Unlock(elements);
            Click(elements.newKeyButton);
            string key = elements.keyField.value;

            Click(elements.deleteKeyButton);

            Assert.IsFalse(TestLocalizationTool.HasTableEntry(TestTableType.ScratchAssetDrawer, key));
            Assert.AreEqual("", elements.keyField.value);
            Assert.AreEqual("", elements.contentsField.value);
            Assert.IsFalse(elements.lockToggle.value, "Delete should re-lock the key fields");
        }
        #endregion
    }
}

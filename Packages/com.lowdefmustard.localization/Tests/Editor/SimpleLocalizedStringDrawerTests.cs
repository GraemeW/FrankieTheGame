using System.Collections.Generic;
using System.Linq;
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
        private const string _prefabFolder = "Assets/_TEMP_LocalizationDrawerPrefabTests_SafeToDelete";

        // State
        private HeadlessEditorWindowTestHelper window;
        private TestDrawerTarget target;
        private SerializedObject serializedObject;
        private readonly List<Object> createdPrefabObjects = new();
        private readonly List<string> createdAssetPaths = new();

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
            
            foreach (Object createdObject in createdPrefabObjects.Where(createdObject => createdObject != null)) { Object.DestroyImmediate(createdObject); }
            createdPrefabObjects.Clear();
            
            foreach (var assetPath in createdAssetPaths.Where(assetPath => AssetDatabase.LoadAssetAtPath<Object>(assetPath) != null)) { AssetDatabase.DeleteAsset(assetPath); }
            if (AssetDatabase.IsValidFolder(_prefabFolder)) { AssetDatabase.DeleteAsset(_prefabFolder); }
            createdAssetPaths.Clear();
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

        private (GameObject instance, TestDrawerPrefabTarget instanceComponent) CreatePrefabInstanceWithKey(string key, string contents)
        {
            var sourceGameObject = new GameObject("DrawerPrefabSource");
            var sourceComponent = sourceGameObject.AddComponent<TestDrawerPrefabTarget>();

            TestLocalizationTool.AddUpdateEnglishEntry(TestTableType.ScratchAssetDrawer, key, contents);
            LocalizedString localizedString = TestLocalizationTool.MakeLocalizedString(TestTableType.ScratchAssetDrawer, key);
            TestLocalizationTool.SafelyUpdateReference(TestTableType.ScratchAssetDrawer, localizedString, key);
            sourceComponent.localizedField = localizedString;

            if (!AssetDatabase.IsValidFolder(_prefabFolder)) { AssetDatabase.CreateFolder("Assets", "_TEMP_LocalizationDrawerPrefabTests_SafeToDelete"); }
            string prefabPath = $"{_prefabFolder}/TestDrawerPrefab_{System.Guid.NewGuid():N}.prefab";
            createdAssetPaths.Add(prefabPath);
            GameObject prefabAsset = PrefabUtility.SaveAsPrefabAsset(sourceGameObject, prefabPath);
            Object.DestroyImmediate(sourceGameObject);
            Assert.IsNotNull(prefabAsset, "Failed to save the test prefab asset");

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefabAsset);
            createdPrefabObjects.Add(instance);
            var instanceComponent = instance.GetComponent<TestDrawerPrefabTarget>();
            return (instance, instanceComponent);
        }

        private DrawerElements BuildDrawerUIFor(Object componentOrScriptableObject)
        {
            serializedObject = new SerializedObject(componentOrScriptableObject);
            var inspectorElement = new InspectorElement(serializedObject);
            window.root.Add(inspectorElement);
            return new DrawerElements(window.root);
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
            Assert.AreEqual(TableEntryReference.Type.Id, localizedString?.TableEntryReference.ReferenceType);
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

        [Test]
        public void CreatePropertyGUI_PrefabInstanceWithEmptyOverride_AutoResetsToPrefabValue()
        {
            (GameObject _, TestDrawerPrefabTarget instanceComponent) = CreatePrefabInstanceWithKey("Drawer.Prefab.AutoReset", "Prefab Value");

            // Override the instance's field to empty before the drawer ever sees it - Unity registers it as a prefab-instance property override
            var overrideSetup = new SerializedObject(instanceComponent);
            SerializedProperty overrideProperty = overrideSetup.FindProperty(nameof(TestDrawerPrefabTarget.localizedField));
            overrideProperty.boxedValue = new LocalizedString();
            overrideSetup.ApplyModifiedProperties();

            DrawerElements elements = BuildDrawerUIFor(instanceComponent);

            Assert.AreEqual("Drawer.Prefab.AutoReset", elements.keyField.value, "Expected the empty override to auto-reset back to the prefab's key on open");
            Assert.AreEqual("Prefab Value", elements.contentsField.value);
        }

        [Test]
        public void DeleteButtonState_SharedKeyWithPrefab_DisabledUntilKeyBecomesUnique()
        {
            (GameObject _, TestDrawerPrefabTarget instanceComponent) = CreatePrefabInstanceWithKey("Drawer.Prefab.Shared", "Shared Value");

            DrawerElements elements = BuildDrawerUIFor(instanceComponent);
            Unlock(elements);

            // Instance still points at the prefab source - shared, so delete stays disabled
            Assert.IsFalse(elements.deleteKeyButton.enabledInHierarchy, "Button delete should be disabled while the key is shared with the prefab source");

            Click(elements.newKeyButton);

            // Fresh key is different from the prefab's - now unique, so delete enables
            Assert.IsTrue(elements.deleteKeyButton.enabledInHierarchy, "Button delete should enable once the key differs from the prefab source");
        }
        #endregion
    }
}

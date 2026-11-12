using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using LowDefMustard.Utils.Tests;
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
        private EditorWindow window;
        private TestDrawerTarget target;
        private SerializedObject serializedObject;
        private DrawerElements builtElements;
        private readonly List<Object> createdPrefabObjects = new();
        private TempAssetFolder prefabFolder;

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
            window = HeadlessEditorWindow.CreateOffscreenWindow(400f, 400f);
            target = ScriptableObject.CreateInstance<TestDrawerTarget>();
        }

        [TearDown]
        public void TearDown()
        {
            HeadlessEditorWindow.Close(window);
            builtElements = default;
            if (target != null) { Object.DestroyImmediate(target); }
            
            foreach (Object createdObject in createdPrefabObjects.Where(createdObject => createdObject != null)) { Object.DestroyImmediate(createdObject); }
            createdPrefabObjects.Clear();
            
            prefabFolder?.Dispose();
            prefabFolder = null;
        }
        #endregion

        #region PrivateMethods
        private IEnumerator BuildDrawerUI() => BuildDrawerUIFor(target);

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

            prefabFolder ??= new TempAssetFolder("LocalizationDrawerPrefabTests");
            string prefabPath = prefabFolder.Path($"TestDrawerPrefab_{System.Guid.NewGuid():N}.prefab");
            GameObject prefabAsset = PrefabUtility.SaveAsPrefabAsset(sourceGameObject, prefabPath);
            Object.DestroyImmediate(sourceGameObject);
            Assert.IsNotNull(prefabAsset, "Failed to save the test prefab asset");

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefabAsset);
            createdPrefabObjects.Add(instance);
            var instanceComponent = instance.GetComponent<TestDrawerPrefabTarget>();
            return (instance, instanceComponent);
        }

        private IEnumerator BuildDrawerUIFor(Object componentOrScriptableObject)
        {
            // PropertyField builds the drawer on bind, which can land a few updates later
            serializedObject = new SerializedObject(componentOrScriptableObject);
            var inspectorElement = new InspectorElement(serializedObject);
            window.rootVisualElement.Add(inspectorElement);
            yield return TestWait.Until(() => new DrawerElements(window.rootVisualElement).lockToggle != null);
            yield return HeadlessEditorWindow.Settle();

            builtElements = new DrawerElements(window.rootVisualElement);
            Assert.IsNotNull(builtElements.lockToggle, "Drawer UI was never built");
        }
        #endregion

        #region Tests
        [UnityTest]
        public IEnumerator CreatePropertyGUI_AttributedField_BuildsRealDrawerUIRatherThanDefaultField()
        {
            yield return BuildDrawerUI();

            Assert.IsEmpty(window.rootVisualElement.Query<HelpBox>().ToList(), "Drawer rendered an error box instead of its normal UI");
            Assert.AreEqual(2, window.rootVisualElement.Query<TextField>().ToList().Count, "Expected exactly the key and contents text fields");
            Assert.AreEqual(3, window.rootVisualElement.Query<Button>().ToList().Count, "Expected exactly the new/rename/delete key buttons");
            Assert.AreEqual(1, window.rootVisualElement.Query<Toggle>().ToList().Count, "Expected exactly the lock toggle");
        }

        [UnityTest]
        public IEnumerator LockToggle_InitialState_KeyManagementButtonsDisabled()
        {
            yield return BuildDrawerUI();
            DrawerElements elements = builtElements;

            Assert.IsFalse(elements.lockToggle.value);
            Assert.IsFalse(elements.newKeyButton.enabledInHierarchy);
            Assert.IsFalse(elements.renameKeyButton.enabledInHierarchy);
            Assert.IsFalse(elements.deleteKeyButton.enabledInHierarchy);
        }

        [UnityTest]
        public IEnumerator LockToggle_Unlocked_EnablesNewKeyButtonButNotRenameOrDeleteWhileKeyIsEmpty()
        {
            yield return BuildDrawerUI();
            DrawerElements elements = builtElements;

            Unlock(elements);

            Assert.IsTrue(elements.newKeyButton.enabledInHierarchy);
            Assert.IsFalse(elements.renameKeyButton.enabledInHierarchy, "Rename should stay disabled until a key exists");
            Assert.IsFalse(elements.deleteKeyButton.enabledInHierarchy, "Delete should stay disabled until a key exists");
        }

        [UnityTest]
        public IEnumerator NewKeyButton_Click_CreatesEntryAndEnablesRenameAndDelete()
        {
            yield return BuildDrawerUI();
            DrawerElements elements = builtElements;
            Unlock(elements);

            Click(elements.newKeyButton);

            string generatedKey = elements.keyField.value;
            Assert.IsFalse(string.IsNullOrWhiteSpace(generatedKey), "Expected a generated key to appear in the key field");
            Assert.IsTrue(TestLocalizationTool.HasTableEntry(TestTableType.ScratchAssetDrawer, generatedKey));
            Assert.IsTrue(elements.renameKeyButton.enabledInHierarchy);
            Assert.IsTrue(elements.deleteKeyButton.enabledInHierarchy);
        }

        [UnityTest]
        public IEnumerator ContentsField_ValueChanged_UpdatesEnglishEntryForCurrentKey()
        {
            yield return BuildDrawerUI();
            DrawerElements elements = builtElements;
            Unlock(elements);
            Click(elements.newKeyButton);
            string key = elements.keyField.value;

            elements.contentsField.value = "New Contents";

            Assert.AreEqual("New Contents", TestLocalizationTool.GetEnglishEntry(TestTableType.ScratchAssetDrawer, key));
        }

        [UnityTest]
        public IEnumerator KeyField_ValueChangedToNewName_RenamesEntryAndUpdatesReference()
        {
            yield return BuildDrawerUI();
            DrawerElements elements = builtElements;
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

        [UnityTest]
        public IEnumerator RenameKeyButton_Click_PopulatesKeyFieldWithANewGeneratedName()
        {
            yield return BuildDrawerUI();
            DrawerElements elements = builtElements;
            Unlock(elements);
            Click(elements.newKeyButton);
            string oldKey = elements.keyField.value;

            Click(elements.renameKeyButton);

            Assert.AreNotEqual(oldKey, elements.keyField.value);
            Assert.IsFalse(string.IsNullOrWhiteSpace(elements.keyField.value));
        }

        [UnityTest]
        public IEnumerator DeleteKeyButton_Click_RemovesEntryAndClearsFields()
        {
            yield return BuildDrawerUI();
            DrawerElements elements = builtElements;
            Unlock(elements);
            Click(elements.newKeyButton);
            string key = elements.keyField.value;

            Click(elements.deleteKeyButton);

            Assert.IsFalse(TestLocalizationTool.HasTableEntry(TestTableType.ScratchAssetDrawer, key));
            Assert.AreEqual("", elements.keyField.value);
            Assert.AreEqual("", elements.contentsField.value);
            Assert.IsFalse(elements.lockToggle.value, "Delete should re-lock the key fields");
        }

        [UnityTest]
        public IEnumerator CreatePropertyGUI_PrefabInstanceWithEmptyOverride_AutoResetsToPrefabValue()
        {
            (GameObject _, TestDrawerPrefabTarget instanceComponent) = CreatePrefabInstanceWithKey("Drawer.Prefab.AutoReset", "Prefab Value");

            // Override the instance's field to empty before the drawer ever sees it - Unity registers it as a prefab-instance property override
            var overrideSetup = new SerializedObject(instanceComponent);
            SerializedProperty overrideProperty = overrideSetup.FindProperty(nameof(TestDrawerPrefabTarget.localizedField));
            overrideProperty.boxedValue = new LocalizedString();
            overrideSetup.ApplyModifiedProperties();

            yield return BuildDrawerUIFor(instanceComponent);
            DrawerElements elements = builtElements;

            Assert.AreEqual("Drawer.Prefab.AutoReset", elements.keyField.value, "Expected the empty override to auto-reset back to the prefab's key on open");
            Assert.AreEqual("Prefab Value", elements.contentsField.value);
        }

        [UnityTest]
        public IEnumerator DeleteButtonState_SharedKeyWithPrefab_DisabledUntilKeyBecomesUnique()
        {
            (GameObject _, TestDrawerPrefabTarget instanceComponent) = CreatePrefabInstanceWithKey("Drawer.Prefab.Shared", "Shared Value");

            yield return BuildDrawerUIFor(instanceComponent);
            DrawerElements elements = builtElements;
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

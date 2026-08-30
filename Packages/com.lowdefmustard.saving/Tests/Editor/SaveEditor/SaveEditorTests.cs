using LowDefMustard.Saving.Editor;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace LowDefMustard.Saving.Tests.Editor
{
    // Covered:
    //  - Header/selection/save-list rendering and button wiring
    //  - Load Scene Data / Apply All Data flow
    //  - Group-root parent-hierarchy filter
    // Not Covered:
    //  - OnFocus/OnSceneOpened - Unity lifecycle callbacks tied to window-focus/scene-open events
    
    public class SaveEditorTests
    {
        // Static/Const Fixed Tunables
        private const string _tempSaveFile = "_TEMP_SaveEditorTests_SafeToDelete";

        // State
        private SaveEditor window;
        private ISaveFileManagerAdapter originalAdapter;
        private TestSaveFileManagerAdapter testAdapter;
        private readonly List<GameObject> spawnedGameObjects = new();
        
        #region DataStructures
        // Marker component for SaveEditor's "skip entities nested under a group root" filtering
        private class TestSaveableGroupRoot : MonoBehaviour, ISaveableGroupRoot { }
        #endregion

        #region Setup
        [SetUp]
        public void SetUp()
        {
            originalAdapter = SaveFileManagerProvider.current;
            testAdapter = new TestSaveFileManagerAdapter();
            SaveFileManagerProvider.current = testAdapter;
        }

        [TearDown]
        public void TearDown()
        {
            if (window != null) { window.Close(); }
            SaveFileManagerProvider.current = originalAdapter;

            foreach (GameObject gameObject in spawnedGameObjects.Where(gameObject => gameObject != null))
            {
                Object.DestroyImmediate(gameObject);
            }
            spawnedGameObjects.Clear();

            string path = Path.Combine(Application.persistentDataPath, _tempSaveFile + ".sav");
            if (File.Exists(path)) { File.Delete(path); }
        }
        #endregion

        #region PrivateUtility
        private SaveEditor OpenWindow()
        {
            window = ScriptableObject.CreateInstance<SaveEditor>();
            window.ShowUtility();
            window.position = new Rect(-10000, -10000, 800, 600);
            return window;
        }

        private static void Click(Button button)
        {
            using ClickEvent clickEvent = ClickEvent.GetPooled();
            clickEvent.target = button;
            button.SendEvent(clickEvent);
        }

        private static Button FindButton(VisualElement root, string text) => root.Query<Button>().ToList().First(button => button.text == text);

        private static IEnumerable<string> AllLabelText(VisualElement root) => root.Query<Label>().ToList().Select(label => label.text);

        private (GameObject gameObject, SaveableEntity entity, TestSaveableComponent component) SpawnSaveableEntity(string name, object captureValue)
        {
            var gameObject = new GameObject(name);
            var entity = gameObject.AddComponent<SaveableEntity>();
            var component = gameObject.AddComponent<TestSaveableComponent>();
            component.captureStateReturns = captureValue;
            component.loadPriority = LoadPriority.ObjectProperty;

            spawnedGameObjects.Add(gameObject);
            return (gameObject, entity, component);
        }
        #endregion

        #region Header
        [Test]
        public void DrawSaveHeaderBox_ShowsCurrentSaveAndCharacterInfo()
        {
            testAdapter.currentSaveName = "Save1";
            testAdapter.saves["Save1"] = ("Hero", 5);

            VisualElement root = OpenWindow().rootVisualElement;

            List<string> labels = AllLabelText(root).ToList();
            CollectionAssert.Contains(labels, "Current Save:  Save1");
            CollectionAssert.Contains(labels, "Party Leader:   Hero");
            CollectionAssert.Contains(labels, "Level:   5");
        }

        [Test]
        public void DrawSaveHeaderBox_NoCurrentSave_ShowsNoSaveLabel()
        {
            testAdapter.currentSaveName = null;

            VisualElement root = OpenWindow().rootVisualElement;

            CollectionAssert.Contains(AllLabelText(root), "Current Save:  NoSave");
        }

        [Test]
        public void RenameSaveButton_Click_CopiesDeletesOriginalAndSetsCurrent()
        {
            testAdapter.currentSaveName = "Save1";
            testAdapter.saves["Save1"] = ("Hero", 5);
            VisualElement root = OpenWindow().rootVisualElement;

            Click(FindButton(root, "Rename Save"));

            Assert.IsTrue(testAdapter.saves.ContainsKey("Save1_Dupe"));
            Assert.IsFalse(testAdapter.saves.ContainsKey("Save1"));
            Assert.AreEqual("Save1_Dupe", testAdapter.currentSaveName);
        }

        [Test]
        public void DuplicateSaveButton_Click_CopiesButKeepsOriginal()
        {
            testAdapter.currentSaveName = "Save1";
            testAdapter.saves["Save1"] = ("Hero", 5);
            VisualElement root = OpenWindow().rootVisualElement;

            Click(FindButton(root, "Duplicate Save"));

            Assert.IsTrue(testAdapter.saves.ContainsKey("Save1"));
            Assert.IsTrue(testAdapter.saves.ContainsKey("Save1_Dupe"));
            Assert.AreEqual("Save1", testAdapter.currentSaveName);
        }

        [Test]
        public void DeleteSaveButton_Click_RemovesCurrentSave()
        {
            testAdapter.currentSaveName = "Save1";
            testAdapter.saves["Save1"] = ("Hero", 5);
            VisualElement root = OpenWindow().rootVisualElement;

            Click(FindButton(root, "Delete Save"));

            Assert.IsFalse(testAdapter.saves.ContainsKey("Save1"));
        }
        #endregion

        #region SaveListSelection
        [Test]
        public void DrawSaveList_ReflectsAdapterSaves()
        {
            testAdapter.saves["Alpha"] = ("A", 1);
            testAdapter.saves["Beta"] = ("B", 2);

            VisualElement root = OpenWindow().rootVisualElement;

            ListView listView = root.Query<ListView>().First();
            CollectionAssert.AreEquivalent(new[] { "Alpha", "Beta" }, (IEnumerable<string>)listView.itemsSource);
        }

        [Test]
        public void SelectingSave_UpdatesSelectionHeader()
        {
            testAdapter.saves["Alpha"] = ("A", 1);
            VisualElement root = OpenWindow().rootVisualElement;
            ListView listView = root.Query<ListView>().First();

            listView.SetSelection(0);

            List<string> labels = AllLabelText(root).ToList();
            CollectionAssert.Contains(labels, "Selected Save:  Alpha");
            CollectionAssert.Contains(labels, "Party Leader:   A");
            CollectionAssert.Contains(labels, "Level:   1");
        }

        [Test]
        public void SetToCurrentButton_Click_SetsSelectedSaveAsCurrent()
        {
            testAdapter.saves["Alpha"] = ("A", 1);
            VisualElement root = OpenWindow().rootVisualElement;
            root.Query<ListView>().First().SetSelection(0);

            Click(FindButton(root, "Set To Current"));

            Assert.AreEqual("Alpha", testAdapter.currentSaveName);
        }

        [Test]
        public void CopyToNextOpenButton_Click_CopiesToFirstAvailableSlot()
        {
            testAdapter.saves["Alpha"] = ("A", 1);
            VisualElement root = OpenWindow().rootVisualElement;
            root.Query<ListView>().First().SetSelection(0);

            Click(FindButton(root, "Copy To Next Open"));

            Assert.IsTrue(testAdapter.saves.ContainsKey("Save0"));
            Assert.AreEqual(("A", 1), testAdapter.saves["Save0"]);
        }

        [Test]
        public void DeleteSelectedButton_Click_RemovesSelectedSave()
        {
            testAdapter.saves["Alpha"] = ("A", 1);
            VisualElement root = OpenWindow().rootVisualElement;
            root.Query<ListView>().First().SetSelection(0);

            Click(FindButton(root, "Delete Selected"));

            Assert.IsFalse(testAdapter.saves.ContainsKey("Alpha"));
        }
        #endregion

        #region LoadSceneData
        [Test]
        public void LoadSceneDataButton_NoCurrentSave_LogsWarningAndStaysUnloaded()
        {
            testAdapter.currentSaveName = null;
            VisualElement root = OpenWindow().rootVisualElement;

            LogAssert.Expect(LogType.Warning, "Save file not found.");
            Click(FindButton(root, "Load Scene Data"));

            CollectionAssert.Contains(AllLabelText(root), "Status:  Unloaded");
        }

        [Test]
        public void LoadSceneDataButton_ValidSave_PopulatesEntityCardsAndStatusLoaded()
        {
            (_, SaveableEntity entity, _) = SpawnSaveableEntity("SavedEntity", "value1");
            entity.TryCaptureState(null, out JToken capturedState);
            SavingSystem.ManualSave(_tempSaveFile, new JObject { [entity.GetUniqueIdentifier()] = capturedState });

            testAdapter.currentSaveName = _tempSaveFile;
            testAdapter.saves[_tempSaveFile] = ("Hero", 3);
            VisualElement root = OpenWindow().rootVisualElement;

            Click(FindButton(root, "Load Scene Data"));

            CollectionAssert.Contains(AllLabelText(root), "Status:  Loaded");
            CollectionAssert.Contains(AllLabelText(root), "GameObject:  SavedEntity");
            Assert.IsTrue(FindButton(root, "Apply All Data").enabledSelf);
        }

        [Test]
        public void LoadSceneDataButton_EntityUnderGroupRoot_IsExcluded()
        {
            var groupRootGameObject = new GameObject("GroupRoot");
            groupRootGameObject.AddComponent<TestSaveableGroupRoot>();
            spawnedGameObjects.Add(groupRootGameObject);

            var nestedGameObject = new GameObject("NestedEntity");
            nestedGameObject.transform.SetParent(groupRootGameObject.transform);
            var nestedEntity = nestedGameObject.AddComponent<SaveableEntity>();
            nestedGameObject.AddComponent<TestSaveableComponent>().captureStateReturns = "nested";
            spawnedGameObjects.Add(nestedGameObject);

            (_, SaveableEntity standaloneEntity, _) = SpawnSaveableEntity("StandaloneEntity", "standalone");

            var fullState = new JObject();
            nestedEntity.TryCaptureState(null, out JToken nestedState);
            fullState[nestedEntity.GetUniqueIdentifier()] = nestedState;
            standaloneEntity.TryCaptureState(null, out JToken standaloneState);
            fullState[standaloneEntity.GetUniqueIdentifier()] = standaloneState;
            SavingSystem.ManualSave(_tempSaveFile, fullState);

            testAdapter.currentSaveName = _tempSaveFile;
            testAdapter.saves[_tempSaveFile] = ("Hero", 1);
            VisualElement root = OpenWindow().rootVisualElement;

            Click(FindButton(root, "Load Scene Data"));

            var labels = AllLabelText(root).ToList();
            CollectionAssert.Contains(labels, "GameObject:  StandaloneEntity");
            CollectionAssert.DoesNotContain(labels, "GameObject:  GroupRoot/NestedEntity");
        }
        #endregion

        #region ApplyAllData
        [Test]
        public void ApplyAllDataButton_Click_WritesStateWithoutError()
        {
            (_, SaveableEntity entity, _) = SpawnSaveableEntity("SavedEntity", "value1");
            entity.TryCaptureState(null, out JToken capturedState);
            SavingSystem.ManualSave(_tempSaveFile, new JObject { [entity.GetUniqueIdentifier()] = capturedState });

            testAdapter.currentSaveName = _tempSaveFile;
            testAdapter.saves[_tempSaveFile] = ("Hero", 3);
            VisualElement root = OpenWindow().rootVisualElement;
            Click(FindButton(root, "Load Scene Data"));

            Assert.DoesNotThrow(() => Click(FindButton(root, "Apply All Data")));

            JObject savedState = SavingSystem.ManualGetFullState(_tempSaveFile);
            Assert.IsTrue(savedState.ContainsKey(entity.GetUniqueIdentifier()));
        }
        #endregion

        #region LifecycleCallbacks
        [Test]
        public void OnFocus_LoadedButCachedStateNull_UnloadsControlData()
        {
            SaveEditor openedWindow = OpenWindow();
            openedWindow.saveControlBoxLoaded = true;
            openedWindow.cachedFullSaveState = null; // the specific mismatch OnFocus guards against

            openedWindow.OnFocus();

            Assert.IsFalse(openedWindow.saveControlBoxLoaded);
            CollectionAssert.Contains(AllLabelText(openedWindow.rootVisualElement), "Status:  Unloaded");
        }

        [Test]
        public void OnFocus_NormalLoadedState_DoesNotUnload()
        {
            (_, SaveableEntity entity, _) = SpawnSaveableEntity("SavedEntity", "value1");
            entity.TryCaptureState(null, out JToken capturedState);
            SavingSystem.ManualSave(_tempSaveFile, new JObject { [entity.GetUniqueIdentifier()] = capturedState });

            testAdapter.currentSaveName = _tempSaveFile;
            testAdapter.saves[_tempSaveFile] = ("Hero", 3);
            var openedWindow = OpenWindow();
            Click(FindButton(openedWindow.rootVisualElement, "Load Scene Data"));

            openedWindow.OnFocus();

            Assert.IsTrue(openedWindow.saveControlBoxLoaded);
            CollectionAssert.Contains(AllLabelText(openedWindow.rootVisualElement), "Status:  Loaded");
        }

        [Test]
        public void OnSceneOpened_UnloadsControlData()
        {
            (_, SaveableEntity entity, _) = SpawnSaveableEntity("SavedEntity", "value1");
            entity.TryCaptureState(null, out JToken capturedState);
            SavingSystem.ManualSave(_tempSaveFile, new JObject { [entity.GetUniqueIdentifier()] = capturedState });

            testAdapter.currentSaveName = _tempSaveFile;
            testAdapter.saves[_tempSaveFile] = ("Hero", 3);
            var openedWindow = OpenWindow();
            Click(FindButton(openedWindow.rootVisualElement, "Load Scene Data"));
            Assert.IsTrue(openedWindow.saveControlBoxLoaded); // precondition - confirms it was actually loaded first

            openedWindow.OnSceneOpened(default, OpenSceneMode.Single);

            Assert.IsFalse(openedWindow.saveControlBoxLoaded);
            CollectionAssert.Contains(AllLabelText(openedWindow.rootVisualElement), "Status:  Unloaded");
        }
        #endregion
    }
}

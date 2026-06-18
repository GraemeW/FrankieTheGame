using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;
using Frankie.Core;
using Frankie.Stats;

namespace Frankie.Saving.Editor
{
    public class SaveEditor : EditorWindow
    {
        // Const
        private const string _noSaveLabel = "NoSave";
        private const int _maxSaves = 100;
        private const string _controlBoxStatusUnloaded = "Unloaded";
        private const string _controlBoxStatusLoaded = "Loaded";
        
        // State
        private string newSave;
        private string selectedSave;
        private bool saveControlBoxLoaded = false;
        private List<SaveableEntity> cachedSaveableEntities = null;
        
        // UI Cached References
        private Box saveHeaderBox;
        private Box selectionHeaderBox;
        private ListView saveEntries;
        private Box saveControlBox;
        private Box saveControlHeaderBox;
        private ScrollView saveControlEntityScrollView;
        
        #region UnityMethods
        [MenuItem("Tools/Save Editor", false, 205)]
        private static void ShowWindow()
        {
            var window = GetWindow<SaveEditor>("Save Editor");
            window.Show();
        }

        private void OnEnable()
        {
            SubscribeListeners(true);
        }

        private void OnDisable()
        {
            SubscribeListeners(false);
        }

        private void SubscribeListeners(bool enable)
        {
            SavingWrapper.gameListUpdated -= ReDrawUI;
            EditorSceneManager.sceneOpened -= OnSceneOpened;
            if (saveEntries != null) { saveEntries.selectionChanged -= OnSaveSelectionChanged;  }
            if (!enable) { return; }
            
            SavingWrapper.gameListUpdated += ReDrawUI;
            EditorSceneManager.sceneOpened += OnSceneOpened;
            if (saveEntries != null) { saveEntries.selectionChanged += OnSaveSelectionChanged;  }
        }
        #endregion

        #region CreateUIElements
        private void CreateGUI()
        {
            var splitView = new TwoPaneSplitView(0, 150, TwoPaneSplitViewOrientation.Vertical);
            rootVisualElement.Add(splitView);

            Box saveHeader = CreateSaveHeaderBox();
            splitView.Add(saveHeader);

            VisualElement saveLoaderPanel = CreateSaveLoaderPanel();
            splitView.Add(saveLoaderPanel);

            ReDrawUI();
        }

        private Box CreateSaveHeaderBox()
        {
            saveHeaderBox = new Box();
            DrawSaveHeaderBox();
            return saveHeaderBox;
        }

        private VisualElement CreateSaveLoaderPanel()
        {
            var saveLoaderPanel = new VisualElement();
            var splitView = new TwoPaneSplitView(0, 200, TwoPaneSplitViewOrientation.Horizontal);
            saveLoaderPanel.Add(splitView);
            
            VisualElement saveEntryBox = CreateSaveSelectionPanel();
            splitView.Add(saveEntryBox);

            saveControlBox = CreateSaveControlBox();
            splitView.Add(saveControlBox);
            
            return saveLoaderPanel;
        }

        private VisualElement CreateSaveSelectionPanel()
        {
            var saveSelectionPanel = new VisualElement();
            var splitView = new TwoPaneSplitView(0, 110, TwoPaneSplitViewOrientation.Vertical);
            saveSelectionPanel.Add(splitView);
            
            selectionHeaderBox = new Box();
            splitView.Add(selectionHeaderBox);
            DrawSelectionHeaderBox();
            
            if (saveEntries != null)
            {
                saveEntries.selectionChanged -= OnSaveSelectionChanged;
                saveEntries = null;
            }
            saveEntries = new ListView { selectionType = SelectionType.Single };
            saveEntries.selectionChanged += OnSaveSelectionChanged;
            splitView.Add(saveEntries);
            
            return saveSelectionPanel;
        }

        private Box CreateSaveControlBox()
        {
            saveControlBox = new Box();
            
            var splitView = new TwoPaneSplitView(0, 110, TwoPaneSplitViewOrientation.Vertical);
            saveControlBox.Add(splitView);

            saveControlHeaderBox = new Box();
            splitView.Add(saveControlHeaderBox);
            DrawSaveControlHeaderBox();

            saveControlEntityScrollView = new ScrollView(ScrollViewMode.Vertical);
            splitView.Add(saveControlEntityScrollView);
            DrawSaveControlEntityList();
            
            return saveControlBox;
        }
        #endregion
        
        #region DrawUIElements
        private void ReDrawUI()
        {
            DrawSaveHeaderBox();
            DrawSelectionHeaderBox();
            DrawSaveList();
            DrawSaveControlHeaderBox();
            DrawSaveControlEntityList();
        }

        private void DrawSaveHeaderBox()
        {
            if (saveHeaderBox == null) { return; }
            saveHeaderBox.Clear();
            
            saveHeaderBox.Add(new Label("Save Editor Tool"));
            
            string currentSaveName = SavingWrapper.GetCurrentSaveName() ?? _noSaveLabel;
            saveHeaderBox.Add(new Label($"Current Save:  {currentSaveName}"));
            
            if (SavingWrapper.GetInfoFromName(currentSaveName, out string characterName, out int level))
            {
                saveHeaderBox.Add(new Label($"Party Leader:   {characterName}"));
                saveHeaderBox.Add(new Label($"Level:   {level}"));
            }

            newSave = $"{currentSaveName}_Dupe";
            var saveNameField = new TextField
            {
                label = "Name for Rename/Dupe",
                value = newSave,
                style = { width = 250 }
            };
            saveNameField.RegisterValueChangedCallback(x => newSave = x.newValue);
            saveHeaderBox.Add(saveNameField);

            var buttonStack = new VisualElement { style = { width = 150 } };
            saveHeaderBox.Add(buttonStack);
            
            var renameSave = new Button { text = "Rename Save" };
            renameSave.RegisterCallback<ClickEvent>(RenameCurrentSave);
            buttonStack.Add(renameSave);
            
            var duplicateSave = new Button { text = "Duplicate Save" };
            duplicateSave.RegisterCallback<ClickEvent>(DuplicateCurrentSave);
            buttonStack.Add(duplicateSave);
            
            var deleteSave = new Button { text = "Delete Save" };
            deleteSave.RegisterCallback<ClickEvent>(DeleteCurrentSave);
            buttonStack.Add(deleteSave);
        }

        private void DrawSelectionHeaderBox()
        {
            if (selectionHeaderBox == null) { return; }
            selectionHeaderBox.Clear();
            
            string selectedSaveLabel = selectedSave ?? _noSaveLabel;
            selectionHeaderBox.Add(new Label($"Selected Save:  {selectedSaveLabel}"));

            if (!string.IsNullOrWhiteSpace(selectedSave) && SavingWrapper.HasSave(selectedSave))
            {
                if (SavingWrapper.GetInfoFromName(selectedSave, out string characterName, out int level))
                {
                    selectionHeaderBox.Add(new Label($"Party Leader:   {characterName}"));
                    selectionHeaderBox.Add(new Label($"Level:   {level}"));
                }
            }
            
            var spacer = new VisualElement { style = { height = 20 } };
            selectionHeaderBox.Add(spacer);
            
            var setSelectedToCurrent = new Button { text = "Set To Current" };
            setSelectedToCurrent.RegisterCallback<ClickEvent>(SetSelectedSaveToCurrent);
            selectionHeaderBox.Add(setSelectedToCurrent);
            
            var copySelectedToNextOpen = new Button { text = "Copy To Next Open" };
            copySelectedToNextOpen.RegisterCallback<ClickEvent>(CopySelectedSaveToNextOpen);
            selectionHeaderBox.Add(copySelectedToNextOpen);
            
            var deleteSelected = new Button { text = "Delete Selected" };
            deleteSelected.RegisterCallback<ClickEvent>(DeleteSelectedSave);
            selectionHeaderBox.Add(deleteSelected);
        }
        
        private void DrawSaveList()
        {
            if (saveEntries == null) { return; }
            saveEntries.Clear();
            
            List<string> saveList = SavingWrapper.ListSaves(false).ToList();
            saveEntries.makeItem = () => new Label();
            saveEntries.bindItem = (item, index) =>
            {
                if (item is Label label && index < saveList.Count) { label.text = saveList[index]; }
            };
            saveEntries.itemsSource = saveList;
        }
        
        private void DrawSaveControlHeaderBox()
        {
            if (saveControlHeaderBox == null) { return; }
            saveControlHeaderBox.Clear();

            string currentSaveName = SavingWrapper.GetCurrentSaveName() ?? _noSaveLabel;
            saveControlHeaderBox.Add(new Label($"Save:  {currentSaveName}"));

            string currentSceneName = SceneManager.GetActiveScene().name;
            saveControlHeaderBox.Add(new Label($"Scene:  {currentSceneName}"));

            string statusLabel = saveControlBoxLoaded ? _controlBoxStatusLoaded : _controlBoxStatusUnloaded;
            saveControlHeaderBox.Add(new Label($"Status:  {statusLabel}"));

            var buttonStack = new VisualElement { style = { width = 150 } };
            saveControlHeaderBox.Add(buttonStack);

            var loadDataButton = new Button { text = "Load Scene Data" };
            loadDataButton.RegisterCallback<ClickEvent>(LoadSaveControlData);
            buttonStack.Add(loadDataButton);

            var applyDataButton = new Button { text = "Apply All Data" };
            applyDataButton.SetEnabled(false);
            buttonStack.Add(applyDataButton);
        }

        private void DrawSaveControlEntityList()
        {
            if (saveControlEntityScrollView == null) { return; }
            saveControlEntityScrollView.Clear();

            if (!saveControlBoxLoaded || cachedSaveableEntities == null) { return; }

            foreach (SaveableEntity saveableEntity in cachedSaveableEntities)
            {
                Box entityCard = CreateSaveableEntityCard(saveableEntity);
                saveControlEntityScrollView.Add(entityCard);
            }
        }
        
        private Box CreateSaveableEntityCard(SaveableEntity saveableEntity)
        {
            var entityCard = new Box { style = { marginBottom = 4 } };
            
            var entitySubHeader = new Box();
            entityCard.Add(entitySubHeader);

            entitySubHeader.Add(new Label($"GameObject:  {saveableEntity.gameObject.name}"));
            entitySubHeader.Add(new Label($"ID:  {saveableEntity.GetUniqueIdentifier()}"));

            var saveEntityButton = new Button { text = "Save Entity" };
            saveEntityButton.SetEnabled(false);
            entitySubHeader.Add(saveEntityButton);
            
            foreach (ISaveable saveable in saveableEntity.GetSaveableComponents())
            {
                Box saveableSubCard = CreateISaveableSubCard(saveable);
                entityCard.Add(saveableSubCard);
            }
            return entityCard;
        }

        private Box CreateISaveableSubCard(ISaveable saveable)
        {
            var saveableSubCard = new Box { style = { marginTop = 2, marginLeft = 8 } };
            string typeName = saveable.GetType().Name;
            saveableSubCard.Add(new Label($"Component:  {typeName}"));

            // TODO:  Add editable properties per ISaveable type

            return saveableSubCard;
        }
        #endregion
        
        #region UtilityMethods
        private void RenameCurrentSave(ClickEvent clickEvent)
        {
            if (string.IsNullOrWhiteSpace(newSave)) { return; }
            
            SavingWrapper.CopySave(newSave, false);
            SavingWrapper.Delete(false);
            SavingWrapper.SetCurrentSave(newSave, true);
        }

        private void DuplicateCurrentSave(ClickEvent clickEvent)
        {
            if (string.IsNullOrWhiteSpace(newSave)) { return; }
            
            SavingWrapper.CopySave(newSave);
        }

        private void DeleteCurrentSave(ClickEvent clickEvent)
        {
            SavingWrapper.Delete();
        }
        
        private void SetSelectedSaveToCurrent(ClickEvent clickEvent)
        {
            if (string.IsNullOrWhiteSpace(selectedSave) || !SavingWrapper.HasSave(selectedSave)) { return; }
            
            SavingWrapper.SetCurrentSave(selectedSave);
        }

        private void CopySelectedSaveToNextOpen(ClickEvent clickEvent)
        {
            if (string.IsNullOrWhiteSpace(selectedSave) || !SavingWrapper.HasSave(selectedSave)) { return; }
            
            for (int index = 0; index < _maxSaves; index++)
            {
                string trySave = SavingWrapper.GetSaveNameForIndex(index);
                if (SavingWrapper.HasSave(trySave)) { continue; }

                SavingWrapper.CopySave(selectedSave, trySave);
                break;
            }
        }

        private void DeleteSelectedSave(ClickEvent clickEvent)
        {
            if (string.IsNullOrWhiteSpace(selectedSave) || !SavingWrapper.HasSave(selectedSave)) { return; }
            
            SavingWrapper.Delete(selectedSave);
        }
        
        private void LoadSaveControlData(ClickEvent clickEvent)
        {
            cachedSaveableEntities = SavingSystem.GetAllSaveableEntities().OrderBy(GetEntitySortPriority).ThenBy(saveableEntity => saveableEntity.GetUniqueIdentifier()).ToList();
            saveControlBoxLoaded = true;
            DrawSaveControlHeaderBox();
            DrawSaveControlEntityList();
        }
        
        private void UnloadSaveControlData()
        {
            cachedSaveableEntities = null;
            saveControlBoxLoaded = false;
            DrawSaveControlHeaderBox();
            DrawSaveControlEntityList();
        }
        
        private static int GetEntitySortPriority(SaveableEntity saveableEntity)
        {
            GameObject go = saveableEntity.gameObject;
            if (go.GetComponent<Player>() != null) { return 0; }
            if (HasPlayerInParentHierarchy(go.transform.parent)) { return 1; }
            if (go.GetComponent<BaseStats>() != null) { return 2; }
            return 3;
        }
        
        private static bool HasPlayerInParentHierarchy(Transform parent)
        {
            while (parent != null)
            {
                if (parent.GetComponent<Player>() != null) { return true; }
                parent = parent.parent;
            }
            return false;
        }
        #endregion
        
        #region EventHandlers
        private void OnSaveSelectionChanged(IEnumerable<object> selectedItems)
        {
            foreach (var selectedItem in selectedItems)
            {
                if (selectedItem is not string tentativeSelectedSave) { continue; }
                selectedSave = tentativeSelectedSave;
                ReDrawUI();
                return;
            }
        }
        
        private void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            UnloadSaveControlData();
        }
        #endregion
    }
}

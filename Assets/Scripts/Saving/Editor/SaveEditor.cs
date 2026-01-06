using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;
using Frankie.Core;

namespace Frankie.Saving.Editor
{
    public class SaveEditor : EditorWindow
    {
        // Const
        private const string _noSaveLabel = "NoSave";
        private const int _maxSaves = 100;
        
        // State
        private string newSave;
        private string selectedSave;
        
        // UI Cached References
        private Box saveHeaderBox;
        private Box selectionHeaderBox;
        private ListView saveEntries;
        private Box saveControlBox;
        
        #region UnityCore
        [MenuItem("Tools/Save Editor")]
        private static void ShowWindow()
        {
            var window = GetWindow<SaveEditor>("Save Editor");
            window.Show();
        }

        private void OnEnable()
        {
            SavingWrapper.gameListUpdated += ReDrawUI;
            if (saveEntries != null) { saveEntries.selectionChanged += OnSaveSelectionChanged; }
        }

        private void OnDisable()
        {
            SavingWrapper.gameListUpdated -= ReDrawUI;
            if (saveEntries != null) { saveEntries.selectionChanged -= OnSaveSelectionChanged; }
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
            var splitView = new TwoPaneSplitView(0, 80, TwoPaneSplitViewOrientation.Vertical);
            saveSelectionPanel.Add(splitView);
            
            selectionHeaderBox = new Box();
            splitView.Add(selectionHeaderBox);
            
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
            DrawSaveControlBox();
            
            return saveControlBox;
        }
        #endregion
        
        #region DrawUIElements
        private void ReDrawUI()
        {
            DrawSaveHeaderBox();
            DrawSelectionHeaderBox();
            DrawSaveList();
            DrawSaveControlBox();
        }

        private void DrawSaveHeaderBox()
        {
            if (saveHeaderBox == null) { return; }
            saveHeaderBox.Clear();
            
            saveHeaderBox.Add(new Label("Save Editor Tool"));
            
            string currentSaveName = SavingWrapper.GetCurrentSave() ?? _noSaveLabel;
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

        private void DrawSaveControlBox()
        {
            if (saveControlBox == null) { return; }
            saveControlBox.Clear();
            
            if (string.IsNullOrWhiteSpace(selectedSave) || !SavingWrapper.HasSave(selectedSave)) { return; }

            if (SavingWrapper.GetInfoFromName(selectedSave, out string characterName, out int level))
            {
                saveControlBox.Add(new Label($"Party Leader:   {characterName}"));
                saveControlBox.Add(new Label($"Level:   {level}"));
            }
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
        #endregion
    }
}

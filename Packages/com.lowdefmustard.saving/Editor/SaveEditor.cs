using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace LowDefMustard.Saving.Editor
{
    public class SaveEditor : EditorWindow
    {
        // Const
        private const string _noSaveLabel = "NoSave";
        private const int _maxSaves = 100;
        private const string _controlBoxStatusUnloaded = "Unloaded";
        private const string _controlBoxStatusLoaded = "Loaded";
        private static readonly Color _controlBoxStatusUnloadedColor = Color.softRed;
        private static readonly Color _controlBoxStatusLoadedColor = Color.lightGreen;
        private static readonly Color _applyAllDataButtonColor = Color.softRed;

        private const float _smallButtonWidth = 100f;
        private const float _standardButtonWidth = 175f;
        private const float _largeButtonWidth = 250f;
        private const float _entityCardSpacerHeight = 10f;
        
        // State
        private string newSave;
        private string selectedSave;
        private readonly List<SaveableEntityCardData> cachedSaveableEntityCardData = new();
        private readonly HashSet<string> saveableEntityGUIDs = new();
        // Note:  internal for below for test visibility (as needed for OnFocus verification)
        internal bool saveControlBoxLoaded = false;
        internal JObject cachedFullSaveState;
        
        // Static Hooks
        public static Func<SceneSelectorContext, VisualElement> SceneSelectorFactory;
        
        // UI Cached References
        private Box saveHeaderBox;
        private Box selectionHeaderBox;
        private ListView saveEntries;
        private Box saveControlBox;
        private Box saveControlHeaderBox;
        private Box sceneSelectBox;
        private ScrollView saveControlEntityScrollView;
        
        #region UnityMethods
        [MenuItem("Tools/Save Editor", false, 305)]
        private static void ShowWindow()
        {
            var window = GetWindow<SaveEditor>("Save Editor");
            window.Show();
        }
        
        // Internal for test visibility (window-focus events aren't otherwise triggerable from EditMode tests practically)
        internal void OnFocus()
        {
            if (saveControlBoxLoaded && cachedFullSaveState == null) { UnloadSaveControlData(); }
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
            SaveFileManagerProvider.current.gameListUpdated -= ReDrawUI;
            EditorSceneManager.sceneOpened -= OnSceneOpened;
            if (saveEntries != null) { saveEntries.selectionChanged -= OnSaveSelectionChanged;  }
            if (!enable) { return; }
            
            SaveFileManagerProvider.current.gameListUpdated += ReDrawUI;
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
            
            var splitView = new TwoPaneSplitView(0, 160, TwoPaneSplitViewOrientation.Vertical);
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
            DrawSceneSelectBox();
            DrawSaveControlEntityList();
        }

        private void DrawSaveHeaderBox()
        {
            if (saveHeaderBox == null) { return; }
            saveHeaderBox.Clear();
            
            saveHeaderBox.Add(MakeTitleLabel("Save Editor Tool"));
            
            string currentSaveName = SaveFileManagerProvider.current.GetCurrentSaveName() ?? _noSaveLabel;
            saveHeaderBox.Add(new Label($"Current Save:  {currentSaveName}"));
            
            if (SaveFileManagerProvider.current.GetInfoFromSave(currentSaveName, out string characterName, out int level))
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

            var buttonStack = new VisualElement { style = { width = _standardButtonWidth } };
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

            if (!string.IsNullOrWhiteSpace(selectedSave) && SaveFileManagerProvider.current.HasSave(selectedSave))
            {
                if (SaveFileManagerProvider.current.GetInfoFromSave(selectedSave, out string characterName, out int level))
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
            
            List<string> saveList = SaveFileManagerProvider.current.ListSaves(false).ToList();
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

            string currentSaveName = SaveFileManagerProvider.current.GetCurrentSaveName() ?? _noSaveLabel;
            saveControlHeaderBox.Add(new Label($"Save:  {currentSaveName}"));

            string currentSceneName = SceneManager.GetActiveScene().name;
            saveControlHeaderBox.Add(new Label($"Scene:  {currentSceneName}"));

            string statusLabel = saveControlBoxLoaded ? _controlBoxStatusLoaded : _controlBoxStatusUnloaded;
            saveControlHeaderBox.Add(new Label($"Status:  {statusLabel}")
            {
                style = { color = saveControlBoxLoaded ? _controlBoxStatusLoadedColor : _controlBoxStatusUnloadedColor }
            });

            var buttonStack = new VisualElement { style = { width = _largeButtonWidth } };
            saveControlHeaderBox.Add(buttonStack);

            var loadDataButton = new Button { text = "Load Scene Data", style = { width = _standardButtonWidth } };
            loadDataButton.RegisterCallback<ClickEvent>(LoadSaveControlData);
            buttonStack.Add(loadDataButton);

            var applyDataButton = new Button { text = "Apply All Data", style = { width = _standardButtonWidth, backgroundColor = _applyAllDataButtonColor, color = Color.white } };
            applyDataButton.SetEnabled(cachedFullSaveState != null);
            applyDataButton.RegisterCallback<ClickEvent>(ApplyAllSaveableEntityData);
            buttonStack.Add(applyDataButton);
            
            var spacer = new VisualElement { style = { height = _entityCardSpacerHeight } };
            saveControlHeaderBox.Add(spacer);
            
            
            sceneSelectBox = new Box();
            saveControlHeaderBox.Add(sceneSelectBox);
            DrawSceneSelectBox();
        }

        private void DrawSceneSelectBox()
        {
            if (sceneSelectBox == null) { return; }
            sceneSelectBox.Clear();
            sceneSelectBox.Add(new Label("Last Saved Scene:"));

            var context = new SceneSelectorContext(cachedFullSaveState,
                onSceneDataChanged: () => SavingSystem.ManualSave(SaveFileManagerProvider.current.GetCurrentSaveName(), cachedFullSaveState),
                onReloadRequested: () => LoadSaveControlData(null));

            VisualElement selector = SceneSelectorFactory?.Invoke(context) ?? CreateDefaultSceneSelector(context);
            sceneSelectBox.Add(selector);
        }

        private static VisualElement CreateDefaultSceneSelector(SceneSelectorContext context)
        {
            string currentLastScene = SavingSystem.ManualGetLastScene(context.SaveState);
            return new TextField { value = currentLastScene, isReadOnly = true, style = { width = _largeButtonWidth } };
        }

        private void DrawSaveControlEntityList()
        {
            if (saveControlEntityScrollView == null) { return; }
            saveControlEntityScrollView.Clear();
            
            if (!saveControlBoxLoaded || cachedSaveableEntityCardData == null) { return; }

            foreach (SaveableEntityCardData saveableEntityCardData in cachedSaveableEntityCardData)
            {
                Box entityCardView = saveableEntityCardData.DrawSaveableEntityCard(() => saveableEntityCardData.SaveSaveableEntity(true, SetLastSceneToCurrent));
                saveControlEntityScrollView.Add(entityCardView);
                saveableEntityCardData.SetSelectCallback(() => ScrollToTopEdge(saveControlEntityScrollView, entityCardView));
                
                var spacer = new VisualElement { style = { height = _entityCardSpacerHeight } };
                saveControlEntityScrollView.Add(spacer);
            }
        }
        
        private static Label MakeTitleLabel(string title)
        {
            return new Label(title)
            {
                style =
                {
                    fontSize = 14,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    marginBottom = 8
                }
            };
        }
        #endregion
        
        #region SaveFileUtility
        private void RenameCurrentSave(ClickEvent clickEvent)
        {
            if (string.IsNullOrWhiteSpace(newSave)) { return; }
            
            SaveFileManagerProvider.current.CopySave(newSave, false);
            SaveFileManagerProvider.current.Delete(false);
            SaveFileManagerProvider.current.SetCurrentSave(newSave, true);
        }

        private void DuplicateCurrentSave(ClickEvent clickEvent)
        {
            if (string.IsNullOrWhiteSpace(newSave)) { return; }
            
            SaveFileManagerProvider.current.CopySave(newSave);
        }

        private void DeleteCurrentSave(ClickEvent clickEvent)
        {
            SaveFileManagerProvider.current.Delete();
        }
        
        private void SetSelectedSaveToCurrent(ClickEvent clickEvent)
        {
            if (string.IsNullOrWhiteSpace(selectedSave) || !SaveFileManagerProvider.current.HasSave(selectedSave)) { return; }
            
            SaveFileManagerProvider.current.SetCurrentSave(selectedSave);
            UnloadSaveControlData();
        }

        private void CopySelectedSaveToNextOpen(ClickEvent clickEvent)
        {
            if (string.IsNullOrWhiteSpace(selectedSave) || !SaveFileManagerProvider.current.HasSave(selectedSave)) { return; }
            
            for (int index = 0; index < _maxSaves; index++)
            {
                string trySave = SaveFileManagerProvider.current.GetSaveNameForIndex(index);
                if (SaveFileManagerProvider.current.HasSave(trySave)) { continue; }

                SaveFileManagerProvider.current.CopySave(selectedSave, trySave);
                break;
            }
        }

        private void DeleteSelectedSave(ClickEvent clickEvent)
        {
            if (string.IsNullOrWhiteSpace(selectedSave) || !SaveFileManagerProvider.current.HasSave(selectedSave)) { return; }
            
            SaveFileManagerProvider.current.Delete(selectedSave);
        }
        #endregion
        
        #region EditSaveUtility
        private static bool HasPlayerInParentHierarchy(Transform parent)
        {
            while (parent != null)
            {
                if (parent.GetComponent<ISaveableGroupRoot>() != null) { return true; }
                parent = parent.parent;
            }
            return false;
        }
        
        private void SetLastSceneToCurrent()
        {
            string sceneName = SceneManager.GetActiveScene().name;
            if (string.IsNullOrEmpty(sceneName)) { return; }
            if (sceneName == SavingSystem.ManualGetLastScene(cachedFullSaveState)) { return; }
            
            Debug.Log($"Saved last scene updated to {sceneName}.");
            SavingSystem.ManualUpdateLastScene(cachedFullSaveState, sceneName);
            SavingSystem.ManualSave(SaveFileManagerProvider.current.GetCurrentSaveName(), cachedFullSaveState);
            
            DrawSceneSelectBox();
        }
        
        private void LoadSaveControlData(ClickEvent _)
        {
            string currentSave = SaveFileManagerProvider.current.GetCurrentSaveName();
            if (string.IsNullOrEmpty(currentSave) || !SaveFileManagerProvider.current.HasSave(currentSave))
            {
                Debug.LogWarning($"Save file not found.");
                return;
            }
            
            cachedFullSaveState = SavingSystem.ManualGetFullState(currentSave);
            if (cachedFullSaveState == null)
            {
                Debug.LogWarning($"Save file malformed.");
                return;
            }
            
            cachedSaveableEntityCardData.Clear();
            saveableEntityGUIDs.Clear();
            foreach (SaveableEntity saveableEntity in SavingSystem.GetValidSaveableEntities().OrderBy(SaveableEntityCardData.GetEntitySortPriority).ThenBy(saveableEntity => saveableEntity.name).ToList())
            {
                if (saveableEntity == null) { continue; }
                if (HasPlayerInParentHierarchy(saveableEntity.transform.parent)) { continue; } // Avoid re-pulling entries e.g. in party container
                if (saveableEntityGUIDs.Contains(saveableEntity.GetUniqueIdentifier())) { continue; } // Avoid re-drawing dupe elements
                
                var saveableEntityCardData = new SaveableEntityCardData(saveableEntity, cachedFullSaveState, saveableEntityGUIDs, DrawSaveControlEntityList, SaveFileManagerProvider.current.GetCurrentSaveName);
                saveableEntityCardData.SelfReferenceInSubCards();
                cachedSaveableEntityCardData.Add(saveableEntityCardData);
            }
            saveControlBoxLoaded = true;
            DrawSaveControlHeaderBox();
            DrawSceneSelectBox();
            DrawSaveControlEntityList();
        }
        
        private void UnloadSaveControlData()
        {
            cachedFullSaveState = null;
            cachedSaveableEntityCardData.Clear();
            saveableEntityGUIDs.Clear();
            saveControlBoxLoaded = false;
            
            DrawSaveControlHeaderBox();
            DrawSceneSelectBox();
            DrawSaveControlEntityList();
        }

        private void ApplyAllSaveableEntityData(ClickEvent clickEvent)
        {
            foreach (SaveableEntityCardData saveableEntityCardData in cachedSaveableEntityCardData)
            {
                saveableEntityCardData.SaveSaveableEntity(false, SetLastSceneToCurrent);
                saveableEntityCardData.ResetSaveableSyncFlag();
            }
            SavingSystem.ManualSave(SaveFileManagerProvider.current.GetCurrentSaveName(), cachedFullSaveState);
            DrawSaveControlEntityList(); // Safety to draw in case any updates triggering repaint (ignored otherwise)
        }

        private static void ScrollToTopEdge(ScrollView scrollView, VisualElement visualElement)
        {
            if (scrollView == null || visualElement == null) { return; }
            if (!scrollView.contentContainer.Contains(visualElement)) { return; }
            scrollView.scrollOffset = new Vector2(scrollView.scrollOffset.x, visualElement.layout.y);
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
        
        // Internal for test visibility (scene-open events aren't otheriwse triggerable from EditMode tests practically)
        internal void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            UnloadSaveControlData();
        }
        #endregion
    }
}

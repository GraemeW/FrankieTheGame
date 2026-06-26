using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using Frankie.Core;
using Frankie.ZoneManagement;

namespace Frankie.Saving.Editor
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
        private bool saveControlBoxLoaded = false;
        private JObject cachedFullSaveState;
        private readonly List<SaveableEntityCardData> cachedSaveableEntityCardData = new();
        private readonly HashSet<string> saveableEntityGUIDs = new();
        
        // UI Cached References
        private Box saveHeaderBox;
        private Box selectionHeaderBox;
        private ListView saveEntries;
        private Box saveControlBox;
        private Box saveControlHeaderBox;
        private Box sceneSelectBox;
        private ScrollView saveControlEntityScrollView;
        
        #region UnityMethods
        [MenuItem("Tools/Save Editor", false, 205)]
        private static void ShowWindow()
        {
            var window = GetWindow<SaveEditor>("Save Editor");
            window.Show();
        }
        
        private void OnFocus()
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
            var splitView = new TwoPaneSplitView(0, 135, TwoPaneSplitViewOrientation.Vertical);
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
            if (sceneSelectBox == null) { return;  }
            sceneSelectBox.Clear();
            
            sceneSelectBox.Add(new Label("Last Saved Scene:"));
            
            string currentLastScene = SavingSystem.ManualGetLastScene(cachedFullSaveState);
            Zone lastZone = Zone.GetFromName(currentLastScene);
            var zoneField = new ObjectField { objectType = typeof(Zone), value = lastZone, style = { width = _largeButtonWidth } };
            zoneField.SetEnabled(cachedFullSaveState != null);
            sceneSelectBox.Add(zoneField);
            
            var openSceneButton = new Button { text = "Open Scene", style = { width = _largeButtonWidth } };
            openSceneButton.SetEnabled(lastZone != null);
            sceneSelectBox.Add(openSceneButton);

            zoneField.RegisterValueChangedCallback(changeEvent =>
            {
                Zone testZone = changeEvent.newValue as Zone;
                string testSceneName = string.Empty;
                if (testZone != null)
                {
                    SceneReference sceneReference = testZone.GetSceneReference();
                    if (!string.IsNullOrEmpty(sceneReference.SceneName)) { testSceneName = sceneReference.SceneName; }
                }

                if (testSceneName == string.Empty)
                {
                    zoneField.SetValueWithoutNotify(changeEvent.previousValue as Zone);
                    return;
                }
                
                lastZone = testZone;
                openSceneButton.SetEnabled(lastZone != null);
                SavingSystem.ManualUpdateLastScene(cachedFullSaveState, testSceneName);
                SavingSystem.ManualSave(SavingWrapper.GetCurrentSaveName(), cachedFullSaveState);
                
                Debug.LogWarning($"Saved last scene updated to {lastZone} - ensure that player mover is updated!");
            });
            
            openSceneButton.RegisterCallback<ClickEvent>(_ =>
            {
                if (lastZone == null) { return; }
                if (SceneManager.GetActiveScene().name == lastZone.GetSceneReference().SceneName) { return;}
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) { return; }

                string scenePath = lastZone.GetSceneReference().GetScenePath();
                if (string.IsNullOrEmpty(scenePath))
                {
                    Debug.LogWarning($"Last Scene not found: {lastZone}");
                    return;
                }
                
                EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                LoadSaveControlData(null);
            });
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
        #endregion
        
        #region SaveFileUtility
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
            UnloadSaveControlData();
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
        
        #region EditSaveUtility
        private static bool HasPlayerInParentHierarchy(Transform parent)
        {
            while (parent != null)
            {
                if (parent.GetComponent<Player>() != null) { return true; }
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
            SavingSystem.ManualSave(SavingWrapper.GetCurrentSaveName(), cachedFullSaveState);
            
            DrawSceneSelectBox();
        }
        
        private void LoadSaveControlData(ClickEvent _)
        {
            string currentSave = SavingWrapper.GetCurrentSaveName();
            if (string.IsNullOrEmpty(currentSave) || !SavingWrapper.HasSave(currentSave))
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
            foreach (SaveableEntity saveableEntity in SavingSystem.GetAllSaveableEntities().OrderBy(SaveableEntityCardData.GetEntitySortPriority).ThenBy(saveableEntity => saveableEntity.name).ToList())
            {
                if (saveableEntity == null) { continue; }
                if (HasPlayerInParentHierarchy(saveableEntity.transform.parent)) { continue; } // Avoid re-pulling entries e.g. in party container
                if (saveableEntityGUIDs.Contains(saveableEntity.GetUniqueIdentifier())) { continue; } // Avoid re-drawing dupe elements
                
                var saveableEntityCardData = new SaveableEntityCardData(saveableEntity, cachedFullSaveState, saveableEntityGUIDs, DrawSaveControlEntityList);
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
            SavingSystem.ManualSave(SavingWrapper.GetCurrentSaveName(), cachedFullSaveState);
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
        
        private void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            UnloadSaveControlData();
        }
        #endregion
    }
}

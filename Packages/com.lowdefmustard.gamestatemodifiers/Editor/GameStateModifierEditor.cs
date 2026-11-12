#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using UnityEngine.UIElements;
using LowDefMustard.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;

namespace LowDefMustard.GameStateModifiers.Editor
{
    [CustomEditor(typeof(GameStateModifier), true)]
    public class GameStateModifierEditor : UnityEditor.Editor
    {
        #region Tunables
        private const string _headerTitle = "Modifier Handler Data";
        private const string _noEntriesLabel = "No entries - add one below.";
        private const string _zoneFieldLabel = "Zone Name";
        private const string _objectFieldLabel = "Object Name";
        private const string _guidFieldLabel = "GUID";
        private const string _handlerTypeFieldLabel = "Handler Type";
        private const string _unknownHandlerTypeLabel = "Unknown Handler Type";
        private const string _editUnlockedLabel = "🔓 Editing";
        private const string _editLockedLabel = "🔒 Locked";
        private const string _editUnlockedHelpBox = "Manual editing enabled.  Warning:  Does NOT update assets in scene, edit at your discretion.";
        private const string _editLockedHelpBox = "Manual editing locked.";
        private const string _buttonAddEntryText = "+ Add Entry";
        private const string _buttonRemoveEntryText = "- Remove Last";
        private const string _buttonOpenSceneText = "Open & Select";
        private const string _buttonCleanEntriesText = "Remove Invalid Entries";
        #endregion
        
        // UI Class Names + State Keys (for test visibility)
        internal const string entryBoxClass = "gsm-handler-entry";
        internal const string groupFoldoutClass = "gsm-handler-group";
        internal const string groupFoldoutStateKeyStem = "LowDefMustard.GameStateModifiers.HandlerGroupFoldout.";

        #region Style Constants
        private static readonly StyleColor _editingActiveColour = new(new Color(1f, 0.55f, 0.1f));
        private static readonly StyleColor _editingInactiveColour = new(new Color(0.5f, 0.9f, 0.5f));
        private static readonly StyleColor _entryBorderColour = new(new Color(0.3f, 0.3f, 0.3f));
        private static readonly StyleColor _entryBackgroundColour = new(new Color(0.22f, 0.22f, 0.22f, 0.5f));
        private static readonly Color _emptyLabelColour= Color.gray;
        
        private const int _fontSize = 10;
        
        private const float _sectionMargin = 5f;
        private const float _listIndent = 15f;
        private const float _listSpacing = 2f;
        private const float _indexLabelWidth = 28f;
        
        private const float _borderWidth = 1f;
        private const float _borderRadius = 3f;
        private const float _entryPadding = 6f;
        private const float _entryMargin = 2f;
        
        private const float _standardButtonWidth = 100f;
        private const float _bigButtonWidth = 180f;
        private const float _smallButtonWidth = 22f;
        private const float _smallButtonHeight = 18f;
        #endregion

        // Handles
        [NoAutoStaticsCleanup] public static Action<string, Action> openSceneAndActProvider;
        
        // Functional State
        private GameStateModifier selectedGameStateModifier;
        private SerializedProperty gameStateModifierHandlerDataProperty;

        // Editor State
        private bool isListFoldedOut = true;
        private bool isEditingEnabled = false;

        // Cached UI References
        private Button editToggleButton;
        private HelpBox editHelpBox;
        private VisualElement listContainer;
        private VisualElement listItemsContainer;
        private Label linkedHandlersLabel;
        
        #region UnityMethods
        protected virtual void OnEnable()
        {
            selectedGameStateModifier = target as GameStateModifier;
            gameStateModifierHandlerDataProperty = serializedObject.FindProperty(GameStateModifier.GetLinkedHandlersRef());
        }

        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            InspectorElement.FillDefaultInspector(root, serializedObject, this);
            RemoveExcludedFields(root);
            
            // Header
            VisualElement headerSection = MakeEmptySection();
            root.Add(headerSection);
            
            VisualElement headerRow = MakeHeaderRow(_headerTitle);
            editToggleButton = MakeStandardButton("");
            editToggleButton.RegisterCallback<ClickEvent>(ToggleEditing);
            UpdateEditToggleButton();
            headerRow.Add(editToggleButton);
            headerSection.Add(headerRow);
            
            editHelpBox = new HelpBox(GetHelpBoxMessage(), GetHelpBoxType());
            headerSection.Add(editHelpBox);
            
            // Modifier List
            VisualElement modifierHandlerList = MakeModifierHandlerList();
            root.Add(modifierHandlerList);
            
            return root;
        }
        #endregion
        
        #region StaticUtility
        private static void RemoveExcludedFields(VisualElement root)
        {
            // Exclude default rendering of GameStateModifierHandlerData, so we can render our own way below
            string handlerDataRef = GameStateModifier.GetLinkedHandlersRef();
            
            List<VisualElement> elementsToRemove = new();
            foreach (VisualElement child in root.Children())
            {
                if (child is not PropertyField propertyField) { continue; }
                // PropertyField names are prefixed with "unity-property-field-"
                string fieldName = propertyField.bindingPath;
                if (fieldName == handlerDataRef || fieldName == "m_Script")
                {
                    elementsToRemove.Add(child);
                }
            }
            foreach (VisualElement elementToRemove in elementsToRemove) { root.Remove(elementToRemove); }
        }
        #endregion
        
        #region CoreFunctionality
        private static void SelectGameObject(string handlerGUID, string handlerGameObjectName, string handlerParentObjectName)
        {
            List<IGameStateModifierHandler> handlers = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include).OfType<IGameStateModifierHandler>().ToList();
            GameObject foundGameObject = handlers.Where(handler => handler.handlerGUID == handlerGUID).Select(handler => handler.gameObject).FirstOrDefault();

            if (foundGameObject == null)
            {
                string parentStem = !string.IsNullOrEmpty(handlerParentObjectName) ? $"{handlerParentObjectName}/" : "";
                LDMLog.Warning(LDMLogCategory.World, $"No handler found with GUID '{handlerGUID}'. Falling back to a name search for '{parentStem}{handlerGameObjectName}'; check the GUID hook-up.");
                
                List<GameObject> nameMatches = handlers.Select(handler => handler.gameObject).Where(handlerObject => IsNameMatch(handlerObject, handlerGameObjectName, handlerParentObjectName)).Distinct().ToList();
                if (nameMatches.Count > 1)
                {
                    LDMLog.Warning(LDMLogCategory.World, $"{nameMatches.Count} handlers named '{parentStem}{handlerGameObjectName}' found in the open scene; skipping selection (cannot guess).");
                    return;
                }
                foundGameObject = nameMatches.FirstOrDefault();
            }

            if (foundGameObject == null)
            {
                LDMLog.Warning(LDMLogCategory.World, $"No handler named '{handlerGameObjectName}' found in the open scene.");
                return;
            }

            Selection.activeGameObject = foundGameObject;
            var sceneView = SceneView.lastActiveSceneView;
            if (sceneView != null) { sceneView.FrameSelected(); }
            LDMLog.Info(LDMLogCategory.World, $"Selected '{handlerGameObjectName}'.", foundGameObject);
        }
        
        internal static bool IsNameMatch(GameObject handlerObject, string handlerGameObjectName, string handlerParentObjectName)
        {
            if (handlerObject == null || handlerObject.name != handlerGameObjectName) { return false; }
            string parentName = handlerObject.transform.parent != null ? handlerObject.transform.parent.name : "";
            return parentName == (handlerParentObjectName ?? "");
        }
        
        private void RemoveInvalidEntries(ClickEvent clickEvent)
        {
            if (gameStateModifierHandlerDataProperty == null) { return; }
            if (selectedGameStateModifier == null) { return; }

            int removedCount = selectedGameStateModifier.CleanDanglingModifierHandlerData();
            serializedObject.ApplyModifiedProperties();
            RefreshList();

            string summary = removedCount == 0 ? "All entries are valid, nothing removed." : $"{removedCount} invalid entries removed.";
            EditorUtility.DisplayDialog("Remove Invalid Entries", summary, "OK");
        }
        #endregion
        
        #region StateUpdates
        private void RefreshList()
        {
            serializedObject.Update();
            RebuildListItems();
            
            if (listContainer?.userData is Foldout foldout) { UpdateFoldoutCount(foldout); }
            
            foreach (VisualElement child in listContainer?.Children() ?? Enumerable.Empty<VisualElement>())
            {
                if (child.userData is (Button add, Button remove))
                {
                    UpdateAddRemoveEnabled(add, remove);
                }
            }
        }
        
        private void RebuildListItems()
        {
            if (listItemsContainer == null) { return; }
            listItemsContainer.Clear();

            if (gameStateModifierHandlerDataProperty == null || gameStateModifierHandlerDataProperty.arraySize == 0)
            {
                Label emptyLabel = MakeEmptyEntryLabel();
                listItemsContainer.Add(emptyLabel);
                return;
            }

            List<IGrouping<string, int>> handlerGroups = GroupEntryIndicesByHandlerType();
            bool expandByDefault = handlerGroups.Count == 1;
            foreach (IGrouping<string, int> handlerGroup in handlerGroups)
            {
                Foldout groupFoldout = MakeGroupFoldout(handlerGroup.Key, handlerGroup.Count(), expandByDefault);
                foreach (int gameStateModifierHandlerIndex in handlerGroup)
                {
                    groupFoldout.Add(MakeEntry(gameStateModifierHandlerIndex));
                    groupFoldout.Add(MakeVerticalListSpacer());
                }
                listItemsContainer.Add(groupFoldout);
            }
        }

        private List<IGrouping<string, int>> GroupEntryIndicesByHandlerType()
        {
            return Enumerable.Range(0, gameStateModifierHandlerDataProperty.arraySize)
                .GroupBy(GetHandlerTypeLabel)
                .OrderBy(handlerGroup => handlerGroup.Key == _unknownHandlerTypeLabel)
                .ThenBy(handlerGroup => handlerGroup.Key, StringComparer.Ordinal)
                .ToList();
        }

        private string GetHandlerTypeLabel(int itemIndex)
        {
            string handlerTypeName = gameStateModifierHandlerDataProperty.GetArrayElementAtIndex(itemIndex).FindPropertyRelative(LinkedHandlerRecord.GetHandlerTypeNameRef())?.stringValue;
            return string.IsNullOrWhiteSpace(handlerTypeName) ? _unknownHandlerTypeLabel : handlerTypeName;
        }
        
        private void UpdateFoldoutCount(Foldout foldout)
        {
            int count = gameStateModifierHandlerDataProperty?.arraySize ?? 0;
            foldout.text = $"Linked Handlers ({count})";
        }
        
        private void ToggleEditing(ClickEvent _)
        {
            isEditingEnabled = !isEditingEnabled;
            UpdateEditToggleButton();
            UpdateHelpBox();
            RefreshList();
        }

        private void UpdateEditToggleButton()
        {
            if (editToggleButton == null) { return; }
            editToggleButton.text = isEditingEnabled ? _editUnlockedLabel : _editLockedLabel;
            editToggleButton.style.color = isEditingEnabled ? _editingActiveColour : _editingInactiveColour;
        }

        private void UpdateHelpBox()
        {
            if (editHelpBox == null) { return; }
            editHelpBox.text = GetHelpBoxMessage();
            editHelpBox.messageType = GetHelpBoxType();
        }

        private string GetHelpBoxMessage() => isEditingEnabled ? _editUnlockedHelpBox : _editLockedHelpBox;
        private HelpBoxMessageType GetHelpBoxType() => isEditingEnabled ? HelpBoxMessageType.Warning : HelpBoxMessageType.Info;
        #endregion

        #region ListVisuals
        private VisualElement MakeModifierHandlerList()
        {
            listContainer = new VisualElement();
            
            var foldout = new Foldout { value = isListFoldedOut };
            UpdateFoldoutCount(foldout);
            foldout.RegisterValueChangedCallback(_ => { });
            
            listItemsContainer = new VisualElement { style = { marginLeft = _listIndent } };
            RebuildListItems();
            foldout.Add(listItemsContainer);

            listContainer.Add(foldout);
            listContainer.Add(MakeVerticalListSpacer());
            listContainer.Add(MakeAddRemoveButtons());
            listContainer.Add(MakeCleanUpButton());
            
            listContainer.userData = foldout;

            return listContainer;
        }
        
        private VisualElement MakeEntry(int itemIndex)
        {
            SerializedProperty currentElement = gameStateModifierHandlerDataProperty?.GetArrayElementAtIndex(itemIndex);
            if (currentElement == null) { return new VisualElement(); }

            SerializedProperty zoneNameProperty = currentElement.FindPropertyRelative(LinkedHandlerRecord.GetZoneNameRef());
            SerializedProperty handlerParentObjectNameProperty = currentElement.FindPropertyRelative(LinkedHandlerRecord.GetParentObjectNameRef());
            SerializedProperty handlerGameObjectNameProperty = currentElement.FindPropertyRelative(LinkedHandlerRecord.GetGameObjectNameRef());
            SerializedProperty handlerGUIDProperty = currentElement.FindPropertyRelative(LinkedHandlerRecord.GetHandlerGUIDRef());
            SerializedProperty handlerTypeProperty = currentElement.FindPropertyRelative(LinkedHandlerRecord.GetHandlerTypeNameRef());

            VisualElement entryBox = MakeEmptyEntryBox();
            entryBox.AddToClassList(entryBoxClass);
            string zoneName = zoneNameProperty?.stringValue ?? "";
            string handlerParentObjectName = handlerParentObjectNameProperty?.stringValue ?? "";
            string handlerGameObjectName = handlerGameObjectNameProperty?.stringValue ?? "";
            string handlerGUID = handlerGUIDProperty?.stringValue ?? "";

            string parentStem = !string.IsNullOrEmpty(handlerParentObjectName) ? $"{handlerParentObjectName}." : "";
            string displayName = string.IsNullOrWhiteSpace(handlerGameObjectName) || string.IsNullOrWhiteSpace(zoneName) ? $"Entry {itemIndex}" : $"{zoneName}/{parentStem}{handlerGameObjectName}";
            
            // Entry Header
            VisualElement entryHeaderRow = MakeEmptyRow();
            entryBox.Add(entryHeaderRow);
            Label indexLabel = MakeEntryHeaderLabel($"[{itemIndex}]");
            entryHeaderRow.Add(indexLabel);
            Label displayNameLabel = MakeEntryDisplayNameLabel(displayName);
            entryHeaderRow.Add(displayNameLabel);
            
            // Buttons
            Button openButton = MakeOpenSceneButton(zoneName, handlerGUID, handlerGameObjectName, handlerParentObjectName);
            entryHeaderRow.Add(openButton);
            Button deleteButton = MakeDeleteButton(itemIndex);
            entryHeaderRow.Add(deleteButton);

            // Fields
            VisualElement fieldsContainer = MakeFieldsContainer(isEditingEnabled); 
            var zoneField = new PropertyField(zoneNameProperty, _zoneFieldLabel);
            var objField = new PropertyField(handlerGameObjectNameProperty, _objectFieldLabel);
            var guidField = new PropertyField(handlerGUIDProperty, _guidFieldLabel);
            var handlerTypeField = new PropertyField(handlerTypeProperty, _handlerTypeFieldLabel);
            zoneField.Bind(serializedObject);
            objField.Bind(serializedObject);
            guidField.Bind(serializedObject);
            handlerTypeField.Bind(serializedObject);
            fieldsContainer.Add(zoneField);
            fieldsContainer.Add(objField);
            fieldsContainer.Add(guidField);
            fieldsContainer.Add(handlerTypeField);

            entryBox.Add(fieldsContainer);
            return entryBox;
        }

        private static Foldout MakeGroupFoldout(string handlerTypeLabel, int entryCount, bool expandByDefault)
        {
            string stateKey = groupFoldoutStateKeyStem + handlerTypeLabel;
            var groupFoldout = new Foldout
            {
                text = $"{handlerTypeLabel} ({entryCount})",
                value = expandByDefault || SessionState.GetBool(stateKey, false)
            };
            groupFoldout.AddToClassList(groupFoldoutClass);
            groupFoldout.RegisterValueChangedCallback(changeEvent =>
            {
                if (changeEvent.target != groupFoldout) { return; } // Foldout change events bubble from nested toggles
                SessionState.SetBool(stateKey, changeEvent.newValue);
            });
            return groupFoldout;
        }

        private static Button MakeOpenSceneButton(string zoneName, string handlerGUID, string handlerGameObjectName, string handlerParentObjectName)
        {
            bool viableSceneLoad = !string.IsNullOrWhiteSpace(zoneName) && !string.IsNullOrWhiteSpace(handlerGameObjectName);
            Button openButton = MakeStandardButton(_buttonOpenSceneText);
            openButton.RegisterCallback<ClickEvent>(_ =>
            {
                if (openSceneAndActProvider != null) { EditorApplication.delayCall += () => openSceneAndActProvider?.Invoke(zoneName, () => SelectGameObject(handlerGUID, handlerGameObjectName, handlerParentObjectName)); }
                else { DefaultOpenSceneAndAct(zoneName, () => SelectGameObject(handlerGUID, handlerGameObjectName, handlerParentObjectName)); }
            });
            openButton.SetEnabled(viableSceneLoad);
            return openButton;
        }

        private static void DefaultOpenSceneAndAct(string zoneName, Action action)
        {
            if (!GameStateModifier.DefaultGetScenePath(zoneName, out string scenePath)) { return; }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) { return; }
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            action?.Invoke();
        }

        private Button MakeDeleteButton(int gameStateModifierHandlerIndex)
        {
            Button deleteButton = MakeSmallButton();
            deleteButton.RegisterCallback<ClickEvent>(_ =>
            {
                gameStateModifierHandlerDataProperty.DeleteArrayElementAtIndex(gameStateModifierHandlerIndex);
                serializedObject.ApplyModifiedProperties();
                RefreshList();
            });
            deleteButton.SetEnabled(isEditingEnabled);
            return deleteButton;
        }
        
        private VisualElement MakeAddRemoveButtons()
        {
            VisualElement buttonRow = MakeEmptyButtonRow();

            Button addButton = MakeStandardButton(_buttonAddEntryText);
            addButton.RegisterCallback<ClickEvent>(_ =>
            {
                if (!isEditingEnabled) { return; }
                gameStateModifierHandlerDataProperty.arraySize++;
                SerializedProperty newElement = gameStateModifierHandlerDataProperty.GetArrayElementAtIndex(gameStateModifierHandlerDataProperty.arraySize - 1);
                newElement.FindPropertyRelative(LinkedHandlerRecord.GetZoneNameRef()).stringValue = string.Empty;
                newElement.FindPropertyRelative(LinkedHandlerRecord.GetGameObjectNameRef()).stringValue = string.Empty;
                serializedObject.ApplyModifiedProperties();
                RefreshList();
            });
            buttonRow.Add(addButton);

            var removeButton = MakeStandardButton(_buttonRemoveEntryText);
            removeButton.RegisterCallback<ClickEvent>(_ =>
            {
                if (!isEditingEnabled) { return; }
                if (gameStateModifierHandlerDataProperty.arraySize <= 0) { return; }
                gameStateModifierHandlerDataProperty.arraySize--;
                serializedObject.ApplyModifiedProperties();
                RefreshList();
            });
            buttonRow.Add(removeButton);
            
            buttonRow.RegisterCallback<AttachToPanelEvent>(_ => UpdateAddRemoveEnabled(addButton, removeButton));
            buttonRow.userData = (addButton, removeButton);

            return buttonRow;
        }

        private void UpdateAddRemoveEnabled(Button addButton, Button removeButton)
        {
            addButton.SetEnabled(isEditingEnabled);
            removeButton.SetEnabled(isEditingEnabled);
        }

        private VisualElement MakeCleanUpButton()
        {
            VisualElement buttonRow = MakeEmptyButtonRow();
            Button cleanButton = MakeBigButton(_buttonCleanEntriesText);
            cleanButton.RegisterCallback<ClickEvent>(RemoveInvalidEntries);
            buttonRow.Add(cleanButton);
            return buttonRow;
        }
        #endregion

        #region UIBaseConstruction
        private static VisualElement MakeEmptySection()
        {
            return new VisualElement
            {
                style =
                {
                    marginTop = _sectionMargin,
                    marginBottom = _sectionMargin
                }
            };
        }
        
        private static VisualElement MakeEmptyRow()
        {
            return new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center
                }
            };
        }

        private static VisualElement MakeEmptyButtonRow()
        {
            return new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    justifyContent = Justify.FlexEnd,
                    marginTop = _entryMargin
                }
            };
        }
        
        private static VisualElement MakeHeaderRow(string headerTitle)
        {
            VisualElement headerRow = MakeEmptyRow();
            Label headerLabel = MakeHeaderLabel(headerTitle);
            headerRow.Add(headerLabel);
            return headerRow;
        }

        private static Label MakeHeaderLabel(string headerTitle)
        {
            return new Label(headerTitle)
            {
                style =
                {
                    fontSize = _fontSize,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    flexGrow = 1
                }
            };
        }

        private static Label MakeEmptyEntryLabel()
        {
            return new Label(_noEntriesLabel)
            {
                style =
                {
                    unityTextAlign = TextAnchor.MiddleCenter,
                    color = new StyleColor(_emptyLabelColour),
                    fontSize = _fontSize
                }
            };
        }

        private static VisualElement MakeEmptyEntryBox()
        {
            return new VisualElement
            {
                style =
                {
                    backgroundColor = _entryBackgroundColour,
                    borderTopWidth = _borderWidth,
                    borderBottomWidth = _borderWidth,
                    borderLeftWidth = _borderWidth,
                    borderRightWidth = _borderWidth,
                    borderTopColor = _entryBorderColour,
                    borderBottomColor = _entryBorderColour,
                    borderLeftColor = _entryBorderColour,
                    borderRightColor = _entryBorderColour,
                    borderTopLeftRadius = _borderRadius,
                    borderTopRightRadius = _borderRadius,
                    borderBottomLeftRadius = _borderRadius,
                    borderBottomRightRadius = _borderRadius,
                    paddingTop = _entryPadding,
                    paddingBottom = _entryPadding,
                    paddingLeft = _entryPadding,
                    paddingRight = _entryPadding,
                    marginTop = _entryMargin,
                    marginBottom = _entryMargin,
                }
            };
        }

        private static Label MakeEntryHeaderLabel(string entryHeaderTitle)
        {
            return new Label(entryHeaderTitle)
            {
                style =
                {
                    width = _indexLabelWidth,
                    flexShrink = 0
                }
            };
        }

        private static Label MakeEntryDisplayNameLabel(string displayName)
        {
            return new Label(displayName)
            {
                style =
                {
                    unityFontStyleAndWeight = FontStyle.Bold,
                    flexGrow = 1
                }
            };
        }

        private static Button MakeStandardButton(string buttonText)
        {
            return new Button
            {
                text = buttonText,
                style =
                {
                    width = _standardButtonWidth
                }
            };
        }

        private static Button MakeBigButton(string buttonText)
        {
            return new Button
            {
                text = buttonText,
                style =
                {
                    width = _bigButtonWidth
                }
            };
        }
        
        private static Button MakeSmallButton()
        {
            return new Button
            {
                text = "−",
                style =
                {
                    width = _smallButtonWidth,
                    height = _smallButtonHeight
                }
            };
        }

        private static VisualElement MakeFieldsContainer(bool isEditingEnabled)
        {
            var fieldsContainer = new VisualElement { style =
                {
                    marginTop = _sectionMargin,
                    display = isEditingEnabled ? DisplayStyle.Flex : DisplayStyle.None
                }
            };
            fieldsContainer.SetEnabled(isEditingEnabled);
            return fieldsContainer;
        }
        
        private static VisualElement MakeVerticalListSpacer() => new() { style = { height = _listSpacing } };
        #endregion
    }
}
#endif

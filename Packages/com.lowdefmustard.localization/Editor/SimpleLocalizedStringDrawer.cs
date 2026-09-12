using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.Localization;
using Object = UnityEngine.Object;

namespace LowDefMustard.Localization.Editor
{
    // Note:  SimpleLocalizedStringAttribute is non-generic, so this drawer is too (no per-project alias needed)
    // It reaches whichever closed LocalizationToolBase<T> a field's attribute was configured with via LocalizationToolBridgeRegistry.GetBridge(enumType)
    
    [CustomPropertyDrawer(typeof(SimpleLocalizedStringAttribute))]
    public class SimpleLocalizedStringDrawer : PropertyDrawer
    {
        // IMPORTANT: Do NOT store any state on `this`
        // Unity reuses a single PropertyDrawer instance across every element of an array/list that shares this attribute
        // Any state that varies per-element must live in a local object created fresh inside CreatePropertyGUI and threaded through explicitly
        private class ElementState
        {
            public readonly SerializedProperty property;
            public readonly FieldInfo fieldInfo;
            public readonly string nicePropertyName;
            public LocalizedString localizedString;
            public Enum tableType;
            public ILocalizationToolBridge localizationToolBridge;
            public bool isKeyEditable;
            public bool isKeyUnlocked;
            public TextField keyTextField;
            public TextField contentsTextField;
            public Toggle lockToggle;
            public Button newKeyButton;
            public Button renameKeyButton;
            public Button deleteKeyButton;

            public ElementState(SerializedProperty property, FieldInfo fieldInfo)
            {
                this.property = property;
                this.fieldInfo = fieldInfo;
                nicePropertyName = property.displayName.Replace("Localized", "");
            }
        }

        // Hooks
        // Key generator is project-specific, so must hook up independently via [InitializeOnLoad] registration
        public static Func<Object, string, Type, bool, string> typeSpecificKeyGenerator;

        #region UIProperties
        private const string _keyLabel = "Key";
        private const string _keyFieldName = "keyField";
        private const string _textLabel = "Content";
        private const string _textFieldName = "contentField";
        private const string _newKeyButtonLabel = "Generate New Key-Entry";
        private const string _newKeyButtonName = "newKeyButton";
        private const string _renameKeyButtonLabel = "Auto-Rename Key-Entry";
        private const string _renameKeyButtonName = "renameKeyButton";
        private const string _deleteKeyButtonLabel = "Delete Key-Entry";
        private const string _deleteKeyButtonName = "deleteKeyButton";
        private const string _lockLabel = "🔒";
        private const string _unlockLabel = "🔓";
        private const string _lockTooltip = "Unlock to allow editing the localization key.";
        private const string _lockToggleName = "lockToggle";

        private const int _labelFontSize = 10;
        private const int _headerFontSize = 11;
        private static readonly Color _errorTextColour = new(0.9f, 0.3f, 0.3f);
        private static readonly Color _disabledTextColour = new(0.5f, 0.5f, 0.5f);

        private const float _labelWidth = 56f;
        private const float _buttonWidth = 150f;
        private const float _rowHeight = 20f;
        private const float _lockToggleHeight = 20f;
        private const float _lockToggleLabelWidth = 20f;
        private const int _rowSpacingTop  = 2;
        private const int _rowSpacingBottom  = 2;
        private const int _sectionPaddingLeft = 4;
        #endregion

        #region UnityMethods
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            LocalizationLocale.TriggerLocalizationSettingsInitialization();

            // Per-Element State (see Note above)
            var state = new ElementState(property, fieldInfo);

            // State Initialization
            var simpleLocalizedStringAttribute = (SimpleLocalizedStringAttribute)attribute;
            state.tableType = simpleLocalizedStringAttribute.localizationTableType;

            // No explicit table type on the attribute - fall back to the containing object's ILocalizableCore.localizationTableTypeValue (e.g. a shared package type)
            if (state.tableType == null)
            {
                if (property.serializedObject.targetObject is not ILocalizableCore localizable) { return MakeErrorBox($"[SimpleLocalizedString] on '{property.propertyPath}' has no table type, and '{property.serializedObject.targetObject?.GetType().Name}' does not implement ILocalizableCore to provide one."); }
                
                state.tableType = localizable.localizationTableTypeValue;
                if (state.tableType == null) { return MakeErrorBox($"'{property.serializedObject.targetObject.GetType().Name}' has no table type registered - see LocalizableClassTableTypeRegistry.Register."); }
            }

            if (!LocalizationToolBridgeRegistry.TryGetBridge(state.tableType.GetType(), out state.localizationToolBridge)) { return MakeErrorBox($"No localization bridge registered for table type '{state.tableType}'."); }

            state.localizedString = property.boxedValue as LocalizedString;
            if (state.localizedString == null) { return MakeErrorBox("Property is not LocalizedString."); }

            state.isKeyEditable = simpleLocalizedStringAttribute.isKeyEditable;
            state.isKeyUnlocked = false;

            if (!state.localizationToolBridge.GetOrMakeTableCollection(state.tableType, out StringTableCollection _)) { return MakeErrorBox($"Could not find or create StringTableCollection of type: '{state.tableType}'."); }
            if (IsKeyEmpty(state, out TableEntryReference _) && HasPrefab(property, out Object prefabSource)) { TryResetToPrefab(state, prefabSource); }

            // Build UI Elements
            VisualElement root = MakeRoot(state.nicePropertyName);
            VisualElement keyRow = BuildKeyRow(state);
            root.Add(keyRow);
            VisualElement lockToggleRow = BuildLockToggleRow(state);
            root.Add(lockToggleRow);
            VisualElement contentsRow = BuildContentsRow(state);
            root.Add(contentsRow);
            VisualElement newKeyButtonRow = BuildButtonRow(_newKeyButtonLabel, state.isKeyEditable && state.isKeyUnlocked, _newKeyButtonName, out state.newKeyButton);
            root.Add(newKeyButtonRow);
            VisualElement renameKeyButtonRow = BuildButtonRow(_renameKeyButtonLabel, state.isKeyEditable && state.isKeyUnlocked, _renameKeyButtonName, out state.renameKeyButton);
            root.Add(renameKeyButtonRow);
            VisualElement deleteKeyButtonRow = BuildButtonRow(_deleteKeyButtonLabel, state.isKeyEditable && state.isKeyUnlocked, _deleteKeyButtonName, out state.deleteKeyButton);
            root.Add(deleteKeyButtonRow);

            // Assign callbacks
            state.contentsTextField.RegisterValueChangedCallback(changeEvent => OnContentsChanged(state, changeEvent.newValue));
            state.keyTextField.RegisterValueChangedCallback(changeEvent => OnKeyChanged(state, changeEvent.newValue));
            state.lockToggle.RegisterValueChangedCallback(evt =>
            {
                state.isKeyUnlocked = evt.newValue;
                Label toggleLabel = lockToggleRow.Q<Label>();
                if (toggleLabel != null) { toggleLabel.text = state.isKeyUnlocked ? _unlockLabel : _lockLabel; }

                bool isKeyEmpty = IsKeyEmpty(state, out _);
                state.keyTextField.SetEnabled(state.isKeyEditable && state.isKeyUnlocked);
                state.newKeyButton.SetEnabled(state.isKeyEditable && state.isKeyUnlocked);
                state.renameKeyButton.SetEnabled(state.isKeyEditable && state.isKeyUnlocked && !isKeyEmpty);
                ReconcileDeleteButtonState(state, state.isKeyEditable && state.isKeyUnlocked && !isKeyEmpty);
            });
            state.newKeyButton.RegisterCallback<ClickEvent>(_ => HandleNewKeyButtonClick(state));
            state.renameKeyButton.RegisterCallback<ClickEvent>(_ => HandleRenameKeyButtonClick(state));
            state.deleteKeyButton.RegisterCallback<ClickEvent>(_ => HandleDeleteButtonClick(state));

            return root;
        }
        #endregion

        #region UtilityMethodsAndCallbacks
        private static bool IsKeyEmpty(ElementState state, out TableEntryReference tableEntryReference)
        {
            tableEntryReference = new TableEntryReference();
            if (state.localizedString.IsEmpty) { return true; }

            string currentKey = state.localizationToolBridge.ResolveKeyName(state.tableType, state.localizedString, out tableEntryReference);
            return tableEntryReference.ReferenceType == TableEntryReference.Type.Empty || string.IsNullOrWhiteSpace(currentKey);
        }

        private static bool HasPrefab(SerializedProperty property, out Object prefabSource)
        {
            prefabSource = null;
            if (!TryGetTargetComponent(property, out Component targetComponent)) { return false; }
            if (PrefabUtility.IsPartOfPrefabAsset(targetComponent) || PrefabUtility.IsPartOfPrefabInstance(targetComponent))
            {
                prefabSource = PrefabUtility.GetCorrespondingObjectFromSource(targetComponent);
                return prefabSource != null;
            }
            return false;
        }

        private static bool TryGetTargetComponent(SerializedProperty property, out Component component)
        {
            Object targetObject = property.serializedObject.targetObject;
            component = targetObject as Component;
            return targetObject != null && component != null;
        }

        private static void SetKeyFromLocalization(ElementState state, bool isEnabled, bool shouldNotify)
        {
            if (state.keyTextField == null) { return; }
            string keyValue = state.localizationToolBridge.ResolveKeyName(state.tableType, state.localizedString, out TableEntryReference _);

            if (shouldNotify) { state.keyTextField.value = keyValue; }
            else { state.keyTextField.SetValueWithoutNotify(keyValue); }
            state.keyTextField.SetEnabled(isEnabled);
        }

        private static void SetContentsFromLocalization(ElementState state, bool shouldNotify)
        {
            if (state.contentsTextField == null) { return; }
            bool isKeyCurrentlyEmpty = IsKeyEmpty(state, out TableEntryReference tableEntryReference);
            string contentsValue = state.localizationToolBridge.GetEnglishEntry(state.tableType, tableEntryReference);

            if (shouldNotify) { state.contentsTextField.value = contentsValue; }
            else { state.contentsTextField.SetValueWithoutNotify(contentsValue); }
            DisableContentsForEmptyKey(state.contentsTextField, isKeyCurrentlyEmpty);
        }

        private static void OnContentsChanged(ElementState state, string newContents)
        {
            if (state.localizedString == null) { return; }
            TableEntryReference tableEntryReference = state.localizedString.TableEntryReference;
            if (tableEntryReference.ReferenceType == TableEntryReference.Type.Empty) { return; }

            Object targetObject = state.property.serializedObject.targetObject;
            string oldContents = state.localizationToolBridge.GetEnglishEntry(state.tableType, tableEntryReference);
            if (newContents == oldContents) { return; }

            string newKey = null;
            if (HasPrefab(state.property, out Object prefabSource) && !IsPropertyUniqueFromPrefab(state, prefabSource))
            {
                // Avoid overwriting prefab entry, generate new key
                newKey = typeSpecificKeyGenerator != null
                    ? typeSpecificKeyGenerator.Invoke(targetObject, state.property.name, state.fieldInfo.DeclaringType, true)
                    : DefaultKeyGenerator.GenerateKindaUniqueKey(targetObject, state.property.name, state.fieldInfo.DeclaringType, true);

                tableEntryReference = newKey;
            }

            if (!state.localizationToolBridge.AddUpdateEnglishEntry(state.tableType, tableEntryReference, newContents)) { return; }
            if (string.IsNullOrWhiteSpace(newKey)) { return; }

            state.localizationToolBridge.SafelyUpdateReference(state.tableType, state.localizedString, newKey);
            Undo.RecordObject(targetObject, "Bind localized string to new key");
            state.property.boxedValue = state.localizedString;
            state.property.serializedObject.ApplyModifiedProperties();
            state.property.serializedObject.Update();

            state.keyTextField?.SetValueWithoutNotify(newKey);
            state.newKeyButton?.SetEnabled(state.isKeyUnlocked);
            state.renameKeyButton?.SetEnabled(state.isKeyUnlocked);
            ReconcileDeleteButtonState(state, state.isKeyUnlocked);
        }

        private static void OnKeyChanged(ElementState state, string newKey)
        {
            Object targetObject = state.property.serializedObject.targetObject;
            if (state.localizedString == null || targetObject == null) { return; }

            string oldKey = state.localizationToolBridge.ResolveKeyName(state.tableType, state.localizedString, out TableEntryReference tableEntryReference);
            if (newKey == oldKey || string.IsNullOrWhiteSpace(newKey)) { return; }

            bool newKeyExists = state.localizationToolBridge.HasTableEntry(state.tableType, newKey);
            if (!newKeyExists) { if (!state.localizationToolBridge.MakeOrRenameKey(state.tableType, tableEntryReference, newKey)) { return; } }
            if (!state.localizationToolBridge.SafelyUpdateReference(state.tableType, state.localizedString, newKey)) { return; }

            if (newKeyExists) { SetContentsFromLocalization(state, false); }

            Undo.RecordObject(targetObject, "Bind localized string to updated key");
            state.property.boxedValue = state.localizedString;
            state.property.serializedObject.ApplyModifiedProperties();
            state.property.serializedObject.Update();

            bool isKeyEmpty = IsKeyEmpty(state, out _);
            state.renameKeyButton.SetEnabled(!isKeyEmpty);
            DisableContentsForEmptyKey(state.contentsTextField, isKeyEmpty);
            ReconcileDeleteButtonState(state, !isKeyEmpty);
        }

        private static void HandleNewKeyButtonClick(ElementState state)
        {
            Object targetObject = state.property.serializedObject.targetObject;
            if (state.localizedString == null || targetObject == null) { return; }

            state.localizationToolBridge.ResolveKeyName(state.tableType, state.localizedString, out TableEntryReference currentTableEntryReference);
            string currentContents = "";
            if (currentTableEntryReference.ReferenceType != TableEntryReference.Type.Empty)
            {
                currentContents = state.localizationToolBridge.GetEnglishEntry(state.tableType, currentTableEntryReference);
            }

            string newKey = typeSpecificKeyGenerator != null
                ? typeSpecificKeyGenerator.Invoke(targetObject, state.property.name, state.fieldInfo.DeclaringType, true)
                : DefaultKeyGenerator.GenerateKindaUniqueKey(targetObject, state.property.name, state.fieldInfo.DeclaringType, true);

            if (!state.localizationToolBridge.AddUpdateEnglishEntry(state.tableType, newKey, currentContents)) { return; }
            if (!state.localizationToolBridge.SafelyUpdateReference(state.tableType, state.localizedString, newKey)) { return; }

            Undo.RecordObject(targetObject, "Bind localized string to new key");
            state.property.boxedValue = state.localizedString;
            state.property.serializedObject.ApplyModifiedProperties();
            state.property.serializedObject.Update();

            state.keyTextField?.SetValueWithoutNotify(newKey);
            bool isKeyEmpty = IsKeyEmpty(state, out _);
            state.renameKeyButton.SetEnabled(!isKeyEmpty);
            DisableContentsForEmptyKey(state.contentsTextField, isKeyEmpty);
            ReconcileDeleteButtonState(state, !isKeyEmpty);
        }

        private static void HandleRenameKeyButtonClick(ElementState state)
        {
            Object targetObject = state.property.serializedObject.targetObject;
            if (targetObject == null || state.localizedString == null || IsKeyEmpty(state, out TableEntryReference _))
            {
                Debug.Log("Localized string is not configured, cannot rename.");
                return;
            }

            var newKey = typeSpecificKeyGenerator != null
                ? typeSpecificKeyGenerator.Invoke(targetObject, state.property.name, state.fieldInfo.DeclaringType, true)
                : DefaultKeyGenerator.GenerateKindaUniqueKey(targetObject, state.property.name, state.fieldInfo.DeclaringType, true);

            state.keyTextField.value = newKey;
        }

        private static void HandleDeleteButtonClick(ElementState state)
        {
            Object targetObject = state.property.serializedObject.targetObject;
            if (state.localizedString == null || targetObject == null) { return; }

            if (string.IsNullOrWhiteSpace(state.keyTextField.value)) { return; }
            state.localizationToolBridge.RemoveEntry(state.tableType, state.keyTextField.value);
            state.localizedString.SetReference("", "");
            state.property.serializedObject.Update();

            if (HasPrefab(state.property, out Object prefabSource))
            {
                TryResetToPrefab(state, prefabSource);
                SetKeyFromLocalization(state, true, false);
                SetContentsFromLocalization(state, false);
            }
            else
            {
                state.keyTextField?.SetValueWithoutNotify("");
                state.contentsTextField?.SetValueWithoutNotify("");
            }

            bool isKeyCurrentlyEmpty = IsKeyEmpty(state, out _);
            state.lockToggle.value = false;
            DisableContentsForEmptyKey(state.contentsTextField, isKeyCurrentlyEmpty);
        }

        private static void ReconcileDeleteButtonState(ElementState state, bool isEnabled)
        {
            if (state.deleteKeyButton == null) { return; }
            if (!isEnabled)
            {
                state.deleteKeyButton.SetEnabled(false);
                return;
            }
            if (!HasPrefab(state.property, out Object prefabSource) || IsPropertyUniqueFromPrefab(state, prefabSource))
            {
                state.deleteKeyButton.SetEnabled(true);
                return;
            }
            state.deleteKeyButton.SetEnabled(false);
        }

        private static void TryResetToPrefab(ElementState state, Object prefabSource)
        {
            if (!TryGetTargetComponent(state.property, out Component targetComponent)) { return; }

            if (PrefabUtility.IsPartOfPrefabInstance(targetComponent))
            {
                Undo.RecordObject(targetComponent.gameObject, "Reset localized string to prefab value");
                PrefabUtility.RevertPropertyOverride(state.property, InteractionMode.UserAction);
            }
            else if (PrefabUtility.IsPartOfPrefabAsset(targetComponent))
            {
                if (prefabSource == null) { return; }

                using var prefabSerializedObject = new SerializedObject(prefabSource);
                SerializedProperty prefabProperty = prefabSerializedObject.FindProperty(state.property.propertyPath);
                if (prefabProperty == null) { return; }

                Undo.RecordObject(targetComponent.gameObject, "Reset localized string to prefab value");
                state.property.serializedObject.CopyFromSerializedPropertyIfDifferent(prefabProperty);
                state.property.serializedObject.ApplyModifiedProperties();
            }
            else { return; }
            state.property.serializedObject.Update();
            state.localizedString = state.property.boxedValue as LocalizedString;
        }

        private static bool IsPropertyUniqueFromPrefab(ElementState state, Object prefabSource)
        {
            // Input sanity checks
            if (!TryGetTargetComponent(state.property, out Component _)) { return false; }

            if (state.localizedString == null) { return false; }
            TableEntryReference targetTableEntryReference = state.localizationToolBridge.GetTableEntryReferencedByID(state.tableType, state.localizedString.TableEntryReference);
            if (targetTableEntryReference.ReferenceType != TableEntryReference.Type.Id) { return false; }

            // Check for prefab existence
            if (prefabSource == null) { return true; } // No prefab source found
            using var prefabSerializedObject = new SerializedObject(prefabSource);
            SerializedProperty prefabProperty = prefabSerializedObject.FindProperty(state.property.propertyPath);
            if (prefabProperty == null) { return true; } // No prefab property found

            TableEntryReference prefabTableEntryReference = state.localizationToolBridge.GetSerializedTableEntryKeyID(state.tableType, prefabProperty);
            if (prefabTableEntryReference.ReferenceType != TableEntryReference.Type.Id) { return true; }

            // Match to ID -- if different, allow for deletion (since unique entry on target)
            return targetTableEntryReference.KeyId != prefabTableEntryReference.KeyId;
        }
        #endregion

        #region RowBuilders
        private static VisualElement BuildKeyRow(ElementState state)
        {
            VisualElement keyRow = MakeLabeledRow(_keyLabel, _keyFieldName, out state.keyTextField);
            bool isEnabled = state.isKeyEditable && state.isKeyUnlocked;
            SetKeyFromLocalization(state, isEnabled, true);
            state.keyTextField.isDelayed = true;
            return keyRow;
        }

        private static VisualElement BuildLockToggleRow(ElementState state)
        {
            VisualElement lockToggleRow = MakeLockToggleBaseRow();
            state.lockToggle = MakeToggle(state.isKeyUnlocked, _lockToggleName);
            state.lockToggle.SetEnabled(state.isKeyEditable);
            lockToggleRow.Add(state.lockToggle);
            return lockToggleRow;
        }

        private static VisualElement BuildContentsRow(ElementState state)
        {
            VisualElement contentsRow = MakeLabeledRow(_textLabel, _textFieldName, out state.contentsTextField);
            SetContentsFromLocalization(state, true);
            state.contentsTextField.isDelayed = true;
            return contentsRow;
        }

        private static VisualElement BuildButtonRow(string buttonLabel, bool isEnabled, string buttonName, out Button button)
        {
            VisualElement buttonRow = MakeButtonBaseRow();
            button = new Button
            {
                name = buttonName,
                text = buttonLabel,
                style = { width = _buttonWidth }
            };
            button.SetEnabled(isEnabled);

            buttonRow.Add(button);
            return buttonRow;
        }

        private static void DisableContentsForEmptyKey(TextField contentsTextField, bool isKeyCurrentlyEmpty)
        {
            if (contentsTextField == null) { return; }
            contentsTextField.SetEnabled(!isKeyCurrentlyEmpty);
            if (isKeyCurrentlyEmpty) { contentsTextField.style.color = new StyleColor(_disabledTextColour); }
        }
        #endregion

        #region BaseUIElements
        private static VisualElement MakeRoot(string headerDisplayName)
        {
            var root = new VisualElement
            {
                style = { paddingLeft = _sectionPaddingLeft }
            };

            var header = new Label(headerDisplayName)
            {
                style =
                {
                    unityFontStyleAndWeight = FontStyle.Bold,
                    fontSize = _headerFontSize,
                    marginTop = _rowSpacingTop,
                    marginBottom = _rowSpacingBottom
                }
            };
            root.Add(header);
            return root;
        }

        private static VisualElement MakeLabeledRow(string labelText, string fieldName, out TextField textField)
        {
            var labeledRow = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    marginTop = _rowSpacingTop,
                    marginBottom = _rowSpacingBottom,
                    height = _rowHeight
                }
            };

            var label = new Label(labelText)
            {
                style =
                {
                    width = _labelWidth,
                    fontSize = _labelFontSize,
                    unityTextAlign = TextAnchor.MiddleLeft
                }
            };

            textField = new TextField
            {
                name = fieldName,
                isDelayed = true,
                style = { flexGrow = 1 }
            };

            labeledRow.Add(label);
            labeledRow.Add(textField);
            return labeledRow;
        }

        private static VisualElement MakeButtonBaseRow()
        {
            var buttonBaseRow = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    marginTop = _rowSpacingTop,
                    marginBottom = _rowSpacingBottom,
                    height = _rowHeight
                }
            };
            return buttonBaseRow;
        }

        private static VisualElement MakeLockToggleBaseRow()
        {
            var lockToggleBaseRow = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    marginTop = _rowSpacingTop,
                    marginBottom = _rowSpacingBottom,
                    height = _rowHeight,
                }
            };

            var spacer = new VisualElement
            {
                style = { width = _labelWidth }
            };
            lockToggleBaseRow.Add(spacer);
            return lockToggleBaseRow;
        }

        private static Toggle MakeToggle(bool isUnlocked, string toggleName)
        {
            return new Toggle
            {
                name = toggleName,
                label = isUnlocked ? _unlockLabel : _lockLabel,
                labelElement = {
                    style =
                    {
                        minWidth = _lockToggleLabelWidth,
                        unityTextAlign = TextAnchor.MiddleLeft
                    }
                },
                value = isUnlocked,
                tooltip = _lockTooltip,
                style =
                {
                    height = _lockToggleHeight,
                }
            };
        }

        private static VisualElement MakeErrorBox(string message)
        {
            var errorBox = new HelpBox(message, HelpBoxMessageType.Error)
            {
                style = { color = new StyleColor(_errorTextColour) }
            };
            return errorBox;
        }
        #endregion
    }
}

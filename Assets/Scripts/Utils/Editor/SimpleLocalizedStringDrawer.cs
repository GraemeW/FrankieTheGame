#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Frankie.Utils.Editor
{
    [CustomPropertyDrawer(typeof(SimpleLocalizedStringAttribute))]
    public class SimpleLocalizedStringDrawer : PropertyDrawer
    {
        // State
        LocalizedString localizedString;
        private bool isKeyUnlocked;
        private TextField keyTextField;
        private TextField contentsTextField;
        private Toggle lockToggle;
        private Button deleteKeyButton;
        
        #region UIProperties
        private const string _keyLabel = "Key";
        private const string _textLabel = "Content";
        private const string _newKeyButtonLabel = "Make New Key-Entry";
        private const string _deleteKeyButtonLabel = "Delete Current Key-Entry";
        private const string _lockLabel = "🔒";
        private const string _unlockLabel = "🔓";
        private const string _lockTooltip = "Unlock to allow editing the localization key.";
        
        private const int _labelFontSize = 12;
        private const int _headerFontSize = 13;
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
            // Local Variables
            var simpleLocalizedStringAttribute = (SimpleLocalizedStringAttribute)attribute;
            LocalizationTableType localizationTableType = simpleLocalizedStringAttribute.localizationTableType;
            bool isKeyEditable = simpleLocalizedStringAttribute.isKeyEditable;
            string sanitizedPropertyName = property.name.Replace("localized","");
            string niceSanitizedPropertyName = property.displayName.Replace("Localized", "");
            
            // State
            LocalizationTool.InitializeEnglishLocale();
            isKeyUnlocked = false;
            localizedString = property.boxedValue as LocalizedString;
            if (localizedString == null) { return MakeErrorBox("Property is not LocalizedString."); }
            if (!LocalizationTool.GetOrMakeTableCollection(localizationTableType, out StringTableCollection _)) { return MakeErrorBox($"Could not find or create StringTableCollection of type: '{localizationTableType}'."); }
            if (IsKeyEmpty(localizedString, localizationTableType, out TableEntryReference _) && HasPrefab(property))
            {
                TryResetToPrefab(property);
            }
            
            // Build UI Elements
            VisualElement root = MakeRoot(niceSanitizedPropertyName);
            VisualElement keyRow = BuildKeyRow(localizedString, localizationTableType, isKeyEditable && isKeyUnlocked, out keyTextField);
            root.Add(keyRow);
            VisualElement lockToggleRow = BuildLockToggleRow(isKeyEditable, isKeyUnlocked, out lockToggle); 
            root.Add(lockToggleRow);
            VisualElement contentsRow =  BuildContentsRow(localizedString, localizationTableType, out contentsTextField);
            root.Add(contentsRow);
            VisualElement newKeyButtonRow = BuildButtonRow(_newKeyButtonLabel, isKeyEditable && isKeyUnlocked, out Button newKeyButton); 
            root.Add(newKeyButtonRow);
            VisualElement deleteKeyButtonRow = BuildButtonRow(_deleteKeyButtonLabel, isKeyEditable && isKeyUnlocked, out deleteKeyButton);
            root.Add(deleteKeyButtonRow);
            
            // Assign callbacks
            contentsTextField.RegisterValueChangedCallback(changeEvent => OnContentsChanged(localizationTableType, changeEvent.newValue));
            keyTextField.RegisterValueChangedCallback(changeEvent => OnKeyChanged(property, localizationTableType, changeEvent.newValue));
            lockToggle.RegisterValueChangedCallback(evt =>
            {
                isKeyUnlocked = evt.newValue;
                Label toggleLabel = lockToggleRow.Q<Label>();
                if (toggleLabel != null) { toggleLabel.text = isKeyUnlocked ? _unlockLabel : _lockLabel; }
                keyTextField.SetEnabled(isKeyEditable && isKeyUnlocked);
                newKeyButton.SetEnabled(isKeyEditable && isKeyUnlocked);
                ReconcileDeleteButtonState(property, localizationTableType, isKeyEditable && isKeyUnlocked);
            });
            newKeyButton.RegisterCallback<ClickEvent>(_ => HandleNewKeyButtonClick(property, localizationTableType, fieldInfo.DeclaringType, sanitizedPropertyName));
            deleteKeyButton.RegisterCallback<ClickEvent>(_ => HandleDeleteButtonClick(property, localizationTableType));
            
            return root;
        }
        #endregion
        
        #region UtilityMethodsAndCallbacks
        private static bool IsKeyEmpty(LocalizedString localizedString, LocalizationTableType localizationTableType, out TableEntryReference tableEntryReference)
        {
            tableEntryReference = new TableEntryReference();
            if (localizedString.IsEmpty) { return true; }
            
            string currentKey = LocalizationTool.ResolveKeyName(localizationTableType, localizedString, out tableEntryReference);
            return tableEntryReference.ReferenceType == TableEntryReference.Type.Empty || string.IsNullOrWhiteSpace(currentKey);
        }
        
        private static bool HasPrefab(SerializedProperty property)
        {
            Object targetObject = property.serializedObject.targetObject;
            if (targetObject is not Component targetComponent) { return false; }
            if (PrefabUtility.IsPartOfPrefabAsset(targetComponent)) { return PrefabUtility.GetCorrespondingObjectFromSource(targetComponent) != null; }
            if (PrefabUtility.IsPartOfPrefabInstance(targetComponent)) { return IsPropertyOverridden(property); }
            return false;
        }
        
        private static bool IsPropertyOverridden(SerializedProperty property)
        {
            Object targetObject = property.serializedObject.targetObject;
            var component = targetObject as Component;
            if (targetObject == null || component == null) { return false; }
            
            Object prefabSource = PrefabUtility.GetCorrespondingObjectFromSource(component);
            if (prefabSource == null) { return false; }

            PropertyModification[] propertyModifications = PrefabUtility.GetPropertyModifications(component.gameObject);
            return propertyModifications != null && propertyModifications.Any(mod => mod.target == prefabSource && mod.propertyPath.StartsWith(property.propertyPath));
        }
        
        private static void SetKeyFromLocalization(LocalizedString localizedString, LocalizationTableType localizationTableType, TextField textField, bool isEnabled, bool shouldNotify)
        {
            if (textField == null) { return; }
            string keyValue = LocalizationTool.ResolveKeyName(localizationTableType, localizedString, out TableEntryReference _);

            if (shouldNotify) { textField.value = keyValue; }
            else { textField.SetValueWithoutNotify(keyValue); }
            textField.SetEnabled(isEnabled);
        }

        private static void SetContentsFromLocalization(LocalizedString localizedString, LocalizationTableType localizationTableType, TextField contentsTextField, bool shouldNotify)
        {
            if (contentsTextField == null) { return; }
            bool isKeyCurrentlyEmpty = IsKeyEmpty(localizedString, localizationTableType, out TableEntryReference tableEntryReference);
            string contentsValue = LocalizationTool.GetEnglishEntry(localizationTableType, tableEntryReference);
            
            if (shouldNotify) { contentsTextField.value = contentsValue; }
            else { contentsTextField.SetValueWithoutNotify(contentsValue); }
            DisableContentsForEmptyKey(contentsTextField, isKeyCurrentlyEmpty);
        }
        
        private void OnContentsChanged(LocalizationTableType localizationTableType, string newContents)
        {
            if (localizedString == null) { return; }
            TableEntryReference tableEntryReference = localizedString.TableEntryReference;
            if (tableEntryReference.ReferenceType == TableEntryReference.Type.Empty) { return; }
            
            string oldContents = LocalizationTool.GetEnglishEntry(localizationTableType, tableEntryReference);
            if (newContents == oldContents) { return; }
            
            LocalizationTool.AddUpdateEnglishEntry(localizationTableType, tableEntryReference, newContents);
        }
        
        private void OnKeyChanged(SerializedProperty property, LocalizationTableType localizationTableType, string newKey)
        {
            Object targetObject = property.serializedObject.targetObject;
            if (localizedString == null || targetObject == null) { return; }
            
            string oldKey = LocalizationTool.ResolveKeyName(localizationTableType, localizedString, out TableEntryReference tableEntryReference);
            if (newKey == oldKey || string.IsNullOrWhiteSpace(newKey)) { return; }
            
            if (!LocalizationTool.MakeOrRenameKey(localizationTableType, tableEntryReference, newKey)) { return; }
            if (!LocalizationTool.SafelyUpdateReference(localizationTableType, localizedString, newKey)) { return; }

            Undo.RecordObject(targetObject, "Bind localized string to updated key");
            property.boxedValue = localizedString;
            property.serializedObject.ApplyModifiedProperties();
            property.serializedObject.Update();
            
            bool isKeyCurrentlyEmpty = IsKeyEmpty(localizedString, localizationTableType, out _);
            DisableContentsForEmptyKey(contentsTextField, isKeyCurrentlyEmpty);
        }
        
        private void HandleNewKeyButtonClick(SerializedProperty property, LocalizationTableType localizationTableType, Type declaringType, string propertyName)
        {
            Object targetObject = property.serializedObject.targetObject;
            if (localizedString == null || targetObject == null) { return; }

            LocalizationTool.ResolveKeyName( localizationTableType, localizedString, out TableEntryReference currentTableEntryReference);
            string currentContents = "";
            if (currentTableEntryReference.ReferenceType != TableEntryReference.Type.Empty)
            {
                currentContents = LocalizationTool.GetEnglishEntry(localizationTableType, currentTableEntryReference);
            }

            string newKey = LocalizationTool.GenerateKindaUniqueKey(declaringType, targetObject, propertyName);
            if (!LocalizationTool.AddUpdateEnglishEntry(localizationTableType, newKey, currentContents)) { return; }
            if (!LocalizationTool.SafelyUpdateReference(localizationTableType, localizedString, newKey)) { return; }
            
            Undo.RecordObject(targetObject, "Bind localized string to new key");
            property.boxedValue = localizedString;
            property.serializedObject.ApplyModifiedProperties();
            property.serializedObject.Update();

            keyTextField?.SetValueWithoutNotify(newKey);
            bool isKeyCurrentlyEmpty = IsKeyEmpty(localizedString, localizationTableType, out _);
            DisableContentsForEmptyKey(contentsTextField, isKeyCurrentlyEmpty);
            ReconcileDeleteButtonState(property, localizationTableType, !isKeyCurrentlyEmpty);
        }

        private void HandleDeleteButtonClick(SerializedProperty property, LocalizationTableType localizationTableType)
        {
            Object targetObject = property.serializedObject.targetObject;
            if (localizedString == null || targetObject == null) { return; }
            
            if (string.IsNullOrWhiteSpace(keyTextField.value)) { return; }
            LocalizationTool.RemoveEntry(localizationTableType, keyTextField.value);
            localizedString.SetReference("", "");
            property.serializedObject.Update();

            if (HasPrefab(property))
            {
                TryResetToPrefab(property);
                SetKeyFromLocalization(localizedString, localizationTableType, keyTextField, true, false);
                SetContentsFromLocalization(localizedString, localizationTableType, contentsTextField, false);
            }
            else
            {
                keyTextField?.SetValueWithoutNotify("");
                contentsTextField?.SetValueWithoutNotify("");
            }

            bool isKeyCurrentlyEmpty = IsKeyEmpty(localizedString, localizationTableType, out _);
            lockToggle.value = false;
            DisableContentsForEmptyKey(contentsTextField, isKeyCurrentlyEmpty);
        }

        private void ReconcileDeleteButtonState(SerializedProperty property, LocalizationTableType localizationTableType, bool isEnabled)
        {
            if (deleteKeyButton == null) { return; }
            if (!isEnabled || localizedString == null || localizedString.IsEmpty || IsKeyEmpty(localizedString, localizationTableType, out _))
            {
                deleteKeyButton.SetEnabled(false); 
                return;
            }
            if (!HasPrefab(property) || IsPropertyUniqueFromPrefab(property, localizationTableType))
            {
                deleteKeyButton.SetEnabled(true); 
                return;
            }
        }

        private void TryResetToPrefab(SerializedProperty property)
        {
            Object targetObject = property.serializedObject.targetObject;
            var targetComponent = targetObject as Component;
            if (targetObject == null || targetComponent == null) { return; }
            
            if (PrefabUtility.IsPartOfPrefabInstance(targetComponent))
            {
                Undo.RecordObject(targetObject, "Reset localized string to prefab value");
                PrefabUtility.RevertPropertyOverride(property, InteractionMode.UserAction);
            }
            else if (PrefabUtility.IsPartOfPrefabAsset(targetComponent))
            {
                Object prefabSource = PrefabUtility.GetCorrespondingObjectFromSource(targetComponent);
                if (prefabSource == null) { return; }

                using var prefabSerializedObject = new SerializedObject(prefabSource);
                SerializedProperty prefabProperty = prefabSerializedObject.FindProperty(property.propertyPath);
                if (prefabProperty == null) { return; }

                Undo.RecordObject(targetObject, "Reset localized string to prefab value");
                property.serializedObject.CopyFromSerializedPropertyIfDifferent(prefabProperty);
                property.serializedObject.ApplyModifiedProperties();
            }
            else { return; }
            property.serializedObject.Update();
            localizedString = property.boxedValue as LocalizedString;
        }

        private bool IsPropertyUniqueFromPrefab(SerializedProperty property, LocalizationTableType localizationTableType)
        {
            Object targetObject = property.serializedObject.targetObject;
            var targetComponent = targetObject as Component;
            // Unexpected Edge Cases
            if (targetObject == null || targetComponent == null || localizedString == null) { return false; } 
            TableEntryReference targetTableEntryReference = LocalizationTool.GetTableEntryReferencedByID(localizationTableType, localizedString.TableEntryReference);
            if (targetTableEntryReference.ReferenceType != TableEntryReference.Type.Id) { return false; }
            
            // Check for prefab existence
            Object prefabSource = PrefabUtility.GetCorrespondingObjectFromSource(targetComponent);
            if (prefabSource == null) { return true; } // No prefab source found
            using var prefabSerializedObject = new SerializedObject(prefabSource);
            SerializedProperty prefabProperty = prefabSerializedObject.FindProperty(property.propertyPath);
            if (prefabProperty == null) { return true; } // No prefab property found
            
            TableEntryReference prefabTableEntryReference = LocalizationTool.GetSerializedTableEntryKeyID(localizationTableType, prefabProperty);
            if (prefabTableEntryReference.ReferenceType != TableEntryReference.Type.Id) { return true; }
            
            // Match to ID -- if different, allow for deletion (since unique entry on target)
            return targetTableEntryReference.KeyId != prefabTableEntryReference.KeyId;
        }
        #endregion
        
        #region RowBuilders
        private static VisualElement BuildKeyRow(LocalizedString localizedString, LocalizationTableType localizationTableType, bool isEnabled, out TextField keyTextField)
        {
            VisualElement keyRow = MakeLabeledRow(_keyLabel, out keyTextField);
            SetKeyFromLocalization(localizedString, localizationTableType, keyTextField, isEnabled, true);
            keyTextField.isDelayed = true;
            return keyRow;
        }
        
        private static VisualElement BuildLockToggleRow(bool isKeyEditable, bool isKeyUnlocked, out Toggle lockToggle)
        {
            VisualElement lockToggleRow = MakeLockToggleBaseRow();
            lockToggle = MakeToggle(isKeyUnlocked);
            lockToggle.SetEnabled(isKeyEditable);

            lockToggleRow.Add(lockToggle);
            return lockToggleRow;
        }

        private static VisualElement BuildContentsRow(LocalizedString localizedString, LocalizationTableType localizationTableType, out TextField contentsTextField)
        {
            VisualElement contentsRow = MakeLabeledRow(_textLabel, out contentsTextField);
            SetContentsFromLocalization(localizedString, localizationTableType, contentsTextField, true);
            contentsTextField.isDelayed = true;

            return contentsRow;
        }

        private static VisualElement BuildButtonRow(string buttonLabel, bool isEnabled, out Button button)
        {
            VisualElement buttonRow = MakeButtonBaseRow();
            button = new Button
            {
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
        
        private static VisualElement MakeLabeledRow(string labelText, out TextField textField)
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

        private static Toggle MakeToggle(bool isUnlocked)
        {
            return new Toggle
            {
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
#endif

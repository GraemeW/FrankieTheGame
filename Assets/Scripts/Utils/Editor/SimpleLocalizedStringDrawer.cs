#if UNITY_EDITOR
using System;
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
        private bool isKeyUnlocked;
        private TextField keyTextField;
        private TextField contentsTextField;
        
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
            var simpleLocalizedStringAttribute = (SimpleLocalizedStringAttribute)attribute;
            LocalizationTableType localizationTableType = simpleLocalizedStringAttribute.localizationTableType;
            bool isKeyEditable = simpleLocalizedStringAttribute.isKeyEditable;
            string sanitizedPropertyName = property.name.Replace("localized","");
            string niceSanitizedPropertyName = property.displayName.Replace("Localized", "");
            isKeyUnlocked = false;
            
            LocalizationTool.InitializeEnglishLocale();
            
            if (property.boxedValue is not LocalizedString localizedString) { return MakeErrorBox("Property is not LocalizedString."); }
            if (!LocalizationTool.GetOrMakeTableCollection(localizationTableType, out StringTableCollection _)) { return MakeErrorBox($"Could not find or create StringTableCollection of type: '{localizationTableType}'."); }

            VisualElement root = MakeRoot(niceSanitizedPropertyName);
            VisualElement keyRow = BuildKeyRow(localizedString, localizationTableType, isKeyEditable, isKeyUnlocked, out keyTextField);
            root.Add(keyRow);
            VisualElement lockToggleRow = BuildLockToggleRow(isKeyEditable, isKeyUnlocked, out Toggle lockToggle); 
            root.Add(lockToggleRow);
            VisualElement contentsRow =  BuildContentsRow(localizedString, localizationTableType, out contentsTextField);
            root.Add(contentsRow);
            VisualElement newKeyButtonRow = BuildButtonRow(_newKeyButtonLabel, isKeyEditable && isKeyUnlocked, out Button newKeyButton); 
            root.Add(newKeyButtonRow);
            VisualElement deleteKeyButtonRow = BuildButtonRow(_deleteKeyButtonLabel, isKeyEditable && isKeyUnlocked, out Button deleteKeyButton);
            root.Add(deleteKeyButtonRow);
            contentsTextField.RegisterValueChangedCallback(changeEvent => OnContentsChanged(localizationTableType, localizedString.TableEntryReference, changeEvent.newValue));
            keyTextField.RegisterValueChangedCallback(changeEvent => OnKeyChanged(property, localizedString, localizationTableType, changeEvent.newValue));
            lockToggle.RegisterValueChangedCallback(evt =>
            {
                isKeyUnlocked = evt.newValue;
                Label toggleLabel = lockToggleRow.Q<Label>();
                if (toggleLabel != null) { toggleLabel.text = isKeyUnlocked ? _unlockLabel : _lockLabel; }
                keyTextField.SetEnabled(isKeyEditable && isKeyUnlocked);
                newKeyButton.SetEnabled(isKeyEditable && isKeyUnlocked);
                deleteKeyButton.SetEnabled(isKeyEditable && isKeyUnlocked);
            });
            
            newKeyButton.RegisterCallback<ClickEvent>(_ => HandleNewKeyButtonClick(property, localizedString, localizationTableType, fieldInfo.DeclaringType, property.serializedObject.targetObject, sanitizedPropertyName));
            deleteKeyButton.RegisterCallback<ClickEvent>(_ => HandleDeleteButtonClick(localizedString, localizationTableType));
            
            return root;
        }
        #endregion
        
        #region UtilityMethodsAndCallbacks
        private static void OnContentsChanged(LocalizationTableType localizationTableType, TableEntryReference tableEntryReference, string newContents)
        {
            string oldContents = LocalizationTool.GetEnglishEntry(localizationTableType, tableEntryReference);
            if (newContents == oldContents) { return; }
            LocalizationTool.AddUpdateEnglishEntry(localizationTableType, tableEntryReference, newContents);
        }

        private static bool IsKeyEmpty(LocalizedString localizedString, LocalizationTableType localizationTableType, out TableEntryReference tableEntryReference)
        {
            string currentKey = LocalizationTool.ResolveKeyName(localizationTableType, localizedString, out tableEntryReference);
            return tableEntryReference.ReferenceType == TableEntryReference.Type.Empty || string.IsNullOrWhiteSpace(currentKey);
        }
        
        private void OnKeyChanged(SerializedProperty property, LocalizedString localizedString, LocalizationTableType localizationTableType, string newKey)
        {
            string oldKey = LocalizationTool.ResolveKeyName(localizationTableType, localizedString, out TableEntryReference tableEntryReference);
            if (newKey == oldKey || string.IsNullOrWhiteSpace(newKey)) { return; }
            
            if (!LocalizationTool.MakeOrRenameKey(localizationTableType, tableEntryReference, newKey)) { return; }
            if (!LocalizationTool.SafelyUpdateReference(localizationTableType, localizedString, newKey)) { return; }

            property.boxedValue = localizedString;
            property.serializedObject.ApplyModifiedProperties();
            
            bool isKeyCurrentlyEmpty = IsKeyEmpty(localizedString, localizationTableType, out _);
            SetContentsEnabled(contentsTextField, isKeyCurrentlyEmpty);
        }
        
        private void HandleNewKeyButtonClick(SerializedProperty property, LocalizedString localizedString, LocalizationTableType localizationTableType, Type declaringType, Object parentObject, string propertyName)
        {
            if (localizedString == null) { return; }

            LocalizationTool.ResolveKeyName( localizationTableType, localizedString, out TableEntryReference currentTableEntryReference);
            string currentContents = "";
            if (currentTableEntryReference.ReferenceType != TableEntryReference.Type.Empty)
            {
                currentContents = LocalizationTool.GetEnglishEntry(localizationTableType, currentTableEntryReference);
            }

            string newKey = LocalizationTool.GenerateKindaUniqueKey(declaringType, parentObject, propertyName);
            if (!LocalizationTool.AddUpdateEnglishEntry(localizationTableType, newKey, currentContents)) { return; }
            if (!LocalizationTool.SafelyUpdateReference(localizationTableType, localizedString, newKey)) { return; }
            
            property.boxedValue = localizedString;
            property.serializedObject.ApplyModifiedProperties();

            keyTextField?.SetValueWithoutNotify(newKey);
            bool isKeyCurrentlyEmpty = IsKeyEmpty(localizedString, localizationTableType, out _);
            SetContentsEnabled(contentsTextField, isKeyCurrentlyEmpty);
        }

        private void HandleDeleteButtonClick(LocalizedString localizedString, LocalizationTableType localizationTableType)
        {
            if (string.IsNullOrWhiteSpace(keyTextField.value)) { return; }
            LocalizationTool.RemoveEntry(localizationTableType, keyTextField.value);
            localizedString.SetReference("", "");
            
            keyTextField?.SetValueWithoutNotify("");
            bool isKeyCurrentlyEmpty = IsKeyEmpty(localizedString, localizationTableType, out _);
            SetContentsEnabled(contentsTextField, isKeyCurrentlyEmpty);
        }
        #endregion
        
        #region RowBuilders
        private static VisualElement BuildKeyRow(LocalizedString localizedString, LocalizationTableType localizationTableType, bool isKeyEditable, bool isKeyUnlocked, out TextField keyTextField)
        {
            VisualElement keyRow = MakeLabeledRow(_keyLabel, out keyTextField);
            
            string oldKey = LocalizationTool.ResolveKeyName(localizationTableType, localizedString, out TableEntryReference _);
            keyTextField.value = oldKey;
            keyTextField.isDelayed = true;
            keyTextField.SetEnabled(isKeyEditable && isKeyUnlocked);

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

            bool isKeyCurrentlyEmpty = IsKeyEmpty(localizedString, localizationTableType, out TableEntryReference tableEntryReference);
            string oldText = LocalizationTool.GetEnglishEntry(localizationTableType, tableEntryReference);
            contentsTextField.value = oldText;
            contentsTextField.isDelayed = true;
            SetContentsEnabled(contentsTextField, isKeyCurrentlyEmpty);

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

        private static void SetContentsEnabled(TextField contentsTextField, bool isKeyCurrentlyEmpty)
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

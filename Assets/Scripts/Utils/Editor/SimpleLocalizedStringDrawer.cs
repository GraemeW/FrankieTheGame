#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using UnityEngine.UIElements;

namespace Frankie.Utils.Editor
{
    [CustomPropertyDrawer(typeof(SimpleLocalizedStringAttribute))]
    public class SimpleLocalizedStringDrawer : PropertyDrawer
    {
        #region UIProperties
        private const string _keyLabel = "Key";
        private const string _textLabel = "Content";
        private const string _newKeyButtonLabel = "Make New Key : StringTable Entry";
        private const string _lockLabel = "🔒";
        private const string _unlockLabel = "🔓";
        private const string _lockTooltip = "Unlock to allow editing the localization key.";
        
        private const int _labelFontSize = 12;
        private const int _headerFontSize = 13;
        private static readonly Color _errorTextColour = new(0.9f, 0.3f, 0.3f);
        private static readonly Color _disabledTextColour = new(0.5f, 0.5f, 0.5f);
        
        private const float _labelWidth = 56f;
        private const float _buttonWidth = 220f;
        private const float _rowHeight = 20f;
        private const float _lockToggleHeight = 20f;
        private const float _lockToggleLabelWidth = 20f;
        private const int _rowSpacingTop  = 2;
        private const int _rowSpacingBottom  = 2;
        private const int _sectionPaddingLeft = 4;
        #endregion
        
        #region UnityMethods
        private bool isKeyUnlocked;

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var simpleLocalizedStringAttribute = (SimpleLocalizedStringAttribute)attribute;
            LocalizationTableType localizationTableType = simpleLocalizedStringAttribute.localizationTableType;
            bool isKeyEditable = simpleLocalizedStringAttribute.isKeyEditable;
            isKeyUnlocked = false;
            
            if (property.boxedValue is not LocalizedString localizedString) { return MakeErrorBox("Property is not a LocalizedString."); }
            if (!LocalizationTool.GetOrMakeTableCollection(localizationTableType, out StringTableCollection _)) { return MakeErrorBox($"Could not find or create StringTableCollection of type: '{localizationTableType}'."); }


            VisualElement root = MakeRoot(property.displayName);
            (VisualElement keyRow, TextField keyTextField) = BuildKeyRow(localizedString, localizationTableType, isKeyEditable, isKeyUnlocked);
            root.Add(keyRow);
            (VisualElement lockToggleRow, Toggle lockToggle) = BuildLockToggleRow(isKeyEditable, isKeyUnlocked); 
            root.Add(lockToggleRow);
            (VisualElement contentsRow, TextField contentsTextField) =  BuildContentsRow(localizedString, localizationTableType);
            root.Add(contentsRow);
            VisualElement newKeyButtonRow = BuildNewKeyButtonRow(property, localizedString, localizationTableType, isKeyEditable && isKeyUnlocked); 
            root.Add(newKeyButtonRow);
            
            contentsTextField.RegisterValueChangedCallback(changeEvent => OnContentsChanged(localizationTableType, localizedString.TableEntryReference, changeEvent.newValue));
            keyTextField.RegisterValueChangedCallback(changeEvent => OnKeyChanged(property, localizedString, localizationTableType, changeEvent.newValue, contentsTextField));
            lockToggle.RegisterValueChangedCallback(evt =>
            {
                isKeyUnlocked = evt.newValue;
                Label toggleLabel = lockToggleRow.Q<Label>();
                if (toggleLabel != null) { toggleLabel.text = isKeyUnlocked ? _unlockLabel : _lockLabel; }
                keyTextField.SetEnabled(isKeyEditable && isKeyUnlocked);
            });
            
            return root;
        }
        #endregion
        
        #region RowBuilders
        private static (VisualElement keyRow, TextField keyTextField) BuildKeyRow(LocalizedString localizedString, LocalizationTableType localizationTableType, bool isKeyEditable, bool isKeyUnlocked)
        {
            (VisualElement keyRow, TextField keyTextField) = MakeLabeledRow(_keyLabel);
            
            string oldKey = LocalizationTool.ResolveKeyName(localizationTableType, localizedString, out TableEntryReference _);
            keyTextField.value = oldKey;
            keyTextField.isDelayed = true;
            keyTextField.SetEnabled(isKeyEditable && isKeyUnlocked);

            return (keyRow, keyTextField);
        }
        
        private static (VisualElement lockToggleRow, Toggle lockToggle) BuildLockToggleRow(bool isKeyEditable, bool isKeyUnlocked)
        {
            VisualElement lockToggleRow = MakeLockToggleBaseRow();
            Toggle lockToggle = MakeToggle(isKeyUnlocked);
            lockToggle.SetEnabled(isKeyEditable);

            lockToggleRow.Add(lockToggle);
            return (lockToggleRow, lockToggle);
        }

        private static (VisualElement contentsRow, TextField contentsTextField) BuildContentsRow(LocalizedString localizedString, LocalizationTableType localizationTableType)
        {
            (VisualElement contentsRow, TextField contentsTextField) = MakeLabeledRow(_textLabel);

            bool isKeyCurrentlyEmpty = IsKeyEmpty(localizedString, localizationTableType, out TableEntryReference tableEntryReference);
            string oldText = LocalizationTool.GetEnglishEntry(localizationTableType, tableEntryReference);
            contentsTextField.value = oldText;
            contentsTextField.isDelayed = true;
            SetContentsEnabled(contentsTextField, isKeyCurrentlyEmpty);

            return (contentsRow, contentsTextField);
        }

        private static VisualElement BuildNewKeyButtonRow(SerializedProperty property, LocalizedString localizedString, LocalizationTableType localizationTableType, bool isEnabled)
        {
            VisualElement buttonBaseRow = MakeButtonBaseRow();
            var button = new Button(() => HandleNewKeyButtonClick(property, localizedString, localizationTableType))
            {
                text = _newKeyButtonLabel,
                style = { width = _buttonWidth }
            };
            button.SetEnabled(isEnabled);

            buttonBaseRow.Add(button);
            return buttonBaseRow;
        }

        private static void SetContentsEnabled(TextField contentsTextField, bool isKeyCurrentlyEmpty)
        {
            contentsTextField.SetEnabled(!isKeyCurrentlyEmpty);
            if (isKeyCurrentlyEmpty) { contentsTextField.style.color = new StyleColor(_disabledTextColour); }
        }
        #endregion
        
        #region ButtonHandlers
        private static void HandleNewKeyButtonClick(
            SerializedProperty property,
            LocalizedString localizedString,
            LocalizationTableType localizationTableType)
        {
            if (localizedString == null) { return; }

            LocalizationTool.ResolveKeyName( localizationTableType, localizedString, out TableEntryReference currentTableEntryReference);

            string currentContents = "";
            if (currentTableEntryReference.ReferenceType != TableEntryReference.Type.Empty)
            {
                currentContents = LocalizationTool.GetEnglishEntry(localizationTableType, currentTableEntryReference);
            }

            string newKey = System.Guid.NewGuid().ToString();
            if (!LocalizationTool.AddUpdateEnglishEntry(localizationTableType, newKey, currentContents)) { return; }
            if (!LocalizationTool.SafelyUpdateReference(localizationTableType, localizedString, newKey)) { return; }

            property.boxedValue = localizedString;
            property.serializedObject.ApplyModifiedProperties();
        }
        #endregion
        
        #region Callbacks
        private static void OnKeyChanged(SerializedProperty property, LocalizedString localizedString, LocalizationTableType localizationTableType, string newKey, TextField contentsTextField)
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
        
        private static (VisualElement row, TextField field) MakeLabeledRow(string labelText)
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

            var field = new TextField
            {
                isDelayed = true,
                style = { flexGrow = 1 }
            };

            labeledRow.Add(label);
            labeledRow.Add(field);
            return (labeledRow, field);
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

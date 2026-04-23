#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

namespace Frankie.Utils.Editor
{
    [CustomPropertyDrawer(typeof(SimpleLocalizedStringAttribute))]
    public class SimpleLocalizedStringDrawer : PropertyDrawer
    {
        // Editor Properties
        private const string _keyLabel = "Key";
        private const string _textLabel = "Content";
        private const float _rowHeight  = 20f;
        private const float _rowSpacing = 2f;
        private const float _labelWidth = 56f;

        #region UnityMethods
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) => (_rowHeight + _rowSpacing) * 3 + _rowSpacing;
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var simpleLocalizedStringAttribute = (SimpleLocalizedStringAttribute)attribute;
            LocalizationTableType localizationTableType = simpleLocalizedStringAttribute.localizationTableType;
            if (property.boxedValue is not LocalizedString localizedString)
            {
                EditorGUI.HelpBox(position, "Property is not a LocalizedString.", MessageType.Error);
                return;
            }
            
            if (!LocalizationTool.GetOrMakeTableCollection(localizationTableType, out StringTableCollection _))
            {
                EditorGUI.HelpBox(position, $"Could not find or create StringTableCollection of type: '{simpleLocalizedStringAttribute.localizationTableType}'.", MessageType.Error);
                return;
            }

            EditorGUI.BeginProperty(position, label, property);
            
            var headerRow = new Rect(position.x, position.y + _rowSpacing, position.width, _rowHeight);
            EditorGUI.LabelField(headerRow, label, EditorStyles.boldLabel);
            
            var keyRow = new Rect(position.x, headerRow.yMax + _rowSpacing, position.width, _rowHeight);
            DrawKeyRow(keyRow, property, localizedString, localizationTableType);
            
            var textRow = new Rect(position.x, keyRow.yMax + _rowSpacing, position.width, _rowHeight);
            DrawTextRow(textRow, localizedString, localizationTableType);

            EditorGUI.EndProperty();
        }
        #endregion

        #region DrawMethods
        private static void DrawKeyRow(Rect keyRow, SerializedProperty property, LocalizedString localizedString, LocalizationTableType localizationTableType)
        {
            if (localizedString == null) { return; }
            
            var labelRect = new Rect(keyRow.x, keyRow.y, _labelWidth, keyRow.height);
            var fieldRect = new Rect(keyRow.x + _labelWidth, keyRow.y, keyRow.width - _labelWidth, keyRow.height);
            EditorGUI.LabelField(labelRect, _keyLabel);
            
            // Use DelayedTextField to only when the user commits (i.e. hits return || focus-loss)
            string oldKey = LocalizationTool.ResolveKeyName(localizationTableType, localizedString, out TableEntryReference tableEntryReference);
            string newKey = EditorGUI.DelayedTextField(fieldRect, oldKey);
            if (newKey == oldKey || string.IsNullOrWhiteSpace(newKey)) { return; }
            
            if (!LocalizationTool.MakeOrRenameKey(localizationTableType, tableEntryReference, newKey)) { return; }
            if (!LocalizationTool.SafelyUpdateReference(localizationTableType, localizedString, newKey)) { return; }
            property.boxedValue = localizedString;
            property.serializedObject.ApplyModifiedProperties();
        }
        
        private static void DrawTextRow(Rect textRow, LocalizedString localizedString, LocalizationTableType localizationTableType)
        {
            var labelRect = new Rect(textRow.x, textRow.y, _labelWidth, textRow.height);
            var fieldRect = new Rect(textRow.x + _labelWidth, textRow.y, textRow.width - _labelWidth, textRow.height);

            EditorGUI.LabelField(labelRect, _textLabel);

            string currentKey = LocalizationTool.ResolveKeyName(localizationTableType, localizedString, out TableEntryReference tableEntryReference); 
            bool keyIsEmpty = tableEntryReference.ReferenceType == TableEntryReference.Type.Empty || string.IsNullOrWhiteSpace(currentKey);
            
            string oldText = LocalizationTool.GetEnglishEntry(localizationTableType, tableEntryReference);
            using (new EditorGUI.DisabledScope(keyIsEmpty)) // Disable control if key is invalid
            {
                // Use DelayedTextField to only when the user commits (i.e. hits return || focus-loss)
                string newText = EditorGUI.DelayedTextField(fieldRect, oldText);
                if (!keyIsEmpty && newText != oldText)
                {
                    LocalizationTool.AddUpdateEnglishEntry(localizationTableType, localizedString.TableEntryReference, newText);
                }
            }
        }
        #endregion
    }
}
#endif

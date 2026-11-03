using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace LowDefMustard.Core.Editor
{
    [CustomEditor(typeof(LDMLogSettings))]
    public sealed class LDMLogSettingsEditor : UnityEditor.Editor
    {
        // Const Tunables
        private const float _categoryLabelWidth = 120f;
        private const float _sectionSpacing = 6f;

        // State
        private SerializedProperty categoryEntriesProperty;

        #region UnityMethods
        public override VisualElement CreateInspectorGUI()
        {
            categoryEntriesProperty = serializedObject.FindProperty(LDMLogSettings.categoryEntriesField);

            var root = new VisualElement();
            root.Add(BuildHeader("Categories"));
            root.Add(BuildToggleAllButtons());
            for (int i = 0; i < categoryEntriesProperty.arraySize; i++) { root.Add(BuildCategoryRow(categoryEntriesProperty.GetArrayElementAtIndex(i))); }

            root.Add(BuildHeader("Stack Traces"));
            root.Add(new PropertyField(serializedObject.FindProperty(LDMLogSettings.logStackTraceField), "Log (Info / Important)"));
            root.Add(new PropertyField(serializedObject.FindProperty(LDMLogSettings.warningStackTraceField), "Warning"));
            root.Add(new PropertyField(serializedObject.FindProperty(LDMLogSettings.errorStackTraceField), "Error / Assert"));
            root.Add(new PropertyField(serializedObject.FindProperty(LDMLogSettings.exceptionStackTraceField), "Exception"));
            return root;
        }
        #endregion

        #region GUI
        private static Label BuildHeader(string text)
        {
            return new Label(text)
            {
                style =
                {
                    unityFontStyleAndWeight = UnityEngine.FontStyle.Bold,
                    marginTop = _sectionSpacing
                }
            };
        }

        private VisualElement BuildToggleAllButtons()
        {
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            row.Add(new Button(() => SetAllEnabled(true)) { text = "Enable All" });
            row.Add(new Button(() => SetAllEnabled(false)) { text = "Disable All" });
            return row;
        }

        private static VisualElement BuildCategoryRow(SerializedProperty entryProperty)
        {
            var category = (LDMLogCategory)entryProperty.FindPropertyRelative(LDMLogCategoryEntry.categoryField).enumValueFlag;
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row } };

            var enabledToggle = new Toggle();
            enabledToggle.BindProperty(entryProperty.FindPropertyRelative(LDMLogCategoryEntry.enabledField));
            row.Add(enabledToggle);

            var label = new Label(category.ToString()) { style = { width = _categoryLabelWidth, unityTextAlign = UnityEngine.TextAnchor.MiddleLeft } };
            row.Add(label);

            var colorField = new ColorField { showAlpha = false, style = { flexGrow = 1f } };
            colorField.BindProperty(entryProperty.FindPropertyRelative(LDMLogCategoryEntry.colorField));
            row.Add(colorField);
            return row;
        }
        #endregion

        #region PrivateMethods
        private void SetAllEnabled(bool enabled)
        {
            serializedObject.Update();
            for (int i = 0; i < categoryEntriesProperty.arraySize; i++)
            {
                categoryEntriesProperty.GetArrayElementAtIndex(i).FindPropertyRelative(LDMLogCategoryEntry.enabledField).boolValue = enabled;
            }
            serializedObject.ApplyModifiedProperties();
        }
        #endregion
    }
}

using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Frankie.Combat.Editor
{
    public static class BattleActionWindowStyles
    {
        // Notes:
        //  - Colours live in BattleActionWindow.uss (this sets layout + the classes it styles)

        // Const Tunables
        public const float columnPadding = 6f;
        public const float sectionSpacing = 8f;
        public const float rowHeight = 22f;
        public const float smallButtonWidth = 22f;
        private const string _scriptField = "m_Script";
        public const string pingTooltip = "Show the asset in the project window";
        private const string _pingText = "Ping";

        // Classes
        public const string windowClass = "bae-window";
        private const string _libraryColumnClass = "bae-column--library";
        private const string _dependentColumnClass = "bae-column--dependent";
        private const string _columnHeaderClass = "bae-column-header";
        private const string _columnHeaderTitleClass = "bae-column-header__title";
        private const string _sectionHeaderClass = "bae-section-header";
        private const string _sectionHeaderTitleClass = "bae-section-header__title";
        private const string _cardClass = "bae-card";
        private const string _hintClass = "bae-hint";
        private const string _dimClass = "bae-dim";
        private const string _dividerClass = "bae-divider";
        private const string _treeHeaderClass = "bae-tree-header";
        private const string _missingClass = "bae-missing";

        #region PublicMethods
        public static void ApplyDependentColumn(VisualElement column) => column.AddToClassList(_dependentColumnClass);
        public static void ApplyLibraryColumn(VisualElement column) => column.AddToClassList(_libraryColumnClass);
        public static void ApplyTreeHeader(VisualElement element, bool isHeader) => element.EnableInClassList(_treeHeaderClass, isHeader);
        public static void SetMissing(VisualElement element, bool isMissing) => element.EnableInClassList(_missingClass, isMissing);
        
        public static ScrollView CreateColumnScrollView()
        {
            var scrollView = new ScrollView(ScrollViewMode.Vertical) { style = { flexGrow = 1f } };
            scrollView.contentContainer.style.paddingLeft = columnPadding;
            scrollView.contentContainer.style.paddingRight = columnPadding;
            scrollView.contentContainer.style.paddingBottom = columnPadding;
            return scrollView;
        }
        
        public static VisualElement CreateColumnHeader(string title, string subtitle, out Label titleLabel)
        {
            var header = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row, alignItems = Align.Center, flexShrink = 0f,
                    paddingLeft = columnPadding, paddingRight = columnPadding, paddingTop = 4f, paddingBottom = 4f, marginBottom = 4f
                }
            };
            header.AddToClassList(_columnHeaderClass);
            titleLabel = new Label(title) { style = { flexGrow = 1f, flexShrink = 1f, overflow = Overflow.Hidden, textOverflow = TextOverflow.Ellipsis } };
            titleLabel.AddToClassList(_columnHeaderTitleClass);
            header.Add(titleLabel);
            if (!string.IsNullOrEmpty(subtitle)) { header.Add(CreateDimLabel(subtitle)); }
            return header;
        }

        public static VisualElement CreateSectionHeader(string text, out Label textLabel)
        {
            var header = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row, alignItems = Align.Center, flexShrink = 0f,
                    marginTop = sectionSpacing, marginBottom = 2f, paddingLeft = 4f, paddingTop = 2f, paddingBottom = 2f
                }
            };
            header.AddToClassList(_sectionHeaderClass);
            textLabel = new Label(text) { style = { flexGrow = 1f, flexShrink = 1f, overflow = Overflow.Hidden, textOverflow = TextOverflow.Ellipsis } };
            textLabel.AddToClassList(_sectionHeaderTitleClass);
            header.Add(textLabel);
            return header;
        }

        public static Label CreateDimLabel(string text)
        {
            var label = new Label(text) { style = { flexShrink = 0f } };
            label.AddToClassList(_dimClass);
            return label;
        }

        public static VisualElement CreateCard()
        {
            var card = new VisualElement { style = { paddingLeft = 4f, paddingRight = 4f, paddingTop = 2f, paddingBottom = 4f, flexShrink = 0f } };
            card.AddToClassList(_cardClass);
            return card;
        }
        
        public static Button CreatePingButton(Object asset) => new(() => EditorGUIUtility.PingObject(asset)) { text = _pingText, tooltip = pingTooltip };
        
        public static Label CreateHint(string text)
        {
            var hint = new Label(text) { style = { marginTop = sectionSpacing / 2f, flexShrink = 0f } };
            hint.AddToClassList(_hintClass);
            return hint;
        }

        public static VisualElement CreateDivider()
        {
            var divider = new VisualElement { style = { flexShrink = 0f, marginTop = sectionSpacing, marginBottom = sectionSpacing } };
            divider.AddToClassList(_dividerClass);
            return divider;
        }

        public static VisualElement CreateFields(SerializedObject serializedObject, ICollection<string> skippedFields)
        {
            // Note:  Bound here ~ never Bind an ancestor of these afterwards (each section binds to its own object)
            var fields = new VisualElement();
            SerializedProperty property = serializedObject.GetIterator();
            for (bool hasProperty = property.NextVisible(true); hasProperty; hasProperty = property.NextVisible(false))
            {
                if (property.name == _scriptField || skippedFields.Contains(property.name)) { continue; }
                fields.Add(new PropertyField(property.Copy()));
            }
            fields.Bind(serializedObject);
            return fields;
        }
        #endregion
    }
}

#if UNITY_EDITOR
using System;
using System.Globalization;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace LowDefMustard.Rendering.Editor
{
    internal sealed class BattleEffectEntryRow : VisualElement
    {
        // Const Tunables
        internal const float kindWidth = 90f;
        internal const float startWidth = 52f;
        internal const float deleteWidth = 20f;
        internal const float columnGap = 6f;
        private const string _timeFormat = "0.###";
        private const string _noRecipeLabel = "No recipe";

        // Element References
        private readonly Label labelText;
        private readonly TextField labelField;
        private readonly DropdownField shapeField;
        private readonly Label recipeText;
        private readonly Label startText;
        private readonly FloatField startField;
        
        // State
        public int rowIndex { get; private set; } = -1;
        private SerializedProperty entryProperty;
        private BattleEffectEntryId entryId;
        private bool isStartEditable;
        private bool isSelected;
        private bool isHovered;

        // Constructor
        public BattleEffectEntryRow(Action<BattleEffectEntryId> deleteEntry)
        {
            // Row Styling
            style.flexDirection = FlexDirection.Row;
            style.alignItems = Align.Center;
            style.flexGrow = 1f;

            // Draw Elements
            labelText = CreateText(1f);
            labelField = new TextField { isDelayed = true, style = { flexGrow = 1f, flexShrink = 1f, display = DisplayStyle.None } };
            shapeField = new DropdownField(BattleEffectShapeTypes.GetNames(), 0) { style = { width = kindWidth, flexShrink = 0f } };
            
            recipeText = CreateText(0f);
            recipeText.style.width = kindWidth;
            recipeText.style.unityFontStyleAndWeight = FontStyle.Italic;
            
            startText = CreateText(0f);
            startText.style.width = startWidth;
            startText.style.marginLeft = columnGap;
            startText.style.paddingLeft = 0f;
            startText.style.unityTextAlign = TextAnchor.MiddleCenter;
            startField = new FloatField { isDelayed = true, style = { width = startWidth, marginLeft = columnGap, flexShrink = 0f, display = DisplayStyle.None } };
            
            var deleteButton = new Button(() => deleteEntry(entryId)) { text = "-", style = { width = deleteWidth, flexShrink = 0f } };
            
            Add(labelText);
            Add(labelField);
            Add(shapeField);
            Add(recipeText);
            Add(startText);
            Add(startField);
            Add(deleteButton);

            // Bindings and Callbacks
            RegisterCallback<PointerEnterEvent>(_ => SetHovered(true));
            RegisterCallback<PointerLeaveEvent>(_ => SetHovered(false));
            
            SetUpEditing(labelText, labelField, () => true);
            SetUpEditing(startText, startField, () => isStartEditable);
            shapeField.RegisterValueChangedCallback(changeEvent =>
            {
                if (entryProperty == null || entryId.isInstance) { return; }
                BattleEffectShapeTypes.TrySetShape(entryProperty.FindPropertyRelative(BattleEffectItem.shapeField), changeEvent.newValue);
            });
            
            this.AddManipulator(new ContextualMenuManipulator(populateEvent =>
            {
                if (entryProperty == null) { return; }
                BattleEffectClipboard.AppendActions(populateEvent.menu, entryId.isInstance ? "Instance" : "Item", entryProperty, BattleEffectSection.GetChildren(entryProperty).ToList());
            }));
        }

        #region PublicMethods
        public void Bind(SerializedProperty setEntryProperty, BattleEffectRecipeEntry entry, int setRowIndex, bool setSelected)
        {
            rowIndex = setRowIndex;
            isSelected = setSelected;
            entryProperty = setEntryProperty;
            entryId = entry.id;
            isStartEditable = !entry.isInstance;

            labelText.text = entry.label;
            labelText.tooltip = entry.label;
            startText.text = entry.startTime.ToString(_timeFormat, CultureInfo.InvariantCulture);
            labelField.BindProperty(entryProperty.FindPropertyRelative(entry.isInstance ? BattleEffectInstance.labelField : BattleEffectItem.labelField));
            if (entry.isInstance)
            {
                startField.Unbind();
                recipeText.text = entry.recipeName ?? _noRecipeLabel;
                recipeText.tooltip = recipeText.text;
            }
            else
            {
                startField.BindProperty(entryProperty.FindPropertyRelative(BattleEffectItem.startTimeSecondsField));
                shapeField.SetValueWithoutNotify(entry.shapeName);
            }
            shapeField.style.display = entry.isInstance ? DisplayStyle.None : DisplayStyle.Flex;
            recipeText.style.display = entry.isInstance ? DisplayStyle.Flex : DisplayStyle.None;
            RefreshColour();
            tooltip = entry.isInstance ? "Instance (its start is where its items begin;  set the offset in its settings)" : null;
            EndEditing(labelText, labelField);
            EndEditing(startText, startField);
        }

        public void SetSelected(bool setSelected)
        {
            if (isSelected == setSelected) { return; }
            isSelected = setSelected;
            RefreshColour();
        }

        public Color GetColour() => style.backgroundColor.value;

        public void UnbindRow()
        {
            rowIndex = -1;
            entryProperty = null;
            labelField.Unbind();
            startField.Unbind();
        }
        #endregion

        #region PrivateMethods
        private void SetHovered(bool setHovered)
        {
            isHovered = setHovered;
            RefreshColour();
        }

        private void RefreshColour() => style.backgroundColor = BattleEffectEntryColours.GetRowColour(entryId.isInstance, isSelected, isHovered);

        private static Label CreateText(float flexGrow)
        {
            return new Label
            {
                style =
                {
                    flexGrow = flexGrow,
                    flexShrink = flexGrow,
                    paddingLeft = 3f,
                    unityTextAlign = TextAnchor.MiddleLeft,
                    overflow = Overflow.Hidden,
                    whiteSpace = WhiteSpace.NoWrap,
                    textOverflow = TextOverflow.Ellipsis
                }
            };
        }

        private static void SetUpEditing(Label text, VisualElement field, Func<bool> isEditable)
        {
            // Double-click swaps the text for its delayed field && enter / escape / leaving the field commits + swaps back
            text.RegisterCallback<ClickEvent>(clickEvent =>
            {
                if (clickEvent.clickCount != 2 || !isEditable()) { return; }
                text.style.display = DisplayStyle.None;
                field.style.display = DisplayStyle.Flex;
                field.schedule.Execute(field.Focus);
            });
            field.RegisterCallback<FocusOutEvent>(_ => EndEditing(text, field));
            field.RegisterCallback<KeyDownEvent>(keyDownEvent =>
            {
                if (keyDownEvent.keyCode is KeyCode.Return or KeyCode.KeypadEnter or KeyCode.Escape) { field.Blur(); }
            }, TrickleDown.TrickleDown);
        }

        private static void EndEditing(Label text, VisualElement field)
        {
            text.style.display = DisplayStyle.Flex;
            field.style.display = DisplayStyle.None;
        }
        #endregion
    }
}
#endif

#if UNITY_EDITOR
using System.Globalization;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace LowDefMustard.Rendering.Editor
{
    internal sealed class BattleEffectItemElement : Foldout
    {
        // Notes:
        //  - Most sections get their settings from their own drawers, with few notable ∆s:
        //      - timing + transform are built here (loose fields) 
        //      - edge gradient only shows for styles that draw an outline (only rule currently spanning two groups)

        // Const Tunables
        private const string _timeFormat = "0.###";
        private const float _itemSpacing = 4f;
        private static readonly Color _dividerColour = new(0f, 0f, 0f, 0.3f);
        private static readonly string[] _groupFields =
        {
            BattleEffectItem.styleField, BattleEffectItem.altColoursField, BattleEffectItem.outlineEdgeGradientField, BattleEffectItem.stripesField, BattleEffectItem.clipField,
            BattleEffectItem.repetitionField, BattleEffectItem.colourCycleField, BattleEffectItem.beatsField, BattleEffectItem.jitterField, BattleEffectItem.flickerField, BattleEffectItem.pulseField
        };

        // State
        private readonly SerializedProperty item;
        private readonly int itemIndex;
        
        private readonly SerializedProperty labelProperty;
        private readonly SerializedProperty shapeProperty;
        private readonly SerializedProperty startProperty;
        private readonly SerializedProperty durationProperty;
        private readonly SerializedProperty styleModeProperty;
        private PropertyField edgeGradientField;

        // Constructor
        public BattleEffectItemElement(SerializedProperty item, int itemIndex)
        {
            this.item = item;
            this.itemIndex = itemIndex;
            labelProperty = Find(BattleEffectItem.labelField);
            shapeProperty = Find(BattleEffectItem.shapeField);
            startProperty = Find(BattleEffectItem.startTimeSecondsField);
            durationProperty = Find(BattleEffectItem.durationSecondsField);
            styleModeProperty = Find($"{BattleEffectItem.styleField}.{BattleEffectStyle.modeField}");

            style.marginTop = _itemSpacing;
            style.borderTopWidth = 1f;
            style.borderTopColor = _dividerColour;
            value = true;

            BuildFields();
            Refresh();
            this.TrackSerializedObjectValue(item.serializedObject, _ => Refresh()); // Whole object (one per shown item):  tracking the item property here didn't fire
            VisualElement header = this.Q(className: textUssClassName);
            if (header != null) { header.style.unityFontStyleAndWeight = FontStyle.Bold; }
            BattleEffectClipboard.AddMenu(this.Q<Toggle>(className: toggleUssClassName), "Item", item, BattleEffectSection.GetChildren(item));
        }

        #region PublicMethods
        public int GetItemIndex() => itemIndex;
        #endregion

        #region PrivateMethods
        private SerializedProperty Find(string fieldName) => item.FindPropertyRelative(fieldName);
        private bool HasOutline() => (BattleEffectStyleMode)styleModeProperty.intValue != BattleEffectStyleMode.Fill;

        private void BuildFields()
        {
            Add(new PropertyField(labelProperty));
            Add(new PropertyField(shapeProperty));
            Add(new BattleEffectSection("Timing", item, new[] { BattleEffectItem.startTimeSecondsField, BattleEffectItem.durationSecondsField, BattleEffectItem.easingField, BattleEffectItem.approachRatioField, BattleEffectItem.progressStepsField }.Select(Find), 
                null,
                section => RefreshTimingFields(section, Find(BattleEffectItem.easingField))));
            Add(new BattleEffectSection("Transform", item, new[] { BattleEffectItem.positionEUField, BattleEffectItem.scaleEUField, BattleEffectItem.elongationSUField, BattleEffectItem.itemRotationDegreesField, BattleEffectItem.shapeRotationDegreesField }.Select(Find)));
            foreach (string groupField in _groupFields)
            {
                var field = new PropertyField(Find(groupField));
                if (groupField == BattleEffectItem.outlineEdgeGradientField) { edgeGradientField = field; }
                Add(field);
            }
        }

        private void Refresh()
        {
            if (item.serializedObject.FindProperty(item.propertyPath) == null) { return; }
            string label = string.IsNullOrEmpty(labelProperty.stringValue) ? $"Item {itemIndex}" : labelProperty.stringValue;
            string shapeName = BattleEffectShapeTypes.Format(shapeProperty.managedReferenceValue?.GetType());
            float startTime = startProperty.floatValue;
            float endTime = startTime + durationProperty.floatValue;
            text = $"{label}  ~  {shapeName}  ~  {startTime.ToString(_timeFormat, CultureInfo.InvariantCulture)} → {endTime.ToString(_timeFormat, CultureInfo.InvariantCulture)}s";
            edgeGradientField.style.display = HasOutline() ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private static void RefreshTimingFields(BattleEffectSection section, SerializedProperty easingProperty)
        {
            var easing = (BattleEffectEasing)easingProperty.intValue;
            section.SetShown(BattleEffectItem.approachRatioField, easing == BattleEffectEasing.Approach);
        }
        #endregion
    }
}
#endif

using System;
using System.Globalization;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.Scripting.LifecycleManagement;

namespace LowDefMustard.Utils.Editor
{
    public class ProbabilityWeightField : VisualElement
    {
        // Const Tunables
        private const string _excludedText = "never picked";
        private const string _zeroWeightTooltip = "Weight 0 ~ never picked";
        private const string _emptyValueTooltipFormat = "No {0} ~ landing here picks nothing";
        private const string _skippedValueTooltipFormat = "No {0} ~ never picked";
        private const string _emptyValueTypeName = "value";
        private const string _shareTooltip = "Share of total weight across picked entries";
        private const float _shareLabelWidth = 80f;
        [NoAutoStaticsCleanup] private static readonly Color _excludedColour = new(0.95f, 0.65f, 0.25f);

        // Classes
        public const string shareLabelClass = "probability-weight-field__share";

        // Element References
        private readonly IntegerField weightField;
        private readonly Label shareLabel;

        // State
        private readonly string weightPropertyPath;
        private readonly int min;
        private readonly bool isEmptyValueSkipped;

        #region ConstructionRefresh
        public ProbabilityWeightField(SerializedProperty weightProperty, string label, int min = 0, bool isEmptyValueSkipped = false)
        {
            weightPropertyPath = weightProperty.propertyPath;
            this.min = min;
            this.isEmptyValueSkipped = isEmptyValueSkipped;
            style.flexDirection = FlexDirection.Row;

            weightField = new IntegerField(label) { style = { flexGrow = 1f } };
            if (label != null) { weightField.AddToClassList(BaseField<int>.alignedFieldUssClassName); }
            weightField.BindProperty(weightProperty);
            weightField.RegisterValueChangedCallback(changeEvent => ClampToMin(changeEvent.newValue));
            Add(weightField);

            shareLabel = new Label { style = { width = _shareLabelWidth, flexShrink = 0f, marginLeft = 4f, unityTextAlign = TextAnchor.MiddleRight } };
            shareLabel.AddToClassList(shareLabelClass);
            Add(shareLabel);

            SerializedObject serializedObject = weightProperty.serializedObject;
            RefreshShareLabel(serializedObject);
            this.TrackSerializedObjectValue(serializedObject, RefreshShareLabel); // Siblings' weights / values change the share too (update on any object changes)
        }
        
        private void RefreshShareLabel(SerializedObject serializedObject)
        {
            shareLabel.text = GetShareText(serializedObject, weightPropertyPath, isEmptyValueSkipped, out bool isExcluded, out bool isEmpty, out string shareTooltip);
            shareLabel.tooltip = shareTooltip;
            shareLabel.style.color = isExcluded ? _excludedColour : StyleKeyword.Null;
            shareLabel.style.unityFontStyleAndWeight = isExcluded || isEmpty ? FontStyle.Italic : StyleKeyword.Null;
        }
        #endregion

        #region PrivateInternalMethods
        internal static string GetShareText(SerializedObject serializedObject, string weightPropertyPath, bool isEmptyValueSkipped, out bool isExcluded, out bool isEmpty, out string tooltip)
        {
            isExcluded = false;
            isEmpty = false;
            tooltip = _shareTooltip;
            SerializedProperty weightProperty = serializedObject.FindProperty(weightPropertyPath);
            if (weightProperty == null || !weightProperty.TryFindOwningArray(out SerializedProperty arrayProperty, out int elementIndex, out string weightRelativePath)) { return string.Empty; }

            long totalWeight = 0;
            int elementWeight = 0;
            for (int i = 0; i < arrayProperty.arraySize; i++)
            {
                SerializedProperty element = arrayProperty.GetArrayElementAtIndex(i);
                int weight = GetRolledWeight(element, weightRelativePath, isEmptyValueSkipped, out string excludedReason, out string emptyNote);
                totalWeight += weight;
                
                if (i != elementIndex) { continue; }
                elementWeight = weight;
                
                if (emptyNote != null) { isEmpty = true; tooltip = emptyNote; }
                if (excludedReason != null) { isExcluded = true; tooltip = excludedReason; }
            }

            if (isExcluded) { return _excludedText; }
            float share = totalWeight > 0 ? (float)elementWeight / totalWeight : 0f;
            return share.ToString("0.#%", CultureInfo.InvariantCulture);
        }
        
        private static int GetRolledWeight(SerializedProperty element, string weightRelativePath, bool isEmptyValueSkipped, out string excludedReason, out string emptyNote)
        {
            excludedReason = null;
            emptyNote = null;
            SerializedProperty weightProperty = element.FindPropertyRelative(weightRelativePath);
            int weight = weightProperty?.intValue ?? 0;

            if (TryGetEmptyValueTypeName(element, out string emptyValueTypeName))
            {
                if (isEmptyValueSkipped)
                {
                    excludedReason = string.Format(_skippedValueTooltipFormat, emptyValueTypeName);
                    return 0;
                }
                emptyNote = string.Format(_emptyValueTooltipFormat, emptyValueTypeName);
            }
            if (weight <= 0)
            {
                excludedReason = _zeroWeightTooltip;
                return 0;
            }
            return weight;
        }
        
        private static bool TryGetEmptyValueTypeName(SerializedProperty element, out string valueTypeName)
        {
            valueTypeName = null;
            if (element.propertyType != SerializedPropertyType.Generic || element.boxedValue is not IObjectProbabilityPair<object> pair) { return false; } // value types cannot box to `object` -> false, which is fine since they cannot be empty either 
            if (!ProbabilityPairExtensions.IsEmpty(pair.value)) { return false; }

            Type valueType = pair.GetType().GetInterfaces()
                .FirstOrDefault(implemented => implemented.IsGenericType && implemented.GetGenericTypeDefinition() == typeof(IObjectProbabilityPair<>))?
                .GetGenericArguments()[0];
            valueTypeName = valueType != null ? ObjectNames.NicifyVariableName(valueType.Name) : _emptyValueTypeName;
            return true;
        }

        private void ClampToMin(int newValue)
        {
            if (newValue >= min) { return; }
            weightField.value = min; // Bound ~ writes back through the binding
        }
        #endregion
    }
}

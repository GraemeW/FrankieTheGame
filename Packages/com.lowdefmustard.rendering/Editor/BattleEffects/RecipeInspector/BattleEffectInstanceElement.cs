#if UNITY_EDITOR
using System.Globalization;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace LowDefMustard.Rendering.Editor
{
    internal sealed class BattleEffectInstanceElement : Foldout
    {
        // Const Tunables
        private const string _timeFormat = "0.###";
        private const float _entrySpacing = 4f;
        private const string _noRecipeLabel = "No recipe";
        private static readonly Color _dividerColour = new(0f, 0f, 0f, 0.3f);

        // State
        private readonly int instanceIndex;
        private readonly SerializedProperty instance;
        
        private readonly SerializedProperty labelProperty;
        private readonly SerializedProperty recipeProperty;
        private readonly SerializedProperty startOffsetProperty;

        // Constructor
        public BattleEffectInstanceElement(SerializedProperty instance, int instanceIndex)
        {
            this.instance = instance;
            this.instanceIndex = instanceIndex;
            
            // Define property fields
            labelProperty = instance.FindPropertyRelative(BattleEffectInstance.labelField);
            recipeProperty = instance.FindPropertyRelative(BattleEffectInstance.recipeField);
            startOffsetProperty = instance.FindPropertyRelative(BattleEffectInstance.startOffsetSecondsField);
            
            // Style
            style.marginTop = _entrySpacing;
            style.borderTopWidth = 1f;
            style.borderTopColor = _dividerColour;
            style.backgroundColor = BattleEffectEntryColours.GetPanelColour(true);
            value = true;

            // Draw Everything
            foreach (SerializedProperty child in BattleEffectSection.GetChildren(instance)) { Add(new PropertyField(child)); }

            // Initializations && Registrations/Callbacks
            Refresh();
            this.TrackSerializedObjectValue(instance.serializedObject, _ => Refresh()); // Whole object ~ one per shown instance
            VisualElement header = this.Q(className: Foldout.textUssClassName);
            if (header != null) { header.style.unityFontStyleAndWeight = FontStyle.Bold; }
            BattleEffectClipboard.AddMenu(this.Q<Toggle>(className: toggleUssClassName), "Instance", instance, BattleEffectSection.GetChildren(instance));
        }

        #region PublicMethods
        public int GetInstanceIndex() => instanceIndex;
        #endregion

        #region PrivateMethods
        private void Refresh()
        {
            if (instance.serializedObject.FindProperty(instance.propertyPath) == null) { return; }
            var recipe = recipeProperty.objectReferenceValue as BattleEffectRecipe;
            string recipeName = recipe != null ? recipe.name : _noRecipeLabel;
            string label = string.IsNullOrEmpty(labelProperty.stringValue) ? recipeName : labelProperty.stringValue;
            float offset = startOffsetProperty.floatValue;
            float startTime = recipe != null ? offset + recipe.GetTotalStartTime() : offset;
            float endTime = recipe != null ? offset + recipe.GetTotalEndTime() : offset;
            text = $"Instance:  {label}  ~  {recipeName}  ~  {startTime.ToString(_timeFormat, CultureInfo.InvariantCulture)} → {endTime.ToString(_timeFormat, CultureInfo.InvariantCulture)}s";
        }
        #endregion
    }
}
#endif

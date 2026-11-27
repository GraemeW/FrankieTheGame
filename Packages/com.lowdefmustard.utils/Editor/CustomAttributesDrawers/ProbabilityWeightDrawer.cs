using UnityEditor;
using UnityEngine.UIElements;

namespace LowDefMustard.Utils.Editor
{
    [CustomPropertyDrawer(typeof(ProbabilityWeightAttribute), true)]
    public class ProbabilityWeightDrawer : PropertyDrawer
    {
        #region UnityMethods
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            if (property.propertyType != SerializedPropertyType.Integer)
            {
                return new HelpBox($"[ProbabilityWeight] on '{property.name}' requires an int field.", HelpBoxMessageType.Error);
            }

            var weightAttribute = (ProbabilityWeightAttribute)attribute;
            string label = string.IsNullOrEmpty(preferredLabel) ? property.displayName : preferredLabel;
            return new ProbabilityWeightField(property, label, weightAttribute.min, weightAttribute.isEmptyValueSkipped);
        }
        #endregion
    }
}

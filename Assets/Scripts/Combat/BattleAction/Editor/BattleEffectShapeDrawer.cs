#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Frankie.Combat.Editor
{
    [CustomPropertyDrawer(typeof(BattleEffectShape))]
    public sealed class BattleEffectShapeDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            // Set Up // Properties
            SerializedProperty shapeTypeProperty = property.FindPropertyRelative(BattleEffectShape.shapeTypeField);
            PropertyField circleField = null;
            PropertyField squiggleField = null;
            SerializedProperty child = property.Copy();
            SerializedProperty end = property.GetEndProperty();
            
            // Draw the foldout w/ properties
            var foldout = new Foldout { text = property.displayName, value = property.isExpanded };
            for (bool hasChild = child.NextVisible(true); hasChild && !SerializedProperty.EqualContents(child, end); hasChild = child.NextVisible(false))
            {
                var field = new PropertyField(child.Copy());
                switch (child.name)
                {
                    case BattleEffectShape.circleField:
                        circleField = field;
                        break;
                    case BattleEffectShape.squiggleField:
                        squiggleField = field;
                        break;
                }
                foldout.Add(field);
            }
            RefreshShapeSettings();
            
            // Registration
            foldout.RegisterValueChangedCallback(changeEvent =>
            {
                if (changeEvent.target != foldout) { return; }
                property.isExpanded = changeEvent.newValue;
            });
            foldout.TrackPropertyValue(shapeTypeProperty, _ => RefreshShapeSettings());

            return foldout;
            
            
            // Local Functions
            void RefreshShapeSettings()
            {
                var shapeType = (BattleEffectShapeType)shapeTypeProperty.intValue;
                if (circleField != null) { circleField.style.display = shapeType == BattleEffectShapeType.Circle ? DisplayStyle.Flex : DisplayStyle.None; }
                if (squiggleField != null) { squiggleField.style.display = shapeType == BattleEffectShapeType.Squiggle ? DisplayStyle.Flex : DisplayStyle.None; }
            }
        }
    }
}
#endif

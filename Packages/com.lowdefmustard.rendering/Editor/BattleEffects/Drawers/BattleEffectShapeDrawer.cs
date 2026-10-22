#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace LowDefMustard.Rendering.Editor
{
    [CustomPropertyDrawer(typeof(BattleEffectShape), true)]
    public sealed class BattleEffectShapeDrawer : PropertyDrawer
    {
        // Note:  List every BattleEffectShape subclass to ensure new shapes don't need editor changes

        // Const Tunables
        private const string _shapeClassPrefix = "BattleEffect";
        private const string _noShapeLabel = "None";
        private const float _settingsIndent = 15f;

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            // Set Up
            List<Type> shapeTypes = TypeCache.GetTypesDerivedFrom<BattleEffectShape>().Where(type => !type.IsAbstract).OrderBy(type => type.Name).ToList();
            var root = new VisualElement();
            var typeField = new DropdownField(property.displayName, shapeTypes.Select(FormatType).ToList(), 0);
            typeField.SetValueWithoutNotify(FormatType(GetShapeType()));
            typeField.AddToClassList(BaseField<string>.alignedFieldUssClassName);
            var settings = new VisualElement { style = { marginLeft = _settingsIndent } };
            root.Add(typeField);
            root.Add(settings);
            RebuildSettings();

            // Registration
            typeField.RegisterValueChangedCallback(changeEvent =>
            {
                Type selectedType = shapeTypes.FirstOrDefault(type => FormatType(type) == changeEvent.newValue);
                if (selectedType == null || selectedType == GetShapeType()) { return; }
                property.managedReferenceValue = Activator.CreateInstance(selectedType);
                property.serializedObject.ApplyModifiedProperties();
                RebuildSettings();
            });
            root.TrackPropertyValue(property, _ =>
            {
                // e.g. undo / redo swapping the shape type
                if (typeField.value == FormatType(GetShapeType())) { return; }
                typeField.SetValueWithoutNotify(FormatType(GetShapeType()));
                RebuildSettings();
            });

            return root;


            // Local Functions
            Type GetShapeType() => property.managedReferenceValue?.GetType();

            void RebuildSettings()
            {
                settings.Clear();
                SerializedProperty child = property.Copy();
                SerializedProperty end = property.GetEndProperty();
                for (bool hasChild = child.NextVisible(true); hasChild && !SerializedProperty.EqualContents(child, end); hasChild = child.NextVisible(false))
                {
                    settings.Add(new PropertyField(child.Copy()));
                }
                settings.Bind(property.serializedObject);
            }
        }

        private static string FormatType(Type type)
        {
            if (type == null) { return _noShapeLabel; }
            string name = type.Name.StartsWith(_shapeClassPrefix) ? type.Name[_shapeClassPrefix.Length..] : type.Name;
            return ObjectNames.NicifyVariableName(name);
        }
    }
}
#endif

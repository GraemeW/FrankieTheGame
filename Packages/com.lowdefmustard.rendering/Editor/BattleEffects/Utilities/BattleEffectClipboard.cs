#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace LowDefMustard.Rendering.Editor
{
    public static class BattleEffectClipboard
    {
        // Note:  Option to skip start time copy to allow all other properties to paste on new element at a new time
        
        // State
        [NoAutoStaticsCleanup] private static string _kind;
        [NoAutoStaticsCleanup] private static List<(string path, object value)> _values;

        #region PublicMethods
        public static void AddMenu(VisualElement target, string label, SerializedProperty owner, IEnumerable<SerializedProperty> properties)
        {
            List<SerializedProperty> copiedProperties = properties.ToList();
            target.AddManipulator(new ContextualMenuManipulator(populateEvent => AppendActions(populateEvent.menu, label, owner, copiedProperties)));
        }

        public static void AppendActions(DropdownMenu menu, string label, SerializedProperty owner, IReadOnlyList<SerializedProperty> properties)
        {
            menu.AppendAction($"Copy {label}", _ => Copy(owner, properties, false));
            if (HasStartTime(owner, properties)) { menu.AppendAction($"Copy {label} with Start Time", _ => Copy(owner, properties, true)); }
            menu.AppendAction($"Paste {label}", _ => Paste(owner, properties, label), _ => CanPaste(owner, properties) ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
        }

        public static void Copy(SerializedProperty owner, IEnumerable<SerializedProperty> properties, bool isStartTimeIncluded)
        {
            // The kind covers every path either way, so a copy w/o the start time still pastes onto the same set
            if (!IsValid(owner)) { return; }
            List<string> paths = GetPaths(owner, properties);
            string startTimePath = GetStartTimePath(owner);
            _values = paths.Where(path => isStartTimeIncluded || path != startTimePath).Select(path => (path, Capture(Find(owner, path)))).ToList();
            _kind = GetKind(owner, paths);
        }

        public static bool HasStartTime(SerializedProperty owner, IEnumerable<SerializedProperty> properties)
        {
            string startTimePath = GetStartTimePath(owner);
            return startTimePath != null && GetPaths(owner, properties).Contains(startTimePath);
        }

        public static bool CanPaste(SerializedProperty owner, IEnumerable<SerializedProperty> properties)
        {
            return _values != null && IsValid(owner) && _kind == GetKind(owner, GetPaths(owner, properties));
        }

        public static bool Paste(SerializedProperty owner, IEnumerable<SerializedProperty> properties, string label)
        {
            if (!CanPaste(owner, properties)) { return false; }
            owner.serializedObject.Update();
            foreach ((string path, object value) in _values) { Apply(Find(owner, path), value); }
            owner.serializedObject.ApplyModifiedProperties();
            Undo.SetCurrentGroupName($"Paste {label}");
            return true;
        }
        #endregion

        #region PrivateMethods
        private static bool IsValid(SerializedProperty owner) => owner != null && owner.serializedObject.targetObject != null && owner.serializedObject.FindProperty(owner.propertyPath) != null;
        private static SerializedProperty Find(SerializedProperty owner, string path) => path.Length == 0 ? owner : owner.FindPropertyRelative(path);

        private static string GetStartTimePath(SerializedProperty owner)
        {
            return owner.type switch
            {
                nameof(BattleEffectItem) => BattleEffectItem.startTimeSecondsField,
                nameof(BattleEffectInstance) => BattleEffectInstance.startOffsetSecondsField,
                _ => null
            };
        }

        private static List<string> GetPaths(SerializedProperty owner, IEnumerable<SerializedProperty> properties)
        {
            // Relative to the owner ("" = the owner itself, e.g. a shape)
            string ownerPrefix = owner.propertyPath + ".";
            return properties.Select(property => property.propertyPath == owner.propertyPath ? "" : property.propertyPath.StartsWith(ownerPrefix) ? property.propertyPath[ownerPrefix.Length..] : property.propertyPath).ToList();
        }

        private static string GetKind(SerializedProperty owner, IEnumerable<string> paths)
        {
            // A managed reference's type is its current class, so it's keyed on its field's declared type (any shape pastes onto any shape)
            string ownerType = owner.propertyType == SerializedPropertyType.ManagedReference ? owner.managedReferenceFieldTypename : owner.type;
            return $"{ownerType}/{string.Join(",", paths)}";
        }

        private static object Capture(SerializedProperty property)
        {
            if (property.propertyType == SerializedPropertyType.ManagedReference) { return Clone(property.managedReferenceValue); }
            if (property.isArray && property.propertyType != SerializedPropertyType.String)
            {
                var elements = new List<object>();
                for (int i = 0; i < property.arraySize; i++) { elements.Add(Capture(property.GetArrayElementAtIndex(i))); }
                return elements;
            }
            return property.boxedValue;
        }

        private static void Apply(SerializedProperty property, object value)
        {
            if (property == null) { return; }
            if (property.propertyType == SerializedPropertyType.ManagedReference)
            {
                property.managedReferenceValue = Clone(value);
                return;
            }
            if (property.isArray && property.propertyType != SerializedPropertyType.String && value is List<object> elements)
            {
                property.arraySize = elements.Count;
                for (int i = 0; i < elements.Count; i++) { Apply(property.GetArrayElementAtIndex(i), elements[i]); }
                return;
            }
            property.boxedValue = value;
        }

        private static object Clone(object value) => value == null ? null : JsonUtility.FromJson(JsonUtility.ToJson(value), value.GetType());
        #endregion
    }
}
#endif

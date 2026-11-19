using UnityEditor;

namespace LowDefMustard.Utils.Editor
{
    public static class SerializedPropertyExtensions
    {
        // Const Tunables
        private const string _stringElementType = "string"; // SerializedProperty.arrayElementType of a string[] / List<string>

        #region PublicMethods
        public static bool IsStringArray(this SerializedProperty property) => property.isArray && property.arrayElementType == _stringElementType;

        public static SerializedProperty FindParentProperty(this SerializedProperty property)
        {
            //  Notes:
            //  - Parent -> null at the top level (the root holds no relative lookups)
            //  - A list element's parent is the list's .Array node (works as the list:  arraySize, GetArrayElementAtIndex)
            
            SerializedProperty parent = property.Copy();
            return parent.Parent() && parent.depth >= 0 ? parent : null;
        }
        
        public static SerializedProperty FindSiblingProperty(this SerializedProperty property, string siblingName)
        {
            // Note:  field next to this field w/ the same owner ~ top-level fields look up on the object
            
            SerializedProperty parent = property.FindParentProperty();
            return parent != null ? parent.FindPropertyRelative(siblingName) : property.serializedObject.FindProperty(siblingName);
        }

        public static bool TryFindOwningArray(this SerializedProperty property, out SerializedProperty arrayProperty, out int elementIndex, out string relativePath)
        {
            //  Notes:
            //  - Nested lists win inward ~ i.e. taking innermost list holding the property
            //  - False when not in a list, or for the element itself (no relative path)

            arrayProperty = null;
            elementIndex = -1;
            relativePath = null;

            SerializedProperty element = property;
            SerializedProperty parent = property.FindParentProperty();
            while (parent != null && !(parent.isArray && parent.propertyType != SerializedPropertyType.String))
            {
                element = parent;
                parent = parent.FindParentProperty();
            }
            if (parent == null || SerializedProperty.EqualContents(element, property)) { return false; }

            for (int i = 0; i < parent.arraySize; i++)
            {
                if (!SerializedProperty.EqualContents(parent.GetArrayElementAtIndex(i), element)) { continue; }

                arrayProperty = parent;
                elementIndex = i;
                relativePath = property.propertyPath[(element.propertyPath.Length + 1)..];
                return true;
            }
            return false;
        }
        #endregion
    }
}

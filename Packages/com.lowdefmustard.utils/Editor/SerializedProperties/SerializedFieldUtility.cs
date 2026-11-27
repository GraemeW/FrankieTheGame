using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LowDefMustard.Utils.Editor
{
    public static class SerializedFieldUtility
    {
        // Const Tunables
        private const BindingFlags _declaredInstanceFields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

        #region PublicMethods
        public static IEnumerable<FieldInfo> GetSerializedFields(Type type)
        {
            // Note:  Includes private fields of base classes (not returned by a plain GetFields) ~ stops at UnityEngine.Object (no managed fields above)
            for (Type declaringType = type; declaringType != null && declaringType != typeof(Object) && declaringType != typeof(object); declaringType = declaringType.BaseType)
            {
                foreach (FieldInfo field in declaringType.GetFields(_declaredInstanceFields))
                {
                    if (IsSerialized(field)) { yield return field; }
                }
            }
        }

        public static bool IsSerialized(FieldInfo field)
        {
            if (field.IsStatic || field.IsInitOnly || field.IsLiteral || field.IsNotSerialized) { return false; }
            return field.IsPublic || field.IsDefined(typeof(SerializeField), false) || field.IsDefined(typeof(SerializeReference), false);
        }

        public static Type GetCollectionElementType(Type fieldType)
        {
            // Note:  T[] / List<T> -> T (Unity's only serialized collections) ~ any other type is returned as-is
            if (fieldType.IsArray) { return fieldType.GetElementType(); }
            return fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(List<>) ? fieldType.GetGenericArguments()[0] : fieldType;
        }

        public static bool IsInlineSerializable(Type type)
        {
            //  Notes:
            //  - Serialized inline (its fields live inside the owner) ~ i.e. a [Serializable] class / struct
            //  - False for primitives, enums, strings, UnityEngine.Object references (separate objects), abstract types and core library types (e.g. Dictionary)
            if (type == null || type.IsPrimitive || type.IsEnum || type == typeof(string)) { return false; }
            if (typeof(Object).IsAssignableFrom(type)) { return false; }
            if (type.Assembly == typeof(object).Assembly) { return false; }
            return type.IsSerializable && !type.IsAbstract && !type.IsGenericTypeDefinition;
        }
        #endregion
    }
}

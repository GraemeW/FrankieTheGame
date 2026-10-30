#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;

namespace LowDefMustard.Rendering.Editor
{
    public static class BattleEffectShapeTypes
    {
        // Const Tunables
        private const string _shapeClassPrefix = "BattleEffect";
        private const string _noShapeLabel = "None";
        internal const string undoName = "Change Battle Effect Shape";

        // State
        [NoAutoStaticsCleanup] private static readonly Dictionary<Type, string> _names = new(); // Cache since timeline collects names every frame

        #region PublicMethods
        public static List<string> GetNames() => GetTypes().Select(Format).ToList();

        public static string Format(Type type)
        {
            if (type == null) { return _noShapeLabel; }
            if (_names.TryGetValue(type, out string name)) { return name; }
            name = ObjectNames.NicifyVariableName(type.Name.StartsWith(_shapeClassPrefix) ? type.Name[_shapeClassPrefix.Length..] : type.Name);
            _names[type] = name;
            return name;
        }

        public static bool TrySetShape(SerializedProperty shapeProperty, string shapeName)
        {
            Type selectedType = GetTypes().FirstOrDefault(type => Format(type) == shapeName);
            if (selectedType == null || selectedType == shapeProperty.managedReferenceValue?.GetType()) { return false; }
            
            shapeProperty.managedReferenceValue = Activator.CreateInstance(selectedType);
            shapeProperty.serializedObject.ApplyModifiedProperties();
            Undo.SetCurrentGroupName(undoName);
            return true;
        }
        #endregion

        #region PrivateMethods
        private static IEnumerable<Type> GetTypes() => TypeCache.GetTypesDerivedFrom<BattleEffectShape>().Where(type => !type.IsAbstract).OrderBy(type => type.Name);
        #endregion
    }
}
#endif

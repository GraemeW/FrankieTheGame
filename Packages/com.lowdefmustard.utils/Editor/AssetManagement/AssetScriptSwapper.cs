using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LowDefMustard.Utils.Editor
{
    public static class AssetScriptSwapper
    {
        // Const
        private const string _scriptReference = "m_Script";

        // Public Methods
        public static bool ConfirmInPlace(string title, int count, string fromTypeName, string toTypeName)
        {
            string message = $"This changes {count} asset(s) in place from {fromTypeName} to {toTypeName} - same file, same GUID.\n\n" + "Recommended: commit or stash first. Proceed?";
            return EditorUtility.DisplayDialog(title, message, "Convert", "Cancel");
        }

        public static void SwapScriptInPlace<TTarget>(Object source) where TTarget : Object
        {
            if (source == null) { throw new ArgumentNullException(nameof(source)); }
            
            Type targetType = typeof(TTarget);
            bool isCompatible = (source is ScriptableObject && typeof(ScriptableObject).IsAssignableFrom(targetType)) || (source is MonoBehaviour && typeof(MonoBehaviour).IsAssignableFrom(targetType));
            if (!isCompatible) { throw new ArgumentException($"Can't swap a {source.GetType().Name} to {targetType.Name}: both must be ScriptableObjects or both MonoBehaviours", nameof(source)); }

            string path = AssetDatabase.GetAssetPath(source);
            if (string.IsNullOrEmpty(path)) { throw new ArgumentException($"{source.name} isn't a persistent asset", nameof(source)); }

            MonoScript targetScript = FindScript(targetType);
            if (targetScript == null) { throw new ArgumentException($"No script asset found for {targetType.Name} (its class and file names must match)", nameof(TTarget)); }

            var serializedObject = new SerializedObject(source);
            serializedObject.FindProperty(_scriptReference).objectReferenceValue = targetScript;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate); // Forces Unity to re-instantiate the object as TTarget from the newly written m_Script reference
        }

        // Private Methods
        private static MonoScript FindScript(Type type) => MonoImporter.GetAllRuntimeMonoScripts().FirstOrDefault(script => script != null && script.GetClass() == type);
    }
}

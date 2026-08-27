using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace LowDefMustard.RuleTiles.Editor
{
    // Converts a plain RuleTile asset into a new RuleTileSibling asset alongside it,carrying over every serialized RuleTile field, including:
    //  - default sprite
    //  - gameObject
    //  - collider
    //  - type
    //  - tiling rules
    // Uses JsonUtility.ToJson/FromJsonOverwrite to transplant fields by name across the type boundary
    // This correctly resolves nested Object references as long as source and destination are both loaded in the same Editor session
    // Only handles the base RuleTile type - extend the type checks below if a project-specific RuleTile<T> subtype needs the same treatment
    public static class RuleTileToSiblingConverter
    {
        private const string _menuPath = "Assets/LowDefMustard/RuleTiles/Convert to Sibling Rule Tile";
        private const string _inPlaceMenuPath = "Assets/LowDefMustard/RuleTiles/Convert to Sibling Rule Tile (In-Place)";
        private const string _scriptReference = "m_Script";
        
        [MenuItem(_menuPath, true)]
        [MenuItem(_inPlaceMenuPath, true)]
        // Note:  Marked as internal for test access
        internal static bool Validate()
        {
            var selected = Selection.objects;
            return selected.Length != 0 && selected.All(selectedObject => selectedObject.GetType() == typeof(RuleTile));
        }

        [MenuItem(_menuPath)]
        private static void Convert()
        {
            foreach (Object selectedObject in Selection.objects)
            {
                if (selectedObject is not RuleTile ruleTile) { continue; }
                ConvertOne(ruleTile);
            }
            AssetDatabase.SaveAssets();
        }
        
        [MenuItem(_inPlaceMenuPath)]
        private static void ConvertInPlace()
        {
            Object[] targets = Selection.objects;
            string message = $"This changes {targets.Length} asset(s) in place from RuleTile to RuleTileSibling - same file, same GUID.\n\n" +
                             "Recommended: commit or stash first. Proceed?";
            if (!EditorUtility.DisplayDialog("Convert to Sibling Rule Tile (In-Place)", message, "Convert", "Cancel")) { return; }

            foreach (Object selectedObject in targets)
            {
                if (selectedObject is not RuleTile ruleTile) { continue; }
                ConvertOneInPlace(ruleTile);
            }
        }

        // Note:  Marked as internal for test access
        internal static void ConvertOne(RuleTile source)
        {
            string sourcePath = AssetDatabase.GetAssetPath(source);
            string directory = Path.GetDirectoryName(sourcePath);
            if (string.IsNullOrEmpty(directory)) { return; }
            
            var newPath = AssetDatabase.GenerateUniqueAssetPath( Path.Combine(directory, source.name + "Sibling.asset"));
            var clone = ScriptableObject.CreateInstance<RuleTileSibling>();

            var json = JsonUtility.ToJson(source);
            JsonUtility.FromJsonOverwrite(json, clone);

            AssetDatabase.CreateAsset(clone, newPath);
            EditorGUIUtility.PingObject(clone);
        }

        // Note:  Marked as internal for test access
        internal static void ConvertOneInPlace(RuleTile source)
        {
            string path = AssetDatabase.GetAssetPath(source);

            // MonoScript.FromScriptableObject needs a live instance of the target type to resolve its backing script asset
            var scriptLookupInstance = ScriptableObject.CreateInstance<RuleTileSibling>();
            MonoScript siblingScript = MonoScript.FromScriptableObject(scriptLookupInstance);
            Object.DestroyImmediate(scriptLookupInstance);

            var serializedObject = new SerializedObject(source);
            serializedObject.FindProperty(_scriptReference).objectReferenceValue = siblingScript;
            serializedObject.ApplyModifiedProperties();

            AssetDatabase.SaveAssets();
            // Forces Unity to re-instantiate the asset as RuleTileSibling from the newly written m_Script reference
            // The in-memory `source` reference stays typed as RuleTile and shouldn't be used after this
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }
    }
}

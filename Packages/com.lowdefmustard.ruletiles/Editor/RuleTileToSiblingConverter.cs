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

        [MenuItem(_menuPath, true)]
        private static bool Validate()
        {
            var selected = Selection.objects;
            return selected.Length != 0 && selected.All(obj => obj.GetType() == typeof(RuleTile));
        }

        [MenuItem(_menuPath)]
        private static void Convert()
        {
            foreach (Object obj in Selection.objects) { ConvertOne((RuleTile)obj); }
            AssetDatabase.SaveAssets();
        }

        private static void ConvertOne(RuleTile source)
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
    }
}

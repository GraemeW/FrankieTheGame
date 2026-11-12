using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using LowDefMustard.Utils.Editor;

namespace LowDefMustard.RuleTiles.Editor
{
    public static class RuleTileToSiblingConverter
    {
        // Note:  Methods marked as internal for test access
        
        // Const tunables
        private const string _menuPath = "Assets/LowDefMustard/RuleTiles/Convert to Sibling Rule Tile";
        private const string _inPlaceMenuPath = "Assets/LowDefMustard/RuleTiles/Convert to Sibling Rule Tile (In-Place)";

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
            if (!AssetScriptSwapper.ConfirmInPlace("Convert to Sibling Rule Tile (In-Place)", targets.Length, nameof(RuleTile), nameof(RuleTileSibling))) { return; }

            foreach (Object selectedObject in targets)
            {
                if (selectedObject is not RuleTile ruleTile) { continue; }
                ConvertOneInPlace(ruleTile);
            }
        }

        [MenuItem(_menuPath, true)]
        [MenuItem(_inPlaceMenuPath, true)]
        internal static bool Validate()
        {
            var selected = Selection.objects;
            return selected.Length != 0 && selected.All(selectedObject => selectedObject.GetType() == typeof(RuleTile));
        }
        
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
        
        internal static void ConvertOneInPlace(RuleTile source) => AssetScriptSwapper.SwapScriptInPlace<RuleTileSibling>(source);
    }
}

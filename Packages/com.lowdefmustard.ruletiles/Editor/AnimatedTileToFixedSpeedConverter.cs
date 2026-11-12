using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using LowDefMustard.Utils.Editor;

namespace LowDefMustard.RuleTiles.Editor
{
    public static class AnimatedTileToFixedSpeedConverter
    {
        // Note:  Methods marked as internal for test access
        
        // Const Tunables
        private const string _inPlaceMenuPath = "Assets/LowDefMustard/RuleTiles/Convert to Fixed Speed Animated Tile (In-Place)";

        [MenuItem(_inPlaceMenuPath)]
        private static void ConvertInPlace()
        {
            Object[] targets = Selection.objects;
            if (!AssetScriptSwapper.ConfirmInPlace("Convert to Fixed Speed Animated Tile (In-Place)", targets.Length, nameof(AnimatedTile), nameof(AnimatedTileFixedSpeed))) { return; }

            foreach (Object selectedObject in targets)
            {
                if (selectedObject is not AnimatedTile animatedTile) { continue; }
                ConvertOneInPlace(animatedTile);
            }
        }
        
        [MenuItem(_inPlaceMenuPath, true)]
        internal static bool Validate()
        {
            var selected = Selection.objects;
            return selected.Length != 0 && selected.All(selectedObject => selectedObject.GetType() == typeof(AnimatedTile));
        }
        
        internal static void ConvertOneInPlace(AnimatedTile source) => AssetScriptSwapper.SwapScriptInPlace<AnimatedTileFixedSpeed>(source);
    }
}

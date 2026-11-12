using UnityEditor;
using UnityEditor.Tilemaps;

namespace LowDefMustard.RuleTiles.Editor
{
    [CustomEditor(typeof(AnimatedTileFixedSpeed))]
    public sealed class AnimatedTileFixedSpeedEditor : AnimatedTileEditor
    {
        // AnimatedTileEditor isn't registered for child classes ~ need to re-register it to keep the standard inspector
    }
}

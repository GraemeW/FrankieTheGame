using System;
using UnityEngine;

namespace LowDefMustard.Rendering
{
    [Serializable]
    public sealed class BattleEffectOutlineEdgeGradient
    {
        // Notes:
        //  - Blend from outline colour on the inside edge to this colour on the outside edge
        //      - Style -> outline = the item's colour (incl. its palette)
        //      - Style -> fill and outline = the outline colour
        //  - Steps band the blend ~ e.g. where 0 = smooth, 3 = inside / middle / outside colours

        [SerializeField] private bool isEnabled = false;
        [SerializeField][Tooltip("The stroke's outside edge (multiplied by the effect colour)")] private Color colour = Color.white;
        [SerializeField][Tooltip("Bands across the stroke (0 = smooth)")][Min(0)] private int steps = 0;
        [SerializeField][Tooltip("The outside edge takes a palette entry ~ following its colour cycle / pattern / beats")] private bool isFromPalette = false;
        [SerializeField][Tooltip("Palette entries after the item's (wraps;  e.g. 1 = the next colour, -1 = the one before)")] private int paletteStep = 1;

        // Editor Field References
        public const string isEnabledField = nameof(isEnabled);
        public const string colourField = nameof(colour);
        public const string stepsField = nameof(steps);
        public const string isFromPaletteField = nameof(isFromPalette);
        public const string paletteStepField = nameof(paletteStep);

        internal bool IsPaletteUsed() => isEnabled && isFromPalette;

        internal void WriteData(Color[] texels, int rowStart)
        {
            texels[rowStart + BattleEffectDataLayout.edgeGradient] = new Color(isEnabled ? 1f : 0f, Mathf.Max(steps, 0), IsPaletteUsed() ? 1f : 0f, IsPaletteUsed() ? paletteStep : 0f);
            texels[rowStart + BattleEffectDataLayout.edgeColour] = isEnabled ? colour.linear : Color.clear;
        }
    }
}

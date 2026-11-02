using System;
using UnityEngine;

namespace LowDefMustard.Rendering
{
    [Serializable]
    public sealed class BattleEffectStyle
    {
        // Note:
        //  - Outline thickness may end below zero to fade the outline out through the recipe's glow

        [SerializeField] private BattleEffectStyleMode mode = BattleEffectStyleMode.Outline;
        [SerializeField][Tooltip("Over:  paints over earlier items.  Erase:  punches its coverage out of them (fill and outline:  its fill only)")] private BattleEffectBlend blend = BattleEffectBlend.Over;
        [SerializeField][Tooltip("Outline + fill and outline")] private BattleEffectFloatRange outlineThicknessEU = new(0.05f);
        [SerializeField][Tooltip("Multiplied by the effect colour.  Fill / outline:  the shape.  Fill and outline:  the fill.  Palette entry A (alt colours follow)")] private Color colour = Color.white;
        [SerializeField][Tooltip("Fill and outline only:  the stroke's colour (its edge gradient blends from it).  Stripes + an erase blend apply to the fill alone")] private Color outlineColour = Color.white;
        [SerializeField][Tooltip("Fill and outline only:  the stroke takes a palette entry (the fill's + the step) instead of the outline colour ~ following fill's colour cycle / pattern / beats")] private bool isOutlineFromPalette = false;
        [SerializeField][Tooltip("Palette entries after the fill's (wraps;  e.g. 1 = the next colour, -1 = the one before)")] private int outlinePaletteStep = 1;

        // Editor Field References
        public const string modeField = nameof(mode);
        public const string blendField = nameof(blend);
        public const string outlineThicknessEUField = nameof(outlineThicknessEU);
        public const string colourField = nameof(colour);
        public const string outlineColourField = nameof(outlineColour);
        public const string isOutlineFromPaletteField = nameof(isOutlineFromPalette);
        public const string outlinePaletteStepField = nameof(outlinePaletteStep);

        internal Color GetColour() => colour;
        internal bool IsPaletteUsed() => mode == BattleEffectStyleMode.FillAndOutline && isOutlineFromPalette;

        internal void WriteData(Color[] texels, int rowStart, float effectScale)
        {
            texels[rowStart + BattleEffectDataLayout.style] = new Color((int)mode, (int)blend, outlineThicknessEU.GetStart() * effectScale, outlineThicknessEU.GetEnd() * effectScale);
            texels[rowStart + BattleEffectDataLayout.colour] = colour.linear;
            texels[rowStart + BattleEffectDataLayout.outlineColour] = outlineColour.linear;
            texels[rowStart + BattleEffectDataLayout.outlineSource] = new Color(IsPaletteUsed() ? 1f : 0f, outlinePaletteStep, 0f, 0f);
        }
    }
}

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

        // Editor Field References
        public const string modeField = nameof(mode);
        public const string blendField = nameof(blend);
        public const string outlineThicknessEUField = nameof(outlineThicknessEU);
        public const string colourField = nameof(colour);
        public const string outlineColourField = nameof(outlineColour);

        internal Color GetColour() => colour;

        internal void WriteData(Color[] texels, int rowStart, float effectScale)
        {
            texels[rowStart + BattleEffectDataLayout.style] = new Color((int)mode, (int)blend, outlineThicknessEU.GetStart() * effectScale, outlineThicknessEU.GetEnd() * effectScale);
            texels[rowStart + BattleEffectDataLayout.colour] = colour.linear;
            texels[rowStart + BattleEffectDataLayout.outlineColour] = outlineColour.linear;
        }
    }
}

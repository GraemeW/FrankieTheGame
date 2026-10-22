using System;
using UnityEngine;

namespace LowDefMustard.Rendering
{
    [Serializable]
    public sealed class BattleEffectAltColours
    {
        // Notes:
        //  - Palette entries after the item's colour (A):  B -> C -> D (up to the count)
        //  - Stepped through by the colour cycle (over time / per repetition copy) and/or beats' colour indices
        //  - Each keeps its alpha && is multiplied by the effect colour

        [SerializeField][Tooltip("Colours after the item's own")][Range(0, BattleEffectDataLayout.maxAltColours)] private int count = 0;
        [SerializeField] private Color colourB = Color.white;
        [SerializeField] private Color colourC = Color.white;
        [SerializeField] private Color colourD = Color.white;

        // Editor Field References
        public const string countField = nameof(count);
        public const string colourBField = nameof(colourB);
        public const string colourCField = nameof(colourC);
        public const string colourDField = nameof(colourD);

        internal int WriteData(Color[] texels, int rowStart, bool isPaletteUsed)
        {
            // Returns the palette count (colour + alt colours in use) ~ only kept when something steps through it
            int altCount = isPaletteUsed ? Mathf.Clamp(count, 0, BattleEffectDataLayout.maxAltColours) : 0;
            texels[rowStart + BattleEffectDataLayout.altColourB] = altCount > 0 ? colourB.linear : Color.clear;
            texels[rowStart + BattleEffectDataLayout.altColourC] = altCount > 1 ? colourC.linear : Color.clear;
            texels[rowStart + BattleEffectDataLayout.altColourD] = altCount > 2 ? colourD.linear : Color.clear;
            return 1 + altCount;
        }
    }
}

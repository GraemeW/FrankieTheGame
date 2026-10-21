using System;
using UnityEngine;

namespace LowDefMustard.Rendering
{
    [Serializable]
    public sealed class BattleEffectColourCycle
    {
        // Notes:
        //  - Palette cycling includes the item's colour (denoted as A), then -> B -> C  -> D (up to the colourCount)
        //  - Each colour is obviously also multiplied by the effect colour

        [SerializeField][Tooltip("Palette steps per second (0 = off)")][Min(0f)] private float rate = 0f;
        [SerializeField][Tooltip("Palette steps to start ahead by (e.g. 1 = one colour ahead, 0.5 = half a step)")] private float phaseOffset = 0f;
        [SerializeField][Tooltip("Colours after the item's own")][Range(0, BattleEffectDataLayout.maxCycleColours)] private int colourCount = 0;
        [SerializeField] private Color colourB = Color.white;
        [SerializeField] private Color colourC = Color.white;
        [SerializeField] private Color colourD = Color.white;

        // Editor Field References
        public const string colourCountField = nameof(colourCount);
        public const string colourBField = nameof(colourB);
        public const string colourCField = nameof(colourC);
        public const string colourDField = nameof(colourD);

        internal void WriteData(Color[] texels, int rowStart)
        {
            int cycleColourCount = rate > 0f ? Mathf.Clamp(colourCount, 0, BattleEffectDataLayout.maxCycleColours) : 0;
            texels[rowStart + BattleEffectDataLayout.colourCycle] = new Color(rate, 1 + cycleColourCount, phaseOffset, 0f);
            texels[rowStart + BattleEffectDataLayout.cycleColourB] = cycleColourCount > 0 ? colourB.linear : Color.clear;
            texels[rowStart + BattleEffectDataLayout.cycleColourC] = cycleColourCount > 1 ? colourC.linear : Color.clear;
            texels[rowStart + BattleEffectDataLayout.cycleColourD] = cycleColourCount > 2 ? colourD.linear : Color.clear;
        }
    }
}

using System;
using UnityEngine;

namespace LowDefMustard.Rendering
{
    [Serializable]
    public sealed class BattleEffectColourCycle
    {
        // Notes:
        //  - Steps through the palette:  the item's colour (A), and THEN its alt colours (B -> C -> D, up to their count)

        [SerializeField][Tooltip("Time:  steps through the palette over time.  Copy:  each repetition copy takes the next entry")] private BattleEffectColourSource source = BattleEffectColourSource.Time;
        [SerializeField][Tooltip("Time source:  palette steps per second (0 = off)")][Min(0f)] private float rate = 0f;
        [SerializeField][Tooltip("Palette steps to start ahead by (e.g. 1 = one colour ahead;  time source:  0.5 = half a step)")] private float phaseOffset = 0f;

        // Editor Field References
        public const string sourceField = nameof(source);
        public const string rateField = nameof(rate);
        public const string phaseOffsetField = nameof(phaseOffset);

        internal bool IsPaletteUsed() => source == BattleEffectColourSource.Copy || rate > 0f;

        internal void WriteData(Color[] texels, int rowStart, int paletteCount)
        {
            texels[rowStart + BattleEffectDataLayout.colourCycle] = new Color((int)source, rate, phaseOffset, paletteCount);
        }
    }
}

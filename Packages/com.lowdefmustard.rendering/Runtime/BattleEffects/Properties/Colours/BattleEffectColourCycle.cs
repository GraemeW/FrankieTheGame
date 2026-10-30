using System;
using System.Linq;
using UnityEngine;

namespace LowDefMustard.Rendering
{
    [Serializable]
    public sealed class BattleEffectColourCycle
    {
        // Notes:
        //  - Steps through the palette:  the item's colour (A), and THEN its alt colours (B -> C -> D, up to their count)
        //  - Pattern packs into three floats of base-4 digits (step 0 = lowest;  4 ^ 12 = 2 ^ 24 stays exact) + its step count

        // Const Tunables
        public const int patternStepsPerFloat = 12;
        public const int maxPatternSteps = 3 * patternStepsPerFloat;

        [SerializeField][Tooltip("Time:  steps through the palette over time.  Copy:  each repetition copy takes the next entry.  Pattern:  a palette index per step")] private BattleEffectColourSource source = BattleEffectColourSource.Time;
        [SerializeField][Tooltip("Time source:  palette steps per second (0 = off) ~ pattern:  its steps per second")][Min(0f)] private float rate = 0f;
        [SerializeField][Tooltip("Palette steps to start ahead by (e.g. 1 = one colour ahead;  time source:  0.5 = half a step) ~ pattern:  steps")] private float phaseOffset = 0f;
        [SerializeField][Tooltip("Palette index per step from the item's start, then repeating:  0 = the colour, 1 = colour B, ... (e.g. 0112), spaces ignored, up to 36 steps")] private string pattern = "";

        // Editor Field References
        public const string sourceField = nameof(source);
        public const string rateField = nameof(rate);
        public const string phaseOffsetField = nameof(phaseOffset);
        public const string patternField = nameof(pattern);

        internal bool IsPaletteUsed() => source == BattleEffectColourSource.Copy || rate > 0f;

        internal void WriteData(Color[] texels, int rowStart, int paletteCount)
        {
            texels[rowStart + BattleEffectDataLayout.colourCycle] = new Color((int)source, rate, phaseOffset, paletteCount);
            texels[rowStart + BattleEffectDataLayout.colourPattern] = source == BattleEffectColourSource.Pattern ? EncodePattern(pattern) : Color.clear;
        }

        internal static Color EncodePattern(string steps)
        {
            // (steps 0-11, 12-23, 24-35, step count)
            if (string.IsNullOrEmpty(steps)) { return Color.clear; }

            var digits = new float[3];
            int length = 0;
            foreach (char step in steps.Where(step => step is >= '0' and <= '3').TakeWhile(_ => length != maxPatternSteps))
            {
                digits[length / patternStepsPerFloat] += (step - '0') * Mathf.Pow(4f, length % patternStepsPerFloat); // each entry bit-shift << 2, tightly pack 2-bit colour data
                length++;
            }
            return new Color(digits[0], digits[1], digits[2], length);
        }
    }
}

using System;
using UnityEngine;

namespace LowDefMustard.Rendering
{
    [Serializable]
    public sealed class BattleEffectFlicker
    {
        // Notes:
        //  - Pattern packs into one float:  its bits (step 0 = lowest) under a leading 1 that marks its length (exact up to maxPatternSteps)

        // Const Tunables
        public const int maxPatternSteps = 23;

        [SerializeField][Tooltip("Bright / dim cycles per second (0 = off) ~ w/ a pattern:  its steps per second")][Min(0f)] private float rate = 0f;
        [SerializeField][Tooltip("How far alpha drops on the dim half of each cycle (/ a pattern's dim steps)")][Range(0f, 1f)] private float depthFraction = 0.5f;
        [SerializeField][Tooltip("Shifts the flicker by this fraction of a cycle (0.5 = dim half first) ~ w/ a pattern:  of a step")][Range(0f, 1f)] private float phaseFraction = 0f;
        [SerializeField][Tooltip("Optional irregular steps from the item's start, then repeating:  1 = bright, 0 = dim (e.g. 11011 0000), spaces ignored, up to 23 steps;  empty = standard square wave")] private string pattern = "";

        // Editor Field References
        public const string rateField = nameof(rate);

        internal void WriteData(Color[] texels, int rowStart)
        {
            texels[rowStart + BattleEffectDataLayout.flicker] = new Color(rate, depthFraction, phaseFraction, EncodePattern(pattern));
        }

        internal static float EncodePattern(string steps)
        {
            if (string.IsNullOrEmpty(steps)) { return 0f; }

            int bits = 0;
            int length = 0;
            foreach (char step in steps)
            {
                if (step != '0' && step != '1') { continue; }
                if (length == maxPatternSteps) { break; }
                if (step == '1') { bits |= 1 << length; }
                length++;
            }
            return length == 0 ? 0f : (1 << length) | bits;
        }
    }
}

using System;
using System.Linq;
using UnityEngine;

namespace LowDefMustard.Rendering
{
    [Serializable]
    public sealed class BattleEffectBeats
    {
        // Notes:
        //  - Beats are applied on top of the item's ranges (which still run over its duration)
        //  - Two main implementations:
        //      - A. Start-Time Offset:  when pattern is unset / pattern rate = 0, beats start at their defined start times
        //      - B. Pattern:  picks the beat per step, pattern packed into two floats of base-5 digits w/ its rate + step count

        // Const Tunables
        public const int patternStepsPerFloat = 10;
        public const int maxPatternSteps = 2 * patternStepsPerFloat;

        [SerializeField][Tooltip("Beats in use (0 = off)")][Range(0, BattleEffectDataLayout.maxBeats)] private int count = 0;
        [SerializeField][Tooltip("Beat per step from the item's start, then repeating:  0 = none, 1 = beat A, ... 4 = beat D (e.g. 01123), spaces ignored, up to 20 steps")] private string pattern = "";
        [SerializeField][Tooltip("Pattern steps per second")][Min(0f)] private float patternRate = 0f;
        [SerializeField] private BattleEffectBeat beatA;
        [SerializeField] private BattleEffectBeat beatB;
        [SerializeField] private BattleEffectBeat beatC;
        [SerializeField] private BattleEffectBeat beatD;

        // Editor Field References
        public const string countField = nameof(count);
        public const string patternField = nameof(pattern);
        public const string patternRateField = nameof(patternRate);
        public const string beatAField = nameof(beatA);
        public const string beatBField = nameof(beatB);
        public const string beatCField = nameof(beatC);
        public const string beatDField = nameof(beatD);

        // Constants
        private const float _neverSeconds = 1e9f;

        #region InternalMethods
        internal bool IsPaletteUsed()
        {
            for (int i = 0; i < GetCount(); i++) { if (GetBeat(i).GetColourIndex() > 0) { return true; } }
            return false;
        }

        internal void WriteData(Color[] texels, int rowStart, float effectScale)
        {
            var starts = new Color(_neverSeconds, _neverSeconds, _neverSeconds, _neverSeconds);
            var colours = new Color(0f, 0f, 0f, 0f);
            var offsets = new Vector2[BattleEffectDataLayout.maxBeats];
            var elongations = new Vector2[BattleEffectDataLayout.maxBeats];
            for (int i = 0; i < GetCount(); i++)
            {
                BattleEffectBeat beat = GetBeat(i);
                starts[i] = beat.GetStartSeconds();
                colours[i] = beat.GetColourIndex();
                offsets[i] = beat.GetOffset() * effectScale;
                elongations[i] = beat.GetElongation();
            }
            texels[rowStart + BattleEffectDataLayout.beatStarts] = starts;
            texels[rowStart + BattleEffectDataLayout.beatColours] = colours;
            texels[rowStart + BattleEffectDataLayout.beatOffsets01] = new Color(offsets[0].x, offsets[0].y, offsets[1].x, offsets[1].y);
            texels[rowStart + BattleEffectDataLayout.beatOffsets23] = new Color(offsets[2].x, offsets[2].y, offsets[3].x, offsets[3].y);
            texels[rowStart + BattleEffectDataLayout.beatElongations01] = new Color(elongations[0].x, elongations[0].y, elongations[1].x, elongations[1].y);
            texels[rowStart + BattleEffectDataLayout.beatElongations23] = new Color(elongations[2].x, elongations[2].y, elongations[3].x, elongations[3].y);
            texels[rowStart + BattleEffectDataLayout.beatPattern] = GetCount() > 0 ? EncodePattern(pattern, patternRate) : Color.clear;
        }

        internal static void WriteEmpty(Color[] texels, int rowStart)
        {
            texels[rowStart + BattleEffectDataLayout.beatStarts] = new Color(_neverSeconds, _neverSeconds, _neverSeconds, _neverSeconds);
            texels[rowStart + BattleEffectDataLayout.beatPattern] = Color.clear;
        }

        internal static bool IsPatternUsed(string steps, float rate) => EncodePattern(steps, rate).a > 0f;

        internal static Color EncodePattern(string steps, float rate)
        {
            // (steps 0-9, 10-19, rate, step count)
            if (string.IsNullOrEmpty(steps) || rate <= 0f) { return Color.clear; }

            var digits = new float[2];
            int length = 0;
            foreach (char step in steps.Where(step => step is >= '0' and <= '4').TakeWhile(_ => length != maxPatternSteps))
            {
                digits[length / patternStepsPerFloat] += (step - '0') * Mathf.Pow(5f, length % patternStepsPerFloat); // each entry fractional "bit-shift", tightly pack beat steps
                length++;
            }
            return length == 0 ? Color.clear : new Color(digits[0], digits[1], rate, length);
        }
        #endregion

        #region PrivateMethods
        private int GetCount() => Mathf.Clamp(count, 0, BattleEffectDataLayout.maxBeats);
        private BattleEffectBeat GetBeat(int index) => index switch { 0 => beatA, 1 => beatB, 2 => beatC, _ => beatD };
        #endregion
    }
}

using System;
using UnityEngine;

namespace LowDefMustard.Rendering
{
    [Serializable]
    public sealed class BattleEffectStipple
    {
        // Notes:
        //  - Fixed to the effect ~ applies where stripes do (+ w/ them)

        [SerializeField][Tooltip("Random scatter, or a regular checker")] private BattleEffectStipplePattern pattern = BattleEffectStipplePattern.Random;
        [SerializeField][Tooltip("Dots per effect unit (0 = off)")][Min(0f)] private float dotsPerEU = 0f;
        [SerializeField][Tooltip("A dot's height / width (above 1 = taller, below 1 = wider)")][Min(0.01f)] private float aspectRatio = 1f;
        [SerializeField][Tooltip("The share of dots drawn (0-1); checker: set 0.25-0.75")] private BattleEffectFloatRange density = new(0.5f);
        [SerializeField][Tooltip("Picks a different scatter; checker: odd = inverse")] private float seed = 0f;

        // Editor Field References
        public const string patternField = nameof(pattern);
        public const string dotsPerEUField = nameof(dotsPerEU);
        public const string aspectRatioField = nameof(aspectRatio);
        public const string densityField = nameof(density);
        public const string seedField = nameof(seed);

        internal void WriteData(Color[] texels, int rowStart, float effectScale)
        {
            float scaledDots = effectScale > 0f ? dotsPerEU / effectScale : 0f;
            texels[rowStart + BattleEffectDataLayout.stipple] = new Color(scaledDots, density.GetStart(), density.GetEnd(), seed);
            texels[rowStart + BattleEffectDataLayout.stippleShape] = new Color((int)pattern, aspectRatio, 0f, 0f);
        }
    }
}

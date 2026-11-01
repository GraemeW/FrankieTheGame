using System;
using UnityEngine;

namespace LowDefMustard.Rendering
{
    [Serializable]
    public sealed class BattleEffectStipple
    {
        // Notes:
        //  - Fixed to the effect ~ applies where stripes do (+ w/ them)

        [SerializeField][Tooltip("Dots per effect unit (0 = off)")][Min(0f)] private float dotsPerEU = 0f;
        [SerializeField][Tooltip("The share of dots drawn (0-1)")] private BattleEffectFloatRange density = new(0.5f);
        [SerializeField][Tooltip("Picks a different scatter")] private float seed = 0f;

        // Editor Field References
        public const string dotsPerEUField = nameof(dotsPerEU);
        public const string densityField = nameof(density);
        public const string seedField = nameof(seed);

        internal void WriteData(Color[] texels, int rowStart, float effectScale)
        {
            float scaledDots = effectScale > 0f ? dotsPerEU / effectScale : 0f;
            texels[rowStart + BattleEffectDataLayout.stipple] = new Color(scaledDots, density.GetStart(), density.GetEnd(), seed);
        }
    }
}

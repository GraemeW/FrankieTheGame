using System;
using UnityEngine;

namespace LowDefMustard.Rendering
{
    [Serializable]
    public sealed class BattleEffectJitter
    {
        [SerializeField][Tooltip("Position steps per second (0 = off)")][Min(0f)] private float rate = 0f;
        [SerializeField][Tooltip("Random:  re-rolled each step. Orbit:  steps clockwise around the corners of the +/- amount box")] private BattleEffectJitterMode mode = BattleEffectJitterMode.Random;
        [SerializeField][Tooltip("Largest offset on each axis")] private Vector2 amountEU = new(0.02f, 0.02f);
        [SerializeField][Tooltip("Random:  items w/ the same seed + rate jump together (e.g. a shape's fill + outline). Orbit:  the starting corner")] private int seed = 0;
        [SerializeField][Tooltip("Shifts the steps by this fraction of a step")][Range(0f, 1f)] private float phaseFraction = 0f;

        // Editor Field References
        public const string rateField = nameof(rate);

        internal void WriteData(Color[] texels, int rowStart, float effectScale)
        {
            texels[rowStart + BattleEffectDataLayout.jitter] = new Color(rate, amountEU.x * effectScale, amountEU.y * effectScale, seed);
            texels[rowStart + BattleEffectDataLayout.jitterMode] = new Color((int)mode, phaseFraction, 0f, 0f);
        }
    }
}

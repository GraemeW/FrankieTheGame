using System;
using UnityEngine;

namespace LowDefMustard.Rendering
{
    [Serializable]
    public sealed class BattleEffectFlicker
    {
        [SerializeField][Tooltip("Bright / dim cycles per second (0 = off)")][Min(0f)] private float rate = 0f;
        [SerializeField][Tooltip("How far alpha drops on the dim half of each cycle")][Range(0f, 1f)] private float depthFraction = 0.5f;
        [SerializeField][Tooltip("Shifts the flicker by this fraction of a cycle (0.5 = dim half first)")][Range(0f, 1f)] private float phaseFraction = 0f;

        // Editor Field References
        public const string rateField = nameof(rate);

        internal void WriteData(Color[] texels, int rowStart)
        {
            texels[rowStart + BattleEffectDataLayout.flicker] = new Color(rate, depthFraction, phaseFraction, 0f);
        }
    }
}

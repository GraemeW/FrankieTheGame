using System;
using UnityEngine;

namespace LowDefMustard.Rendering
{
    [Serializable]
    public sealed class BattleEffectPulse
    {
        [SerializeField][Tooltip("Big / small cycles per second (0 = off)")][Min(0f)] private float rate = 0f;
        [SerializeField][Tooltip("Size change on the second half of each cycle (e.g. 0.25 = 25% bigger, -0.25 = 25% smaller)")] private float amountFraction = 0.25f;
        [SerializeField][Tooltip("Shifts the pulse by this fraction of a cycle")][Range(0f, 1f)] private float phaseFraction = 0f;

        // Editor Field References
        public const string rateField = nameof(rate);

        internal void WriteData(Color[] texels, int rowStart)
        {
            texels[rowStart + BattleEffectDataLayout.pulse] = new Color(rate, amountFraction, phaseFraction, 0f);
        }
    }
}

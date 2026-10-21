using System;
using UnityEngine;

namespace LowDefMustard.Rendering
{
    [Serializable]
    public sealed class BattleEffectStripes
    {
        // Notes:
        //  - Parallel band masks are fixed to the effect, not the item, e.g. think like scanlines / interlaced shading
        //  - Two items w/ duty 0.5 + phases 0.5 apart interleave exactly (their overlap is solid)
        //  - Bands are anchored on the effect's centre

        [SerializeField][Tooltip("Band + gap pairs per effect unit (0 = off;  e.g. 112 = every other line of a 224-line screen)")][Min(0f)] private float stripesPerEU = 0f;
        [SerializeField][Tooltip("0 = horizontal bands (counter-clockwise)")] private float angleDegrees = 0f;
        [SerializeField][Tooltip("Each band's share of its stripe (0.5 = bands as wide as the gaps)")][Range(0f, 1f)] private float dutyFraction = 0.5f;
        [SerializeField][Tooltip("Shifts the bands by this fraction of a stripe (0.5 = into the gaps)")][Range(0f, 1f)] private float phaseFraction = 0f;

        internal void WriteData(Color[] texels, int rowStart, float effectScale)
        {
            float scaledStripes = effectScale > 0f ? stripesPerEU / effectScale : 0f;
            texels[rowStart + BattleEffectDataLayout.stripes] = new Color(scaledStripes, angleDegrees * Mathf.Deg2Rad, dutyFraction, phaseFraction);
        }
    }
}

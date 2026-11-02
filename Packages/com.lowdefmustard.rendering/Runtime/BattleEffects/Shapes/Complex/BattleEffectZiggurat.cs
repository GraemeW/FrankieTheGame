using System;
using UnityEngine;

namespace LowDefMustard.Rendering
{
    [Serializable]
    [BattleEffectShapeId(12)]
    public sealed class BattleEffectZiggurat : BattleEffectShape
    {
        // Note:
        //  - A horizontal band (+/- base half-height) stepping up into tiers at its centre, mirrored top / bottom
        //      - The band closes to a point at +/- half-length (on the riser slope)
        //      - a long half-length runs it off-screen

        [SerializeField][Tooltip("The band's half height beyond the tiers")] private BattleEffectFloatRange baseHalfHeightSU = new(0.2f);
        [SerializeField][Tooltip("Steps up from the base to the top tread (rounded;  up to 16)")] private BattleEffectFloatRange tiers = new(2f);
        [SerializeField][Tooltip("Each riser's height")] private BattleEffectFloatRange riserSizeSU = new(0.1f);
        [SerializeField][Tooltip("Each tread's width between risers")] private BattleEffectFloatRange treadWidthSU = new(0.3f);
        [SerializeField][Tooltip("Half the top tread's width")] private BattleEffectFloatRange topHalfWidthSU = new(0.2f);
        [SerializeField][Tooltip("Where the band closes to a point")] private BattleEffectFloatRange halfLengthSU = new(1f);
        [SerializeField][Tooltip("Riser width / height (1 = 45 degrees, 0 = vertical)")] private BattleEffectFloatRange riserSlopeRatio = new(1f);

        public override void GetParameters(BattleEffectFloatRange[] parameters)
        {
            parameters[0] = baseHalfHeightSU;
            parameters[1] = tiers;
            parameters[2] = riserSizeSU;
            parameters[3] = treadWidthSU;
            parameters[4] = topHalfWidthSU;
            parameters[5] = halfLengthSU;
            parameters[6] = riserSlopeRatio;
        }
    }
}

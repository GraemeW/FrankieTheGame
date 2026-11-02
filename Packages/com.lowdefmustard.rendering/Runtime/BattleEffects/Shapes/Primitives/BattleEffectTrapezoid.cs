using System;
using UnityEngine;

namespace LowDefMustard.Rendering
{
    [Serializable]
    [BattleEffectShapeId(13)]
    public sealed class BattleEffectTrapezoid : BattleEffectShape
    {
        // Note:
        //  - Parallel edges horizontal at y = -1 (bottom) + y = +1 (top)
        //  - Equal widths = a box, a zero width = a triangle

        [SerializeField][Tooltip("Half the bottom edge's length (negative = 0)")] private BattleEffectFloatRange bottomHalfWidthSU = new(1f);
        [SerializeField][Tooltip("Half the top edge's length (negative = 0)")] private BattleEffectFloatRange topHalfWidthSU = new(0.5f);

        public override void GetParameters(BattleEffectFloatRange[] parameters)
        {
            parameters[0] = bottomHalfWidthSU;
            parameters[1] = topHalfWidthSU;
        }
    }
}

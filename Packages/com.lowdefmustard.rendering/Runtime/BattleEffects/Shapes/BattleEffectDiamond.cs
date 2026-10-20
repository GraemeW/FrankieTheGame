using System;
using UnityEngine;

namespace LowDefMustard.Rendering
{
    [Serializable]
    [BattleEffectShapeId(2)]
    public sealed class BattleEffectDiamond : BattleEffectShape
    {
        // Note:
        //  - Points at x = +/- 1 (i.e. its half-width is the item's scale)
        //  - Elongate the item for a long hexagon

        [SerializeField][Tooltip("Height / width (1 = square diamond)")] private BattleEffectFloatRange aspect = new(1f);

        public override void GetParameters(BattleEffectFloatRange[] parameters)
        {
            parameters[0] = aspect;
        }
    }
}

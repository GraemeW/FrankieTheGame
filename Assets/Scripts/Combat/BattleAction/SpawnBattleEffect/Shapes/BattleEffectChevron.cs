using System;
using UnityEngine;

namespace Frankie.Combat
{
    [Serializable]
    [BattleEffectShapeId(4)]
    public sealed class BattleEffectChevron : BattleEffectShape
    {
        // Note:
        //  - A '^' spanning x = -1 to 1 (point up;  rotate the item to aim it)
        //  - Draw w/ the outline style for thickness

        [SerializeField][Tooltip("How far the arms sweep back from the point (in half-widths)")] private BattleEffectFloatRange depth = new(0.6f);

        public override void GetParameters(BattleEffectFloatRange[] parameters)
        {
            parameters[0] = depth;
        }
    }
}

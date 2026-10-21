using System;
using UnityEngine;

namespace LowDefMustard.Rendering
{
    [Serializable]
    [BattleEffectShapeId(10)]
    public sealed class BattleEffectPolygon : BattleEffectShape
    {
        // Notes:
        //  - Regular polygons only (more complex variations have their own standalone shape entry)
        //  - Corners at radius 1 (i.e. the item's scale) + one corner pointing up (rotate the item to turn it)

        [SerializeField][Min(3)] private int sides = 6;

        public override void GetParameters(BattleEffectFloatRange[] parameters)
        {
            parameters[0] = new BattleEffectFloatRange(sides);
        }
    }
}

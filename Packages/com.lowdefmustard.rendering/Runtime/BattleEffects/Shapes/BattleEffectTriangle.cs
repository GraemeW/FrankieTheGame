using System;
using UnityEngine;

namespace LowDefMustard.Rendering
{
    [Serializable]
    [BattleEffectShapeId(9)]
    public sealed class BattleEffectTriangle : BattleEffectShape
    {
        // Note:
        //  - Isosceles, pointing up (rotate the item to aim it), centred on its bounding box
        //  - Base corners at x = +/- 1, apex at y = + aspect ratio, base at y = - aspect ratio
        //  - For an equilateral triangle centred on its middle (not its bounding box), use a 3-sided polygon

        [SerializeField][Tooltip("Height / base width (0.866 = equilateral)")] private BattleEffectFloatRange aspectRatio = new(0.866f);

        public override void GetParameters(BattleEffectFloatRange[] parameters)
        {
            parameters[0] = aspectRatio;
        }
    }
}

using System;
using UnityEngine;

namespace LowDefMustard.Rendering
{
    [Serializable]
    [BattleEffectShapeId(7)]
    public sealed class BattleEffectLens : BattleEffectShape
    {
        // Note:
        //  - A pointed ellipse (eye / leaf / spark) w/ its tips at x = +/- 1 (i.e. its half-width is the item's scale)
        //  - Made of two arcs, so the aspect ratio is capped at 1 (a circle)

        [SerializeField][Tooltip("Height / width (capped at 1 = circle)")] private BattleEffectFloatRange aspectRatio = new(0.4f);

        public override void GetParameters(BattleEffectFloatRange[] parameters)
        {
            parameters[0] = aspectRatio;
        }
    }
}

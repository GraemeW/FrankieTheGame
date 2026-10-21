using System;
using UnityEngine;

namespace LowDefMustard.Rendering
{
    [Serializable]
    [BattleEffectShapeId(8)]
    public sealed class BattleEffectBox : BattleEffectShape
    {
        // Note:
        //  - Edges at x = +/- 1 (i.e. its half-width is the item's scale)
        //  - Full-screen fill:  stretch fit, aspect ratio 1, scale a little over 0.5 (e.g. 0.52 ~ at exactly 0.5 the antialiased edge lands on the border)
        //  - Corners round inward (the box keeps its size) ~ radius is capped at the shorter half side (= a capsule)

        [SerializeField][Tooltip("Height / width (1 = square)")] private BattleEffectFloatRange aspectRatio = new(1f);
        [SerializeField][Tooltip("0 = sharp corners")] private BattleEffectFloatRange cornerRadiusSU = new(0f);

        public override void GetParameters(BattleEffectFloatRange[] parameters)
        {
            parameters[0] = aspectRatio;
            parameters[1] = cornerRadiusSU;
        }
    }
}

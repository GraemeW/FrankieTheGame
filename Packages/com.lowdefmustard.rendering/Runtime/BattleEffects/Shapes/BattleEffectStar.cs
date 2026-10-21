using System;
using UnityEngine;

namespace LowDefMustard.Rendering
{
    [Serializable]
    [BattleEffectShapeId(11)]
    public sealed class BattleEffectStar : BattleEffectShape
    {
        // Note:
        //  - Tips at radius 1 (i.e. the item's scale), one pointing up (rotate the item to turn it)
        //  - Inner ratio = how far in the corners between the tips sit:  lower = spikier

        [SerializeField][Min(2)] private int points = 5;
        [SerializeField][Tooltip("Inner corner radius / tip radius (0.38 = standard five-pointed star)")] private BattleEffectFloatRange innerRatio = new(0.4f);

        public override void GetParameters(BattleEffectFloatRange[] parameters)
        {
            parameters[0] = new BattleEffectFloatRange(points);
            parameters[1] = innerRatio;
        }
    }
}

using System;
using UnityEngine;

namespace LowDefMustard.Rendering
{
    [Serializable]
    [BattleEffectShapeId(14)]
    public sealed class BattleEffectChamferedBox : BattleEffectShape
    {
        // Note:
        //  - Top right can step:  top edge -> chamfer -> a flat step -> lower chamfer -> right edge

        [SerializeField] private BattleEffectFloatRange halfWidthSU = new(1f);
        [SerializeField] private BattleEffectFloatRange halfHeightSU = new(1f);
        [SerializeField][Tooltip("Top left corner cut")] private BattleEffectFloatRange topLeftChamferSU = new(0.25f);
        [SerializeField][Tooltip("Bottom left corner cut")] private BattleEffectFloatRange bottomLeftChamferSU = new(0.25f);
        [SerializeField][Tooltip("Bottom right corner cut")] private BattleEffectFloatRange bottomRightChamferSU = new(0.25f);
        [SerializeField][Tooltip("Top right corner cut, from the top edge")] private BattleEffectFloatRange topRightChamferSU = new(0.25f);
        [SerializeField][Tooltip("The flat run below the top right chamfer (0 = no step)")] private BattleEffectFloatRange topRightStepWidthSU = new(0f);
        [SerializeField][Tooltip("The chamfer from the step down to the right edge")] private BattleEffectFloatRange topRightLowerChamferSU = new(0f);

        public override void GetParameters(BattleEffectFloatRange[] parameters)
        {
            parameters[0] = halfWidthSU;
            parameters[1] = halfHeightSU;
            parameters[2] = topLeftChamferSU;
            parameters[3] = bottomLeftChamferSU;
            parameters[4] = bottomRightChamferSU;
            parameters[5] = topRightChamferSU;
            parameters[6] = topRightStepWidthSU;
            parameters[7] = topRightLowerChamferSU;
        }
    }
}

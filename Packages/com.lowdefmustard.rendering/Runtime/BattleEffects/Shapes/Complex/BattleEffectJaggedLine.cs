using System;
using UnityEngine;

namespace LowDefMustard.Rendering
{
    [Serializable]
    [BattleEffectShapeId(11)]
    public sealed class BattleEffectJaggedLine : BattleEffectShape
    {
        // Note:
        //  - A flat line along the item's local x-axis, w/ a triangular bump (up, down or flat ~ seeded) in each cell, round ends
        //      - Keep the item's scale uniform (e.g. 1, so shape units = effect units)
        //      - Cells are centred on the item's position, so a bump half width of 0 bumps the middle cell only
        //      - A seed range + the item's progress steps re-rolls the pattern per step
        //      - Scroll animates the bumps along the line
        //  - Draw w/ the outline style for thickness

        [SerializeField][Tooltip("How far the line extends either side of its centre")] private BattleEffectFloatRange halfLengthSU = new(1f);
        [SerializeField][Tooltip("Width of each bump")] private BattleEffectFloatRange cellWidthSU = new(0.25f);
        [SerializeField][Tooltip("Height of each bump")] private BattleEffectFloatRange amplitudeSU = new(0.1f);
        [SerializeField][Tooltip("Fraction of cells left flat (0 = every cell bumps, 1 = a straight line)")] private BattleEffectFloatRange calmFraction = new(0.3f);
        [SerializeField][Tooltip("Picks the bump pattern (whole numbers;  the same seed = the same pattern)")] private BattleEffectFloatRange seed = new(0f);
        [SerializeField][Tooltip("Only cells centred within this distance of the centre bump (negative = none, i.e. a straight line)")] private BattleEffectFloatRange bumpHalfWidthSU = new(1f);
        [SerializeField][Tooltip("Shrinks each bump by a random fraction, up to all of it (0 = every bump at full amplitude)")] private BattleEffectFloatRange variationFraction = new(0f);
        [SerializeField][Tooltip("Slides the bumps along the line")] private BattleEffectFloatRange scrollSU = new(0f);

        public override void GetParameters(BattleEffectFloatRange[] parameters)
        {
            parameters[0] = halfLengthSU;
            parameters[1] = cellWidthSU;
            parameters[2] = amplitudeSU;
            parameters[3] = calmFraction;
            parameters[4] = seed;
            parameters[5] = bumpHalfWidthSU;
            parameters[6] = variationFraction;
            parameters[7] = scrollSU;
        }
    }
}

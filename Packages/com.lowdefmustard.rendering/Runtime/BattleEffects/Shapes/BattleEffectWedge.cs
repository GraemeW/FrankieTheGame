using System;
using UnityEngine;

namespace LowDefMustard.Rendering
{
    [Serializable]
    [BattleEffectShapeId(7)]
    public sealed class BattleEffectWedge : BattleEffectShape
    {
        // Note:
        //  - An infinite 'V' (filled) opening up from its apex (mirror y for a bow-tie)
        //  - Over 90 degrees, this will fill all but a downward wedge (135 + mirror y = outside a square diamond)
        //  - The apex offset may cross zero (a mirrored bow-tie's halves then merge)

        [SerializeField][Tooltip("Each side's angle from straight up, 0-180 (45 = a right-angled V)")] private BattleEffectFloatRange halfAngleDegrees = new(45f);
        [SerializeField][Tooltip("Apex height above the item's position")] private BattleEffectFloatRange apexOffsetSU = new(0f);

        public override void GetParameters(BattleEffectFloatRange[] parameters)
        {
            parameters[0] = halfAngleDegrees;
            parameters[1] = apexOffsetSU;
        }
    }
}

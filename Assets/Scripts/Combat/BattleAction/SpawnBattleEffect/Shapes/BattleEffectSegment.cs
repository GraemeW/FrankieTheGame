using System;

namespace Frankie.Combat
{
    [Serializable]
    [BattleEffectShapeId(3)]
    public sealed class BattleEffectSegment : BattleEffectShape
    {
        // Note:
        //  - A straight line from x = -1 to 1 (i.e. its half-length is the item's scale)
        //  - Draw w/ the outline style for thickness

        public override void GetParameters(BattleEffectFloatRange[] parameters) { }
    }
}

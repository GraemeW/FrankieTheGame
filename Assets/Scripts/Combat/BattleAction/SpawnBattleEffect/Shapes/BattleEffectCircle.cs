using System;

namespace Frankie.Combat
{
    [Serializable]
    [BattleEffectShapeId(0)]
    public sealed class BattleEffectCircle : BattleEffectShape
    {
        // Note:
        //  - Unit circle ~ its radius is the item's scale

        public override void GetParameters(BattleEffectFloatRange[] parameters) { }
    }
}

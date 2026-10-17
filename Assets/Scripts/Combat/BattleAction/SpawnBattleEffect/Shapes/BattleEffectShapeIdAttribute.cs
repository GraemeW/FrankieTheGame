using System;

namespace Frankie.Combat
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class BattleEffectShapeIdAttribute : Attribute
    {
        // Note:
        //  - Stable shape ID, used in the item data texture + generated HLSL dispatch
        //  - MUST BE UNIQUE PER SHAPE

        public int id { get; }

        public BattleEffectShapeIdAttribute(int id)
        {
            this.id = id;
        }
    }
}

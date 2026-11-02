using System;

namespace LowDefMustard.Rendering
{
    [AttributeUsage(AttributeTargets.Enum, Inherited = false)]
    public sealed class BattleEffectMirroredEnumAttribute : Attribute
    {
        // Note:  Mirrors the enum's values into HLSL as defines (via BattleEffectDispatchGenerator)
        //  e.g. BattleEffectStyleMode.Outline -> BATTLE_EFFECT_STYLEMODE_OUTLINE
    }
}

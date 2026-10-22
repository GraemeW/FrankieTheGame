using System;

namespace LowDefMustard.Rendering
{
    [AttributeUsage(AttributeTargets.Enum, Inherited = false)]
    public sealed class BattleEffectMirroredEnumAttribute : Attribute
    {
        // Note:  Mirrors the enum's values into HLSL as defines (via BattleEffectDispatchGenerator)
        //  e.g. BattleEffectStyle.Outline -> BATTLE_EFFECT_STYLE_OUTLINE
    }
}

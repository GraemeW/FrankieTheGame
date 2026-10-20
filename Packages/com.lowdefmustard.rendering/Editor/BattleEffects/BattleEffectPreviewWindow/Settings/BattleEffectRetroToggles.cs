#if UNITY_EDITOR
using System;

namespace LowDefMustard.Rendering.Editor
{
    [Serializable]
    public sealed class BattleEffectRetroToggles
    {
        // These are just preview / export overrides to compare an effect w/ + w/o its retro settings

        public bool isSteppedTime = true;
        public bool isFlickerEnabled = true;
        public bool isJitterEnabled = true;

        public float GetFrameRate(BattleEffectRecipe recipe) => isSteppedTime && recipe != null ? recipe.GetFrameRate() : 0f;
    }
}
#endif

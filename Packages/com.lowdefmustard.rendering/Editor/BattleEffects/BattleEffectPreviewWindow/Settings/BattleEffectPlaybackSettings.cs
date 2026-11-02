#if UNITY_EDITOR
using System;

namespace LowDefMustard.Rendering.Editor
{
    [Serializable]
    public sealed class BattleEffectPlaybackSettings
    {
        public float speed = 1f;
        public bool isLooping = true;
    }
}
#endif

#if UNITY_EDITOR
using System;

namespace Frankie.Combat.Editor
{
    [Serializable]
    public sealed class BattleEffectPlaybackSettings
    {
        public float speed = 1f;
        public bool isLooping = true;
    }
}
#endif

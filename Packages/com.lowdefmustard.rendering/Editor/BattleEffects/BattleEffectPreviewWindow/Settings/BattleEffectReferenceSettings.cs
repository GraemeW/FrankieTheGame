#if UNITY_EDITOR
using System;

namespace LowDefMustard.Rendering.Editor
{
    [Serializable]
    public sealed class BattleEffectReferenceSettings
    {
        // Note:  A folder of reference frames (PNGs) to compare against, played at its own frame rate

        public string folder = "";
        public float frameRate = 30f;
        public BattleEffectReferenceView view = BattleEffectReferenceView.Off;
        public float overlayOpacity = 0.5f;
    }
}
#endif

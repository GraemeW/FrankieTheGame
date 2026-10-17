#if UNITY_EDITOR
using UnityEngine;

namespace Frankie.Combat.Editor
{
    public readonly struct BattleEffectExportRequest
    {
        // Note:
        //  - Frames are sampled at frame rate, mid-frame)
        //  - transparent = no background, w/ alpha

        public Vector2Int size { get; }
        public float frameRate { get; }
        public int frameCount { get; }
        public string folder { get; }
        public bool isTransparent { get; }

        public BattleEffectExportRequest(Vector2Int size, float frameRate, int frameCount, string folder, bool isTransparent)
        {
            this.size = size;
            this.frameRate = frameRate;
            this.frameCount = frameCount;
            this.folder = folder;
            this.isTransparent = isTransparent;
        }
    }
}
#endif

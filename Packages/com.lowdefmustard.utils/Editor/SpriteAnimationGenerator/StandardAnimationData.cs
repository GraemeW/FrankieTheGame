using UnityEngine;

namespace LowDefMustard.Utils.Editor
{
    public class StandardAnimationData
    {
        public readonly string clipAssetPath;
        public readonly string clipName;
        public readonly Sprite[] sprites;

        public StandardAnimationData(string clipAssetPath, string clipName, Sprite[] sprites)
        {
            this.clipAssetPath = clipAssetPath;
            this.clipName = clipName;
            this.sprites = sprites;
        }
    }
}

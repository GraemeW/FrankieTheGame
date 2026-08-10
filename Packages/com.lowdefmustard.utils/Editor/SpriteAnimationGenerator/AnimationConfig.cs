using UnityEditor;

namespace LowDefMustard.Utils.Editor
{
    public class AnimationConfig
    {
        public readonly float frameRate;
        public readonly EditorCurveBinding spriteBinding;
        public readonly AnimationClipSettings refSettings;

        public AnimationConfig(float frameRate, EditorCurveBinding spriteBinding, AnimationClipSettings refSettings)
        {
            this.frameRate = frameRate;
            this.spriteBinding = spriteBinding;
            this.refSettings = refSettings;
        }
    }
}

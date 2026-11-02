using System;
using UnityEngine;

namespace LowDefMustard.Rendering
{
    [Serializable]
    public sealed class BattleEffectBeat
    {
        [SerializeField][Tooltip("After the item starts (unused under a beat pattern)")][Min(0f)] private float startSeconds;
        [SerializeField][Tooltip("Added to the item's position")] private Vector2 offsetEU;
        [SerializeField][Tooltip("Added to the item's elongation (e.g. a longer / shorter stroke)")] private Vector2 elongationSU;
        [SerializeField][Tooltip("Multiplies the item's scale")][Min(0f)] private float scaleRatio = 1f;
        [SerializeField][Tooltip("Palette steps added to the colour cycle's (0 = the item's colour / its cycle, 1 = colour B, ...)")][Min(0)] private int colourIndex;

        // Editor Field References
        public const string startSecondsField = nameof(startSeconds);

        public BattleEffectBeat() { }
        public BattleEffectBeat(float startSeconds, Vector2 offsetEU, Vector2 elongationSU, int colourIndex, float scaleRatio = 1f)
        {
            this.startSeconds = startSeconds;
            this.offsetEU = offsetEU;
            this.elongationSU = elongationSU;
            this.colourIndex = colourIndex;
            this.scaleRatio = scaleRatio;
        }

        public float GetStartSeconds() => startSeconds;
        public Vector2 GetOffset() => offsetEU;
        public Vector2 GetElongation() => elongationSU;
        public int GetColourIndex() => colourIndex;
        public float GetScaleRatio() => scaleRatio;
    }
}

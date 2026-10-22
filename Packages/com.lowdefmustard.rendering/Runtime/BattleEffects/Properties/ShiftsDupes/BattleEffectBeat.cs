using System;
using UnityEngine;

namespace LowDefMustard.Rendering
{
    [Serializable]
    public struct BattleEffectBeat
    {
        // Note:  holds from its start until the next beat starts (or the item ends)

        [SerializeField][Tooltip("After the item starts")][Min(0f)] private float startSeconds;
        [SerializeField][Tooltip("Added to the item's position")] private Vector2 offsetEU;
        [SerializeField][Tooltip("Added to the item's elongation (e.g. a longer / shorter stroke)")] private Vector2 elongationSU;
        [SerializeField][Tooltip("Palette steps added to the colour cycle's (0 = the item's colour / its cycle, 1 = colour B, ...)")][Min(0)] private int colourIndex;

        public BattleEffectBeat(float startSeconds, Vector2 offsetEU, Vector2 elongationSU, int colourIndex)
        {
            this.startSeconds = startSeconds;
            this.offsetEU = offsetEU;
            this.elongationSU = elongationSU;
            this.colourIndex = colourIndex;
        }

        public float GetStartSeconds() => startSeconds;
        public Vector2 GetOffset() => offsetEU;
        public Vector2 GetElongation() => elongationSU;
        public int GetColourIndex() => colourIndex;
    }
}

using System;
using UnityEngine;

namespace LowDefMustard.Rendering
{
    [Serializable]
    public sealed class BattleEffectClip
    {
        // Notes:
        //  - Cuts-out occurs after its style + repetition
        //  - In shape units ->  scales + rotates w/ the item (half size 1 = the item's scale)

        [SerializeField] private BattleEffectClipShape shape = BattleEffectClipShape.None;
        [SerializeField][Tooltip("Half width / height (a diamond's points);  a ring:  x = outer radius, y = inner radius")][BattleEffectAxisLabelsWhen(nameof(shape), (int)BattleEffectClipShape.Ring, "OR", "IR")] private BattleEffectVector2Range halfSizeSU = new(Vector2.one);
        [SerializeField][Tooltip("Moves the clip's centre from the item's position (before the angle turns it)")] private BattleEffectVector2Range offsetSU = new(Vector2.zero);
        [SerializeField][Tooltip("Turns the clip shape in the item's local space (counter-clockwise)")] private float angleDegrees = 0f;

        // Editor Field References
        public const string shapeField = nameof(shape);
        public const string halfSizeSUField = nameof(halfSizeSU);
        public const string offsetSUField = nameof(offsetSU);
        public const string angleDegreesField = nameof(angleDegrees);

        internal void WriteData(Color[] texels, int rowStart)
        {
            texels[rowStart + BattleEffectDataLayout.clip] = new Color((int)shape, angleDegrees * Mathf.Deg2Rad, 0f, 0f);
            texels[rowStart + BattleEffectDataLayout.clipSize] = new Color(halfSizeSU.GetStart().x, halfSizeSU.GetStart().y, halfSizeSU.GetEnd().x, halfSizeSU.GetEnd().y);
            texels[rowStart + BattleEffectDataLayout.clipOffset] = new Color(offsetSU.GetStart().x, offsetSU.GetStart().y, offsetSU.GetEnd().x, offsetSU.GetEnd().y);
        }
    }
}

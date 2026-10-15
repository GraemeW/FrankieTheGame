using System;
using UnityEngine;

namespace Frankie.Combat
{
    [Serializable]
    public sealed class BattleEffectCircle
    {
        // Note:
        //  - Start -> end values run linearly over the shape's duration
        //  - Positions / sizes are in UV (0-1 over the effect, v upwards)

        [SerializeField] private Vector2 position = new(0.5f, 0.5f);
        [SerializeField] private bool isFilled = false;
        [SerializeField][Min(0f)] private float startRadius = 0f;
        [SerializeField][Min(0f)] private float endRadius = 0.3f;
        [SerializeField][Tooltip("Ring only;  end below zero to fade the ring out through the glow")] private float startThickness = 0.05f;
        [SerializeField][Tooltip("Ring only;  end below zero to fade the ring out through the glow")] private float endThickness = 0f;

        public void GetParameters(out bool isVariant, out Vector4 paramsA, out Vector4 paramsB)
        {
            isVariant = isFilled;
            paramsA = new Vector4(position.x, position.y, startRadius, endRadius);
            paramsB = new Vector4(startThickness, endThickness, 0f, 0f);
        }
    }
}

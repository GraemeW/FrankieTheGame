using UnityEngine;

namespace Frankie.Control
{
    public struct MovementAnimationParameters
    {
        public readonly float speed;
        public readonly float xLookDirection;
        public readonly float yLookDirection;
        public Vector2 pixelPerfectOffset;

        public MovementAnimationParameters(float speed, float xLookDirection, float yLookDirection, Vector2 pixelPerfectOffset)
        {
            this.speed = speed;
            this.xLookDirection = xLookDirection;
            this.yLookDirection = yLookDirection;
            this.pixelPerfectOffset = pixelPerfectOffset;
        }
    }
}

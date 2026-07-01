using UnityEngine;

namespace Frankie.Control
{
    public struct MovementAnimationParameters
    {
        public readonly float speed;
        public readonly Vector2 lookDirection;
        public Vector2 pixelPerfectOffset;

        public MovementAnimationParameters(float speed, Vector2 lookDirection, Vector2 pixelPerfectOffset)
        {
            this.speed = speed;
            this.lookDirection = lookDirection;
            this.pixelPerfectOffset = pixelPerfectOffset;
        }
    }
}

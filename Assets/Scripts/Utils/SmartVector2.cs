using UnityEngine;

namespace Frankie.Utils
{
    public class SmartVector2
    {
        public Vector2 vector2;

        public SmartVector2(Vector2 vector2)
        {
            this.vector2 = vector2;
        }

        public static bool CheckDistance(Vector2 a, Vector2 b, float distanceThreshold)
        {
            return (a - b).sqrMagnitude < distanceThreshold * distanceThreshold;
        }
     }
}

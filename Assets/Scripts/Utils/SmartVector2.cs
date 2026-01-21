using UnityEngine;

namespace Frankie.Utils
{
    public static class SmartVector2
    {
        public static bool CheckDistance(Vector2 a, Vector2 b, float distanceThreshold)
        {
            return CheckDistance(a, b, distanceThreshold, out var _);
        }
        
        public static bool CheckDistance(Vector2 a, Vector2 b, float distanceThreshold, out float squareMagnitudeDelta)
        {
            squareMagnitudeDelta = (a - b).sqrMagnitude;
            return squareMagnitudeDelta < distanceThreshold * distanceThreshold;
        }
     }
}

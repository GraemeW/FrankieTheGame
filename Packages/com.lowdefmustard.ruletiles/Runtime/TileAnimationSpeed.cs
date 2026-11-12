using UnityEngine;

namespace LowDefMustard.RuleTiles
{
    internal static class TileAnimationSpeed
    {
        // Unity's tiles roll Random.Range(min, max) even for a fixed speed, which drifts (e.g. 1.5 -> 1.50000012) on each tile refresh and dirties scenes
        //  -> for animated tiles with fixed speed, resolve directly to avoid precision issue
        
        internal static float Resolve(float minSpeed, float maxSpeed)
        {
            return Mathf.Approximately(minSpeed, maxSpeed) ? minSpeed : Random.Range(minSpeed, maxSpeed);
        }
    }
}

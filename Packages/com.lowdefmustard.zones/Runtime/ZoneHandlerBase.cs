using UnityEngine;

namespace LowDefMustard.Zones
{
    public class ZoneHandlerBase : MonoBehaviour
    {
        // Note:  Internal fields/methods for test visibility
        
        // Tunables
        [Header("Zone Handler Base Parameters")]
        [SerializeField] protected internal ZoneNode zoneNode;
        [SerializeField] protected internal Transform warpTransform;
        
        // Methods
        public ZoneNode GetZoneNode() => zoneNode;
        public Vector3 GetWarpPosition() => warpTransform != null ? warpTransform.position : transform.position;
        protected bool HasWarpPosition() => warpTransform != null;
    }
}

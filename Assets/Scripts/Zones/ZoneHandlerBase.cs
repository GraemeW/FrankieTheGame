using UnityEngine;

namespace Frankie.ZoneManagement
{
    public class ZoneHandlerBase : MonoBehaviour
    {
        // Tunables
        [Header("Zone Handler Base Parameters")]
        [SerializeField] protected ZoneNode zoneNode;
        [SerializeField] protected Transform warpTransform;
        
        // Methods
        public ZoneNode GetZoneNode() => zoneNode;
        public Vector3 GetWarpPosition() => warpTransform != null ? warpTransform.position : transform.position;
        protected bool HasWarpPosition() => warpTransform != null;
    }
}

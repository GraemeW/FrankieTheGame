using UnityEngine;

namespace LowDefMustard.Control
{
    public class PatrolPathWaypoint : MonoBehaviour
    {
        // Tunables
        [SerializeField] private WaypointType waypointType = WaypointType.Move;

        // Public Methods
        public WaypointType GetWaypointType() => waypointType;
    }
}

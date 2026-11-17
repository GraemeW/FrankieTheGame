using UnityEngine;

namespace LowDefMustard.Control
{
    public class PatrolPathWaypoint : MonoBehaviour
    {
        // Tunables
        [SerializeField] private WaypointType waypointType = WaypointType.Move;

        // Editor Field References
        internal const string waypointTypeField = nameof(waypointType);

        // Public Methods
        public WaypointType GetWaypointType() => waypointType;
    }
}

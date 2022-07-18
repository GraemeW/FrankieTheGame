using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Control
{
    public class PatrolPathWaypoint : MonoBehaviour
    {
        // Tunables
        [SerializeField] WaypointType waypointType = WaypointType.Move;

        // Public Methods
        public WaypointType GetWaypointType() => waypointType;
    }
}

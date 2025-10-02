using UnityEngine;

namespace Frankie.Control
{
    public class PatrolPath : MonoBehaviour
    {
        // Tunables
        [Header("Behavior")]
        [SerializeField] PatrolPathWaypoint[] waypoints = null;
        [SerializeField] bool looping = true;
        [SerializeField] bool returnToFirstWaypoint = true;
        [Header("Gizmo Properties")]
        [SerializeField] float waypointGizmoSphereRadius = 0.1f;
        [SerializeField] Color waypointGizmoColor = new Color(0.5f, 1f, 1f);
        [SerializeField] Color startSphereColor = new Color(0.2f, 0.8f, 0.2f);
        [SerializeField] float selectAlpha = 0.7f;
        [SerializeField] float deselectAlpha = 0.3f;

        // State
        bool loopedOnce = false;
        int incrementDirection = 1;

        private void Awake()
        {
            loopedOnce = false;
            incrementDirection = 1;
        }

        public PatrolPathWaypoint GetWaypoint(int waypointIndex)
        {
            if (waypoints == null || waypoints.Length == 0) { return null; }
            return waypoints[waypointIndex];
        }

        public int GetNextIndex(int waypointIndex, bool calledFromGizmo = false)
        {
            if (waypoints == null || waypoints.Length == 0) { return 0; }

            if (waypointIndex == waypoints.Length - 1)
            {
                if (!calledFromGizmo) { loopedOnce = true; }
                if (returnToFirstWaypoint) { return 0; } // end patrol at first index
                else { if (!calledFromGizmo) incrementDirection = -1; } // walk backward
            }
            if (!looping && loopedOnce) { return waypointIndex; } // not looping, end patrol
            if (waypointIndex == 0 && loopedOnce) { if (!calledFromGizmo) incrementDirection = 1; } // walk forward

            if (calledFromGizmo) { return waypointIndex + 1; }
            return waypointIndex + incrementDirection;
        }

        public bool IsFinalWaypoint(int waypointIndex) => waypointIndex == waypoints.Length - 1;

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            DrawPatrolPath(deselectAlpha);
        }

        public void OnDrawGizmosSelected()
        {
            DrawPatrolPath(selectAlpha);
        }

        private void DrawPatrolPath(float alphaValue)
        {
            if (waypoints == null) { return; }

            Gizmos.color = new Color(waypointGizmoColor.r, waypointGizmoColor.g, waypointGizmoColor.b, alphaValue);
            for (int waypointIndex = 0; waypointIndex < waypoints.Length; waypointIndex++)
            {
                DrawSphere(waypointIndex, alphaValue);
                DrawLine(waypointIndex);
            }
        }

        private void DrawSphere(int waypointIndex, float alphaValue)
        {
            if (waypointIndex == 0) { Gizmos.color = new Color(startSphereColor.r, startSphereColor.g, startSphereColor.b, alphaValue); }

            PatrolPathWaypoint waypoint = GetWaypoint(waypointIndex);
            if (waypoint == null) { return; }
            Gizmos.DrawSphere(waypoint.transform.position, waypointGizmoSphereRadius);
            if (waypointIndex == 0) { Gizmos.color = new Color(waypointGizmoColor.r, waypointGizmoColor.g, waypointGizmoColor.b, alphaValue); }
        }

        private void DrawLine(int waypointIndex)
        {
            if (waypoints == null || waypoints.Length <= 1) { return; }

            if (waypointIndex != waypoints.Length - 1 || returnToFirstWaypoint)
            {
                PatrolPathWaypoint waypoint = GetWaypoint(waypointIndex);
                if (waypoint == null) { return; }

                Gizmos.DrawLine(waypoint.transform.position, GetWaypoint(GetNextIndex(waypointIndex, true)).transform.position);
            }
        }
#endif
    }
}

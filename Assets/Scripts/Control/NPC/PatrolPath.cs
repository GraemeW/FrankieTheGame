using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Control
{
    public class PatrolPath : MonoBehaviour
    {
        // Tunables
        [Header("Behavior")]
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

        public Transform GetWaypoint(int waypointIndex)
        {
            if (transform.childCount == 0) { return transform; } // Safety against misconfiguration

            return transform.GetChild(waypointIndex);
        }

        public int GetNextIndex(int waypointIndex, bool calledFromGizmo = false)
        {
            if (transform.childCount == 0) { return 0; }

            if (waypointIndex == transform.childCount - 1)
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
            Gizmos.color = new Color(waypointGizmoColor.r, waypointGizmoColor.g, waypointGizmoColor.b, alphaValue);
            for (int waypointIndex = 0; waypointIndex < transform.childCount; waypointIndex++)
            {
                DrawSphere(waypointIndex, alphaValue);
                DrawLine(waypointIndex);
            }
        }

        private void DrawSphere(int waypointIndex, float alphaValue)
        {
            if (waypointIndex == 0) { Gizmos.color = new Color(startSphereColor.r, startSphereColor.g, startSphereColor.b, alphaValue); }

            Gizmos.DrawSphere(GetWaypoint(waypointIndex).position, waypointGizmoSphereRadius);
            if (waypointIndex == 0) { Gizmos.color = new Color(waypointGizmoColor.r, waypointGizmoColor.g, waypointGizmoColor.b, alphaValue); }
        }

        private void DrawLine(int waypointIndex)
        {
            if (transform.childCount == 1) { return; }

            if (waypointIndex != transform.childCount - 1 || returnToFirstWaypoint)
            {
                Gizmos.DrawLine(GetWaypoint(waypointIndex).position, GetWaypoint(GetNextIndex(waypointIndex, true)).position);
            }
        }
#endif
    }
}

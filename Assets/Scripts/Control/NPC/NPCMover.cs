using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Control
{
    public class NPCMover : Mover
    {
        // Tunables
        [SerializeField] Transform interactionCenterPoint = null;
        [Header("Patrol Properties")]
        [SerializeField] PatrolPath patrolPath = null;
        [SerializeField] float waypointDwellTime = 2.0f;

        // Cached References
        Animator animator = null;
        NPCStateHandler npcStateHandler = null;

        // State
        int currentWaypointIndex = 0;
        float timeSinceArrivedAtWaypoint = Mathf.Infinity;

        protected override void Awake()
        {
            base.Awake();
            animator = GetComponent<Animator>();
            npcStateHandler = GetComponent<NPCStateHandler>();
        }

        protected override void FixedUpdate()
        {
            if (npcStateHandler.GetNPCState() == NPCState.occupied) { return; }

            bool hasMoved = MoveToTarget();
            if (!hasMoved)
            {
                SetNextPatrolTarget();
            }
        }

        public Vector2 GetInteractionPosition()
        {
            return interactionCenterPoint.position;
        }

        public void SetLookDirectionToPlayer(PlayerStateHandler playerStateHandler) // called via Unity Event
        {
            PlayerController callingController = playerStateHandler.GetComponent<PlayerController>();
            Vector2 lookDirection = callingController.GetInteractionPosition() - (Vector2)interactionCenterPoint.position;
            SetLookDirection(lookDirection);
            UpdateAnimator();
        }

        private void SetNextPatrolTarget()
        {
            if (patrolPath != null)
            {
                currentSpeed = 0f;
                UpdateAnimator();
                if (timeSinceArrivedAtWaypoint > waypointDwellTime)
                {
                    CycleWaypoint();
                    Vector2 nextPosition = GetCurrentWaypoint();
                    SetMoveTarget(nextPosition);
                }
                timeSinceArrivedAtWaypoint += Time.deltaTime;
            }
        }

        private Vector3 GetCurrentWaypoint()
        {
            return patrolPath.GetWaypoint(currentWaypointIndex).position;
        }

        private void CycleWaypoint()
        {
            currentWaypointIndex = patrolPath.GetNextIndex(currentWaypointIndex);
            timeSinceArrivedAtWaypoint = 0;
        }

        protected override void UpdateAnimator()
        {
            animator.SetFloat("Speed", currentSpeed);
            animator.SetFloat("xLook", lookDirection.x);
            animator.SetFloat("yLook", lookDirection.y);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (patrolPath != null) { patrolPath.OnDrawGizmosSelected(); }
        }
#endif
    }
}

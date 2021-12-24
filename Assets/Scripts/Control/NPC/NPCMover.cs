using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Frankie.Control
{
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(NPCStateHandler))]
    public class NPCMover : Mover
    {
        // Tunables
        [SerializeField] Transform interactionCenterPoint = null;
        [Header("Patrol Properties")]
        [SerializeField] PatrolPath patrolPath = null;
        [SerializeField] float waypointDwellTime = 2.0f;
        [SerializeField] float giveUpOnPatrolTargetTime = 10.0f;

        // Cached References
        Animator animator = null;
        NPCStateHandler npcStateHandler = null;

        // State
        int currentWaypointIndex = 0;
        float timeSinceArrivedAtWaypoint = Mathf.Infinity;
        float timeSinceNewPatrolTarget = 0f;

        protected override void Awake()
        {
            base.Awake();
            animator = GetComponent<Animator>();
            npcStateHandler = GetComponent<NPCStateHandler>();
        }

        protected override void Start()
        {
            base.Start();
            SetNextPatrolTarget();
        }

        protected override void FixedUpdate()
        {
            if (npcStateHandler.GetNPCState() == NPCState.occupied) { return; }

            bool? hasMoved = MoveToTarget();
            if (hasMoved == null) { return; }

            if (!(bool)hasMoved)
            {
                bool isPatrolling = SetNextPatrolTarget();
                if (!isPatrolling)
                {
                    ClearMoveTargets();
                }
            }
            else
            {
                if (timeSinceNewPatrolTarget > giveUpOnPatrolTargetTime)
                {
                    ForceNextPatrolTarget();
                }
                timeSinceNewPatrolTarget += Time.deltaTime; 
            }
        }

        public Vector2 GetInteractionPosition()
        {
            if (interactionCenterPoint != null)
            {
                return interactionCenterPoint.position;
            }
            return Vector2.zero;
        }

        public void SetLookDirectionToPlayer(PlayerStateHandler playerStateHandler) // called via Unity Event
        {
            PlayerController callingController = playerStateHandler.GetComponent<PlayerController>();
            Vector2 lookDirection = callingController.GetInteractionPosition() - (Vector2)interactionCenterPoint.position;
            SetLookDirection(lookDirection);
            UpdateAnimator();
        }

        public RaycastHit2D[] NPCCastFromSelf(float raycastRadius)
        {
            RaycastHit2D[] hits = Physics2D.CircleCastAll(interactionCenterPoint.position, raycastRadius, Vector2.zero);
            return hits;
        }

        private bool SetNextPatrolTarget()
        {
            if (patrolPath == null) { return false; }

            currentSpeed = 0f;
            UpdateAnimator();
            if (timeSinceArrivedAtWaypoint > waypointDwellTime)
            {
                ForceNextPatrolTarget();
            }
            timeSinceArrivedAtWaypoint += Time.deltaTime;
            return true;
        }

        private void ForceNextPatrolTarget()
        {
            CycleWaypoint();
            Vector2 nextPosition = GetCurrentWaypoint();
            SetMoveTarget(nextPosition);
            timeSinceNewPatrolTarget = 0f;
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
            patrolPath?.OnDrawGizmosSelected();
        }
#endif
    }
}

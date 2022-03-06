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
        [Header("NPC Specific Behavior")]
        [SerializeField] Transform interactionCenterPoint = null;
        [SerializeField] PatrolPath patrolPath = null;
        [SerializeField] float waypointDwellTime = 2.0f;
        [SerializeField] float giveUpOnPatrolTargetTime = 10.0f;

        // Cached References
        Animator animator = null;
        NPCStateHandler npcStateHandler = null;

        // State
        bool movingActive = true;
        bool resetPositionOnNextIdle = false;
        Vector3 initialPosition = new Vector3();
        int currentWaypointIndex = 0;
        float timeSinceArrivedAtWaypoint = Mathf.Infinity;
        float timeSinceNewPatrolTarget = 0f;

        #region UnityMethods
        protected override void Awake()
        {
            base.Awake();
            animator = GetComponent<Animator>();
            npcStateHandler = GetComponent<NPCStateHandler>();
        }

        protected override void Start()
        {
            base.Start();
            initialPosition = transform.position;
            SetNextPatrolTarget();
        }

        private void OnEnable()
        {
            npcStateHandler.npcStateChanged += HandleNPCStateChange;
        }

        private void OnDisable()
        {
            npcStateHandler.npcStateChanged -= HandleNPCStateChange;
        }

        protected override void FixedUpdate()
        {
            if (!movingActive) { return; }

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
        #endregion

        #region PublicMethods
        public Vector2 GetInteractionPosition()
        {
            if (interactionCenterPoint != null)
            {
                return interactionCenterPoint.position;
            }
            return Vector2.zero;
        }

        public void SetLookDirectionToPlayer(PlayerStateMachine playerStateHandler) // called via Unity Event
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
        #endregion

        #region PrivateMethods
        private void HandleNPCStateChange(NPCStateType npcStateType)
        {
            movingActive = true;
            switch (npcStateType)
            {
                case NPCStateType.occupied:
                    movingActive = false;
                    return;
                case NPCStateType.suspicious:
                    ClearMoveTargets();
                    break;
                case NPCStateType.aggravated:
                case NPCStateType.frenzied:
                    resetPositionOnNextIdle = true;
                    if (!HasMoveTarget())
                    {
                        SetMoveTarget(npcStateHandler.GetPlayer());
                    }
                    break;
                case NPCStateType.idle:
                default:
                    if (resetPositionOnNextIdle) { MoveToOriginalPosition(); resetPositionOnNextIdle = false; }
                    break;
            }
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
            if (patrolPath == null) { return initialPosition; }
            return patrolPath.GetWaypoint(currentWaypointIndex).position;
        }

        private void CycleWaypoint()
        {
            if (patrolPath == null) { return; }

            currentWaypointIndex = patrolPath.GetNextIndex(currentWaypointIndex);
            timeSinceArrivedAtWaypoint = 0;
        }

        protected override void UpdateAnimator()
        {
            animator.SetFloat("Speed", currentSpeed);
            animator.SetFloat("xLook", lookDirection.x);
            animator.SetFloat("yLook", lookDirection.y);
        }
        #endregion

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            patrolPath?.OnDrawGizmosSelected();
        }
#endif
    }
}

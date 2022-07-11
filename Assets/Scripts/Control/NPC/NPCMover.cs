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
        bool movingAwayFromTarget = false;

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
                if (timeSinceNewPatrolTarget > giveUpOnPatrolTargetTime && patrolPath != null)
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

        #region OverrideMethods
        protected override Vector2 ReckonTarget()
        {
            Vector2 target = base.ReckonTarget();
            if (!movingAwayFromTarget)
            {
                return target;
            }
            else
            {
                return rigidBody2D.position * (Vector2.one - target); // Run toward equally distant position away from target
            }
        }

        protected override void UpdateAnimator()
        {
            animator.SetFloat("Speed", currentSpeed);
            animator.SetFloat("xLook", lookDirection.x);
            animator.SetFloat("yLook", lookDirection.y);
        }
        #endregion

        #region PrivateMethods
        private void HandleNPCStateChange(NPCStateType npcStateType, bool isNPCAfraid)
        {
            movingActive = true;
            movingAwayFromTarget = false;
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
                    movingAwayFromTarget = isNPCAfraid;

                    // Back down after initial aggravation (i.e. run to initiate dialogue)
                    // Otherwise, will force combat with collisions
                    if (!npcStateHandler.WillForceCombat()) { resetPositionOnNextIdle = true; } 
                        
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
        #endregion

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            patrolPath?.OnDrawGizmosSelected();
        }
#endif
    }
}

using System;
using UnityEngine;

namespace Frankie.Control
{
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(NPCStateHandler))]
    public class NPCMover : Mover
    {
        // Tunables
        [Header("NPC Specific Behavior")]
        [SerializeField] private Transform interactionCenterPoint;
        [SerializeField] private PatrolPath patrolPath;
        [SerializeField] private float waypointDwellTime = 2.0f;
        [SerializeField] private float giveUpOnPatrolTargetTime = 10.0f;

        // Cached References
        private Animator animator;
        private NPCStateHandler npcStateHandler;

        // State
        private bool movingActive = true;
        private bool resetPositionOnNextIdle = false;
        private bool movingAwayFromTarget = false;

        private int currentWaypointIndex;
        private float timeSinceArrivedAtWaypoint = Mathf.Infinity;
        private float timeSinceNewPatrolTarget;

        // Events
        public event Action arrivedAtFinalWaypoint;

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
            SetNextPatrolTarget();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
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
            switch (hasMoved)
            {
                case null:
                    return;
                case false:
                {
                    bool isPatrolling = SetNextPatrolTarget();
                    if (!isPatrolling)
                    {
                        ClearMoveTargets();
                    }
                    break;
                }
                case true:
                {
                    if (timeSinceNewPatrolTarget > giveUpOnPatrolTargetTime && patrolPath != null)
                    {
                        ForceNextPatrolTarget();
                    }
                    timeSinceNewPatrolTarget += Time.deltaTime;
                    break;
                }
            }
        }
        #endregion

        #region PublicMethods
        public Vector2 GetInteractionPosition()
        {
            return interactionCenterPoint != null ? interactionCenterPoint.position : Vector2.zero;
        }

        public void SetLookDirectionToPlayer(PlayerStateMachine playerStateHandler) // called via Unity Event
        {
            var callingController = playerStateHandler.GetComponent<PlayerController>();
            SetLookDirection(callingController.GetInteractionPosition() - (Vector2)interactionCenterPoint.position);
            UpdateAnimator();
        }

        public RaycastHit2D[] NPCCastFromSelf(float raycastRadius)
        {
            RaycastHit2D[] hits = Physics2D.CircleCastAll(interactionCenterPoint.position, raycastRadius, Vector2.zero);
            return hits;
        }

        public void SetPatrolPath(PatrolPath patrolPath)
        {
            if (patrolPath == null) { return; }
            this.patrolPath = patrolPath;
            SetNextPatrolTarget();
        }
        #endregion

        #region OverrideMethods
        protected override Vector2 ReckonTarget()
        {
            Vector2 target = base.ReckonTarget();
            if (!movingAwayFromTarget) { return target; }
            
            return rigidBody2D.position * (Vector2.one - target); // Run toward equally distant position away from target
        }

        protected override void UpdateAnimator()
        {
            // Safety on accessing controller properties before setup complete (OnEnable calls)
            if (animator.runtimeAnimatorController == null) { return; }

            SetAnimatorSpeed(animator, currentSpeed);
            SetAnimatorXLook(animator, lookDirection.x);
            SetAnimatorYLook(animator, lookDirection.y);
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
            PatrolPathWaypoint nextWaypoint = patrolPath.GetWaypoint(currentWaypointIndex);
            if (nextWaypoint == null) { return; }

            switch (nextWaypoint.GetWaypointType())
            {
                case WaypointType.Move:
                    SetMoveTarget(nextWaypoint.transform.position);
                    break;
                case WaypointType.Warp:
                    WarpToPosition(nextWaypoint.transform.position);
                    break;
            }

            timeSinceNewPatrolTarget = 0f;
        }

        private void CycleWaypoint()
        {
            if (patrolPath == null) { return; }
            if (patrolPath.IsFinalWaypoint(currentWaypointIndex)) { arrivedAtFinalWaypoint?.Invoke(); }

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

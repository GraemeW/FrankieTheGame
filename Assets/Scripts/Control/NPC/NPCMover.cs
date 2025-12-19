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
        [SerializeField] [Tooltip("Takes priority over random walk")] private PatrolPath patrolPath;
        [SerializeField] [Tooltip("Default behaviour for no patrol path")] private bool canRandomWalk = false;
        [SerializeField] private float randomWalkStepDistance = 0.4f;
        [SerializeField] private float randomWalkLimitDistance = 1.2f;
        [SerializeField] private float waypointDwellTime = 2.0f;
        [SerializeField] [Tooltip("Anything other than U/D/L/R to keep last look direction")] private PlayerInputType lookDirectionOnDwell = PlayerInputType.NavigateDown;
        [SerializeField] private float giveUpOnLocomotionTargetTime = 10.0f;
        [SerializeField] private float locomotionCollisionStayTime = 0.5f;

        [Header("Editor Gizmos")]
#if UNITY_EDITOR
        [SerializeField] private Color randomWalkGizmoColor = Color.steelBlue;
#endif

        // Cached References
        private Animator animator;
        private NPCStateHandler npcStateHandler;

        // State
        private NPCMoveFocus npcMoveFocus = NPCMoveFocus.Pending;
        private bool resetPositionOnNextIdle = false;

        private int currentWaypointIndex;
        private float timeSinceArrivedAtWaypoint = Mathf.Infinity;
        private float timeSinceNewLocomotionTarget;

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
            StartLocomotion();
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

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (npcMoveFocus is not (NPCMoveFocus.Patrolling or NPCMoveFocus.RandomWalk)) { return; }
            SetMoveTarget(transform.position);
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            if (npcMoveFocus is not (NPCMoveFocus.Patrolling or NPCMoveFocus.RandomWalk)) { return; }
            if (timeSinceNewLocomotionTarget > locomotionCollisionStayTime) { SetupNextLocomotionTarget(); }
        }

        protected override void FixedUpdate()
        {
            if (npcMoveFocus == NPCMoveFocus.Inactive) { return; }
            
            switch (MoveToTarget())
            {
                case null:
                    return;
                case false:
                {
                    if (CanLocomote()) { StartLocomotion(); }
                    else { ClearMoveTargets(); } 
                    break;
                }
                case true:
                {
                    if (timeSinceNewLocomotionTarget > giveUpOnLocomotionTargetTime && CanLocomote())
                    {
                        SetupNextLocomotionTarget();
                    }
                    timeSinceNewLocomotionTarget += Time.deltaTime;
                    break;
                }
            }
        }
        #endregion
        
        #region NPCStateHandling
        private void HandleNPCStateChange(NPCStateType npcStateType, bool isNPCAfraid)
        {
            switch (npcStateType)
            {
                case NPCStateType.Occupied:
                {
                    npcMoveFocus = NPCMoveFocus.Inactive;
                    break;
                }
                case NPCStateType.Suspicious:
                {
                    npcMoveFocus = isNPCAfraid ? NPCMoveFocus.Fleeing : NPCMoveFocus.Chasing;
                    ClearMoveTargets();
                    break;
                }
                case NPCStateType.Aggravated:
                case NPCStateType.Frenzied:
                {
                    npcMoveFocus = isNPCAfraid ? NPCMoveFocus.Fleeing : NPCMoveFocus.Chasing;
                    ClearMoveTargets();
                    
                    SetMoveTarget(npcStateHandler.GetPlayer());
                    if (!npcStateHandler.WillForceCombat()) { resetPositionOnNextIdle = true; }
                    break;
                }
                case NPCStateType.Idle:
                default:
                {
                    npcMoveFocus = NPCMoveFocus.Pending;
                    if (resetPositionOnNextIdle)
                    {
                        MoveToOriginalPosition();
                        resetPositionOnNextIdle = false;
                    }
                    break;
                }
            }
        }
        #endregion

        #region PublicMethods
        public Vector2 GetInteractionPosition() => interactionCenterPoint != null ? interactionCenterPoint.position : Vector2.zero;
        public void SetLookDirectionDown() => SetLookDirection(Vector2.down); // Called via Unity Events
        public void SetLookDirectionToPlayer(PlayerStateMachine playerStateHandler) // Called via Unity Events
        {
            var callingController = playerStateHandler.GetComponent<PlayerController>();
            SetLookDirection(callingController.GetInteractionPosition() - (Vector2)interactionCenterPoint.position);
        }

        public RaycastHit2D[] NPCCastFromSelf(float raycastRadius)
        {
            RaycastHit2D[] hits = Physics2D.CircleCastAll(interactionCenterPoint.position, raycastRadius, Vector2.zero);
            return hits;
        }

        public void SetPatrolPath(PatrolPath setPatrolPath)
        {
            if (setPatrolPath == null) { return; }
            patrolPath = setPatrolPath;
            StartLocomotion();
        }
        #endregion

        #region OverrideMethods
        protected override Vector2 ReckonTarget()
        {
            Vector2 target = base.ReckonTarget();
            if (npcMoveFocus != NPCMoveFocus.Fleeing) { return target; }
            
            float offset = Vector2.Dot(rigidBody2D.position, target);
            Vector2 direction = (rigidBody2D.position - target).normalized;
            return offset * direction; // Run toward equally distant position away from target
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
        private bool CanLocomote() => patrolPath != null || canRandomWalk;

        private void StartLocomotion()
        {
            if (npcMoveFocus is not (NPCMoveFocus.Pending or NPCMoveFocus.Patrolling or NPCMoveFocus.RandomWalk)) { return; }
            
            currentSpeed = 0f;
            SetLookDirection(lookDirectionOnDwell);
            UpdateAnimator();
            if (timeSinceArrivedAtWaypoint > waypointDwellTime)
            {
                SetupNextLocomotionTarget();
            }
            timeSinceArrivedAtWaypoint += Time.deltaTime;
        }

        private void SetupNextLocomotionTarget()
        {
            timeSinceArrivedAtWaypoint = 0;
            if (patrolPath != null)
            {
                SetupNextPatrolTarget();
                npcMoveFocus = NPCMoveFocus.Patrolling;
            }
            else if (canRandomWalk)
            {
                SetupRandomWalkTarget();
                npcMoveFocus = NPCMoveFocus.RandomWalk;
            }
            timeSinceNewLocomotionTarget = 0f;
        }

        private void SetupNextPatrolTarget()
        {
            if (patrolPath == null) { return; }
            
            CycleWaypoint();
            PatrolPathWaypoint nextWaypoint = patrolPath.GetWaypoint(currentWaypointIndex);
            if (nextWaypoint == null) { return; }

            switch (nextWaypoint.GetWaypointType())
            {
                case WaypointType.Warp:
                    WarpToPosition(nextWaypoint.transform.position);
                    break;
                case WaypointType.Move:
                default:
                    SetMoveTarget(nextWaypoint.transform.position);
                    break;
            }
        }

        private void SetupRandomWalkTarget()
        {
            Vector2 nextWalkPosition = CycleRandomWalk();
            SetMoveTarget(nextWalkPosition);
        }

        private void CycleWaypoint()
        {
            if (patrolPath == null) { return; }
            if (patrolPath.IsFinalWaypoint(currentWaypointIndex)) { arrivedAtFinalWaypoint?.Invoke(); }

            currentWaypointIndex = patrolPath.GetNextIndex(currentWaypointIndex);
        }

        private Vector2 CycleRandomWalk()
        {
            int direction = UnityEngine.Random.Range(0, 5);
            Vector2 moveDirection = direction switch
            {
                0 => Vector2.down,
                1 => Vector2.up,
                2 => Vector2.right,
                3 => Vector2.left,
                _ => Vector2.down
            };

            Vector2 nextWalkPosition = (Vector2)transform.position + moveDirection * randomWalkStepDistance;
            if (Vector2.Dot((nextWalkPosition - originalPosition), moveDirection) > randomWalkLimitDistance)
            {
                moveDirection *= -1;
                nextWalkPosition = (Vector2)transform.position + moveDirection;
            }
            return nextWalkPosition;
        }
        #endregion

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            patrolPath?.OnDrawGizmosSelected();
            if (canRandomWalk)
            {
                Gizmos.color = randomWalkGizmoColor;
                var cubeCoordinates = new Vector3(randomWalkLimitDistance * 2, randomWalkLimitDistance * 2, 0f);
                Gizmos.DrawWireCube(transform.position, cubeCoordinates);
            }
        }
#endif
    }
}

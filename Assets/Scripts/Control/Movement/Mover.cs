using System.Collections;
using UnityEngine;
using Frankie.Saving;
using Frankie.Utils;

namespace Frankie.Control
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(PathFinder))]
    public abstract class Mover : MonoBehaviour, ISaveable<SerializableVector2>
    {
        // Tunables
        [SerializeField] protected MovementConfiguration movementConfiguration;
        [SerializeField] protected Vector2 defaultLookDirection = Vector2.down;
        [SerializeField] protected float defaultTargetDistanceTolerance = 0.15f;
        [SerializeField] private float closeTargetThresholdSquared = 0.5625f;
        [SerializeField] private bool resetPositionOnEnable = false;

        // State
        protected Vector2 originalPosition;
        protected Vector2 lookDirection = Vector2.down;
        protected float currentSpeed;
        protected float timeSinceLastMove;
        
        private Vector2? moveTargetCoordinate;
        private GameObject moveTargetObject;
        private CircularBuffer<Vector2> targetMovementHistory;
        private float targetDistanceTolerance = 0.025f;
        private bool isQueuedCoroutineActive = false;
        private Coroutine queuedMoveCoroutine;

        // Cached References
        private Rigidbody2D rigidBody2D;
        private PathFinder pathFinder;

        #region Static
        public static float SignFloored(float number) => Mathf.Abs(number) < _signFloorThreshold ? 0 : Mathf.Sign(number);
        public static void SetAnimatorSpeed(Animator animator, float speed) => animator.SetFloat(_speed, speed);
        public static void SetAnimatorXLook(Animator animator, float xLookDirection) => animator.SetFloat(_xLook, xLookDirection);
        public static void SetAnimatorYLook(Animator animator, float yLookDirection) => animator.SetFloat(_yLook, yLookDirection);
        private const float _pixelsPerUnit = 100.0f; // Align to pixel art setting, default: 100
        private const float _signFloorThreshold = 0.1f;
        private static readonly int _speed = Animator.StringToHash("Speed");
        private static readonly int _xLook = Animator.StringToHash("xLook");
        private static readonly int _yLook = Animator.StringToHash("yLook");
        #endregion

        #region UnityMethods
        protected virtual void Awake()
        {
            rigidBody2D = GetComponent<Rigidbody2D>();
            pathFinder = GetComponent<PathFinder>();
            if (!CheckForConfiguration()) { return; }
            if (movementConfiguration.usingPathFinding) { pathFinder.InitialisePathfindingCache(); }
            SetupInitialState();
        }

        protected virtual void Start()
        {
            // N.B. Deliberately NOT calling clear move targets here to avoid order of operations issues
            // In some edge cases Start() can be called after Update(), which can cause shouts to fail

            SetLookDirection(defaultLookDirection); // Initialize look direction to avoid wonky
        }

        protected virtual void OnEnable()
        {
            if (!CheckForConfiguration()) { return; }
            if (!resetPositionOnEnable) { return; }
            transform.position = originalPosition;
            SetLookDirection(defaultLookDirection);
        }

        protected virtual void OnDisable()
        {
            if (queuedMoveCoroutine != null) { StopCoroutine(queuedMoveCoroutine); }
        }

        protected virtual void FixedUpdate()
        {
            if (isQueuedCoroutineActive) { return; }
            timeSinceLastMove += Time.deltaTime;
        }
        
        private void SetupInitialState()
        {
            originalPosition = transform.position;
            targetDistanceTolerance = defaultTargetDistanceTolerance;
            targetMovementHistory = new CircularBuffer<Vector2>(movementConfiguration.targetMovementHistoryLength);
        }

        private bool CheckForConfiguration()
        {
            if (movementConfiguration == null)
            {
                Debug.LogWarning($"{gameObject.name} Mover:  Movement configuration is missing");
                gameObject.SetActive(false);
                return false;
            }
            return true;
        }
        #endregion

        #region PublicMethods
        public abstract float GetCurrentSpeed();
        public Vector2 GetLookDirection() => lookDirection;
        public Vector2 GetCurrentPosition() => rigidBody2D.position;
        public float GetTimeSinceLastMove() => timeSinceLastMove;
        public void ResetTimeSinceLastMove() => timeSinceLastMove = 0;
        
        public void MoveRigidBody(Vector2 newPosition) => rigidBody2D.MovePosition(newPosition);
        public void SetLookDirection(Vector2 setLookDirection)
        {
            SetLookDirection(setLookDirection, true);
        }

        public void SetMoveTarget(Vector2 target)
        {
            moveTargetObject = null;
            moveTargetCoordinate = target;
        }

        public void SetMoveTarget(GameObject target)
        {
            moveTargetCoordinate = null;
            targetMovementHistory.Clear();
            moveTargetObject = target;
            targetDistanceTolerance = defaultTargetDistanceTolerance;
        }

        public void ClearMoveTargets()
        {
            if (HasMoveTarget()) { SetAnimationAndSpeedForMovementEnd(); }
            targetDistanceTolerance = defaultTargetDistanceTolerance;
            moveTargetCoordinate = null;
            targetMovementHistory.Clear();
            moveTargetObject = null;
        }
        
        public void WarpToPosition(Vector2 target) // Called via Unity Events
        {
            SetMoveTarget(target);
            transform.position = target;
        }

        public void MoveToOriginalPosition() // Called via Unity Events
        {
            targetDistanceTolerance = defaultTargetDistanceTolerance;
            SetMoveTarget(originalPosition);
        }

        public void QueueDelayedMoveExecution(Vector2 newPosition, float delayTime)
        {
            if (queuedMoveCoroutine != null) { StopCoroutine(queuedMoveCoroutine); }
            queuedMoveCoroutine = StartCoroutine(DelayedMove(newPosition, delayTime));
        }
        #endregion

        #region AbstractProtectedMethods
        protected abstract void UpdateAnimatorParameters(bool useCardinalLookDelay = false);
        
        protected void SetLookDirection(Vector2 setLookDirection, bool includeAnimationUpdate)
        {
            // Blend tree animation speed depends on magnitude of variable -- to avoid very quick animations, normalize
            setLookDirection.Normalize(); 
            lookDirection = setLookDirection;
            
            OnLookDirectionUpdate();
            
            if (includeAnimationUpdate) { UpdateAnimatorParameters(); }
        }

        protected static Vector2 RoundToPixelPerfect(Vector2 position)
        {
            return new Vector2(
                Mathf.Round(_pixelsPerUnit * position.x) / _pixelsPerUnit, 
                Mathf.Round(_pixelsPerUnit * position.y) / _pixelsPerUnit);
        }
        
        protected Vector2 GetSpritePositionOffset() => RoundToPixelPerfect(rigidBody2D.position) - rigidBody2D.position;

        protected virtual void OnLookDirectionUpdate() {}
        
        protected bool? MoveToTarget()
        {
            // true:  a successful move to target was performed this call
            // false:  the Mover stopped moving - i.e. due to arriving at target or an inability to move
            // null:  no attempts were made to move this call
            if (SetStaticForNoTarget() || isQueuedCoroutineActive) { return null; }
            
            Vector2 position = GetCurrentPosition();
            Vector2 target = ReckonTarget(false, true, PathFindingCheckType.Check);
            if (HasArrivedAtTarget(target, out float squareMagnitudeDelta))
            {
                if (!movementConfiguration.usingPathFinding) { return false; }
                
                Vector2 finalTarget = ReckonTarget(false, false, PathFindingCheckType.Skip);
                if (HasArrivedAtTarget(finalTarget, out float finalSquareMagnitudeDelta)) { return false;}
            }
            target = ReckonTarget(squareMagnitudeDelta > closeTargetThresholdSquared, false, PathFindingCheckType.ForceCheck);
            
            Vector2 direction = target - position;
            SetLookDirection(direction, false);
            
            currentSpeed = GetCurrentSpeed();
            if (!movementConfiguration.MoveToTarget(this, target, Time.deltaTime, out Vector2 _)) { currentSpeed = 0f; }
            UpdateAnimatorParameters(true);

            return true;
        }

        protected virtual Vector2 ReckonTarget(bool withHistoryOffsetting = true, bool addToHistory = true, PathFindingCheckType pathFindingCheckType = PathFindingCheckType.Check)
        {
            if (moveTargetCoordinate != null) { return moveTargetCoordinate.Value; }
            if (moveTargetObject == null) { return Vector2.zero; }
            
            Vector2 currentTargetPosition = moveTargetObject.transform.position;
            if (addToHistory) { targetMovementHistory.Add(currentTargetPosition); }

            Vector2 reckonedTarget = currentTargetPosition;
            if (targetMovementHistory.GetCurrentSize() > 0) { reckonedTarget = withHistoryOffsetting ? targetMovementHistory.GetLastEntry() : targetMovementHistory.GetFirstEntry(); }
            
            if (!movementConfiguration.usingPathFinding || !pathFinder.IsValidPathFinder() || pathFindingCheckType == PathFindingCheckType.Skip) { return reckonedTarget; }
            switch (movementConfiguration.movementStyle)
            {
                case MovementStyle.Warp:
                    return pathFinder.FindBestReachablePosition(GetCurrentPosition(), reckonedTarget, movementConfiguration.warpPathfindingTravelDistance);
                case MovementStyle.Walk:
                default:
                    return !pathFinder.FindPath(GetCurrentPosition(), reckonedTarget, pathFindingCheckType) ? reckonedTarget : pathFinder.GetNextPathTarget();
            }
        }
        #endregion

        #region  PrivateMethods
        private bool HasMoveTarget() => (moveTargetCoordinate != null || moveTargetObject != null);
        private bool HasArrivedAtTarget(Vector2 target, out float squareMagnitudeDelta) => SmartVector2.CheckDistance(GetCurrentPosition(), target, targetDistanceTolerance, out squareMagnitudeDelta);
        
        private bool SetStaticForNoTarget()
        {
            if (moveTargetCoordinate == null && moveTargetObject == null)
            {
                rigidBody2D.bodyType = RigidbodyType2D.Static;
                return true;
            }
            
            rigidBody2D.bodyType = RigidbodyType2D.Dynamic;
            return false;
        }
        
        private void SetAnimationAndSpeedForMovementEnd()
        {
            SetLookDirection(defaultLookDirection);
            currentSpeed = 0f;
            UpdateAnimatorParameters();
        }

        private IEnumerator DelayedMove(Vector2 newPosition, float delayTime)
        {
            isQueuedCoroutineActive = true;
            yield return new WaitForSeconds(delayTime);
            movementConfiguration.ExecuteMove(this, newPosition);
            queuedMoveCoroutine = null;
            isQueuedCoroutineActive = false;
        }
        #endregion

        #region Interfaces
        // Save State
        public LoadPriority GetLoadPriority() => LoadPriority.ObjectProperty;

        public SaveState CaptureState() => ManualGetStateFromData(new SerializableVector2(transform.position));
        
        public void RestoreState(SaveState saveState)
        {
            SerializableVector2 savedPosition = ManualGetDataFromState(saveState);
            if (savedPosition == null) { return; }

            // Force initialization for objects set to disable
            if (rigidBody2D == null)
            {
                rigidBody2D = GetComponent<Rigidbody2D>();
                SetupInitialState();
            }
            
            transform.position = savedPosition.ToVector();
            SetLookDirection(Vector2.down);
        }

        public SaveState ManualGetStateFromData(SerializableVector2 data) => new(GetLoadPriority(), data);
        public SerializableVector2 ManualGetDataFromState(SaveState saveState) => saveState?.GetState(typeof(SerializableVector2)) as SerializableVector2;
        #endregion
    }
}

using UnityEngine;
using Frankie.Saving;
using Frankie.Utils;

namespace Frankie.Control
{
    [RequireComponent(typeof(Rigidbody2D))]
    public abstract class Mover : MonoBehaviour, ISaveable
    {
        // Tunables
        [SerializeField] protected float movementSpeed = 1.0f;
        [SerializeField] protected Vector2 defaultLookDirection = Vector2.down;
        [SerializeField] protected float defaultTargetDistanceTolerance = 0.15f;
        [SerializeField] private float closeTargetThresholdSquared = 0.5625f;
        [SerializeField] private bool resetPositionOnEnable = false;
        [SerializeField][Tooltip("Sets the target position delay for chase")] private int targetMovementHistoryLength = 10;

        // State
        protected Vector2 originalPosition;
        private Vector2? moveTargetCoordinate;
        private GameObject moveTargetObject;
        private CircularBuffer<Vector2> targetMovementHistory;
        protected Vector2 lookDirection = Vector2.down;
        protected float currentSpeed;
        private float targetDistanceTolerance = 0.025f;

        // Cached References
        protected Rigidbody2D rigidBody2D;

        #region Static
        private const float _signFloorThreshold = 0.1f;
        protected const float pixelsPerUnit = 100.0f; // Align to pixel art setting, default: 100
        protected static float SignFloored(float number) => Mathf.Abs(number) < _signFloorThreshold ? 0 : Mathf.Sign(number);
        
        private static readonly int _speed = Animator.StringToHash("Speed");
        private static readonly int _xLook = Animator.StringToHash("xLook");
        private static readonly int _yLook = Animator.StringToHash("yLook");
        public static void SetAnimatorSpeed(Animator animator, float speed) => animator.SetFloat(_speed, speed);
        public static void SetAnimatorXLook(Animator animator, float xLookDirection) => animator.SetFloat(_xLook, xLookDirection);
        public static void SetAnimatorYLook(Animator animator, float yLookDirection) => animator.SetFloat(_yLook, yLookDirection);
        #endregion

        #region UnityMethods
        protected virtual void Awake()
        {
            rigidBody2D = GetComponent<Rigidbody2D>();
            SetupInitialState();
        }

        private void SetupInitialState()
        {
            originalPosition = transform.position;
            targetDistanceTolerance = defaultTargetDistanceTolerance;
            targetMovementHistory = new CircularBuffer<Vector2>(targetMovementHistoryLength);
        }

        protected virtual void Start()
        {
            // N.B. Deliberately NOT calling clear move targets here to avoid order of operations issues
            // In some edge cases Start() can be called after Update(), which can cause shouts to fail

            SetLookDirection(defaultLookDirection); // Initialize look direction to avoid wonky
        }

        protected virtual void FixedUpdate()
        {
            MoveToTarget();
        }

        protected virtual void OnEnable()
        {
            if (!resetPositionOnEnable) return;
            transform.position = originalPosition;
            SetLookDirection(defaultLookDirection);
        }
        #endregion

        #region PublicMethods
        public Vector2 GetLookDirection() => lookDirection;
        
        public void SetLookDirection(Vector2 setLookDirection)
        {
            // Blend tree animation speed depends on magnitude of variable -- to avoid very quick animations, normalize
            setLookDirection.Normalize(); 
            lookDirection = setLookDirection;
            UpdateAnimator();
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
        #endregion

        #region AbstractProtectedMethods
        protected abstract void UpdateAnimator();
        protected bool? MoveToTarget()
        {
            if (SetStaticForNoTarget()) { return null; }

            Vector2 position = rigidBody2D.position;
            Vector2 target = ReckonTarget(false, true);
            
            if (HasArrivedAtTarget(target, out float squareMagnitudeDelta)) { return false; }
            target = ReckonTarget(squareMagnitudeDelta > closeTargetThresholdSquared, false);

            Vector2 direction = target - position;
            lookDirection.Set(direction.x, direction.y);
            lookDirection.Normalize();
            currentSpeed = movementSpeed;

            position.x = Mathf.Round(pixelsPerUnit * (position.x + currentSpeed * SignFloored(lookDirection.x) * Time.deltaTime)) / pixelsPerUnit;
            position.y = Mathf.Round(pixelsPerUnit * (position.y + currentSpeed * SignFloored(lookDirection.y) * Time.deltaTime)) / pixelsPerUnit;
            rigidBody2D.MovePosition(position);
            UpdateAnimator();

            return true;
        }

        protected virtual Vector2 ReckonTarget(bool withOffsetting = true, bool addToHistory = true)
        {
            if (moveTargetCoordinate != null) { return moveTargetCoordinate.Value; }
            if (moveTargetObject == null) { return Vector2.zero; }
            
            Vector2 currentTargetPosition = moveTargetObject.transform.position;
            if (addToHistory) { targetMovementHistory.Add(currentTargetPosition); }
            if (targetMovementHistory.GetCurrentSize() == 0) { return currentTargetPosition; }
            
            return withOffsetting ? targetMovementHistory.GetLastEntry() : targetMovementHistory.GetFirstEntry();
        }
        #endregion

        #region  PrivateMethods
        private bool HasMoveTarget() => (moveTargetCoordinate != null || moveTargetObject != null);
        private bool HasArrivedAtTarget(Vector2 target, out float squareMagnitudeDelta) => SmartVector2.CheckDistance(rigidBody2D.position, target, targetDistanceTolerance, out squareMagnitudeDelta);
        
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
            UpdateAnimator();
        }
        #endregion

        #region Interfaces
        // Save State
        [System.Serializable]
        private class MoverSaveData
        {
            public SerializableVector2 position;
        }

        public LoadPriority GetLoadPriority() => LoadPriority.ObjectProperty;

        SaveState ISaveable.CaptureState()
        {
            var data = new MoverSaveData { position = new SerializableVector2(transform.position) };
            return new SaveState(GetLoadPriority(), data);
        }

        void ISaveable.RestoreState(SaveState saveState)
        {
            if (saveState.GetState(typeof(MoverSaveData)) is not MoverSaveData moverSaveData) { return; }

            // Force initialization for objects set to disable
            if (rigidBody2D == null)
            {
                rigidBody2D = GetComponent<Rigidbody2D>();
                SetupInitialState();
            }
            
            transform.position = moverSaveData.position.ToVector();
            SetLookDirection(Vector2.down);
        }
        #endregion
    }
}

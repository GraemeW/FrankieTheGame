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
        [SerializeField] private bool resetPositionOnEnable = false;
        [SerializeField][Tooltip("Sets the target position delay for chase")] private int targetMovementHistoryLength = 10;

        // State
        private Vector2 originalPosition;
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
        public bool HasMoveTarget() => (moveTargetCoordinate != null || moveTargetObject != null);
        
        public void SetLookDirection(Vector2 setLookDirection)
        {
            setLookDirection.Normalize(); // Blend tree animation speed depends on magnitude of variable -- to avoid very quick animations, normalize 
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
            SetAnimationAndSpeedForMovementEnd();
            targetDistanceTolerance = defaultTargetDistanceTolerance;
            moveTargetCoordinate = null;
            targetMovementHistory.Clear();
            moveTargetObject = null;
        }
        
        public void WarpToPosition(Vector2 target)
        {
            SetMoveTarget(target);
            transform.position = target;
        }

        public void MoveToOriginalPosition()
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
            Vector2 target = ReckonTarget();
            if (HasArrivedAtTarget(target)) { return false; }

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

        protected virtual Vector2 ReckonTarget()
        {
            Vector2 target = Vector2.zero;
            if (moveTargetCoordinate != null)
            {
                target = moveTargetCoordinate.Value;
            }
            else if (moveTargetObject != null)
            {
                targetMovementHistory.Add(moveTargetObject.transform.position);
                target = targetMovementHistory.GetLastEntry();
            }

            return target;
        }

        protected void SetLookDirection(PlayerInputType playerInputType)
        {
            Vector2 newLookDirection;
            switch (playerInputType)
            {
                case PlayerInputType.NavigateDown:
                    newLookDirection = Vector2.down;
                    break;
                case PlayerInputType.NavigateUp:
                    newLookDirection = Vector2.up;
                    break;
                case PlayerInputType.NavigateLeft:
                    newLookDirection = Vector2.left;
                    break;
                case PlayerInputType.NavigateRight:
                    newLookDirection = Vector2.right;
                    break;
                default:
                    newLookDirection = Vector2.zero;
                    break;
            }
            SetLookDirection(newLookDirection);
        }
        #endregion

        #region  PrivateMethods

        private bool HasArrivedAtTarget(Vector2 target)
        {
            if (moveTargetObject == null) { return SmartVector2.CheckDistance(rigidBody2D.position, target, targetDistanceTolerance); }
            
            // Since ReckonTarget can result in an offset position, to finally 'reach' the target, check against target directly
            return SmartVector2.CheckDistance(rigidBody2D.position, moveTargetObject.transform.position, targetDistanceTolerance);
        }
        
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
            SetLookDirection(Vector2.down);
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

        public LoadPriority GetLoadPriority()
        {
            return LoadPriority.ObjectProperty;
        }

        SaveState ISaveable.CaptureState()
        {
            var data = new MoverSaveData
            {
                position = new SerializableVector2(transform.position)
            };
            var saveState = new SaveState(GetLoadPriority(), data);
            return saveState;
        }

        void ISaveable.RestoreState(SaveState saveState)
        {
            if (saveState.GetState(typeof(MoverSaveData)) is not MoverSaveData moverSaveData) { return; }

            if (rigidBody2D == null) { Awake(); } // Force initialization for objects set to disable
            
            transform.position = moverSaveData.position.ToVector();
            SetLookDirection(Vector2.down);
        }
        #endregion
    }
}

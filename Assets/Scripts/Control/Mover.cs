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

        // State
        private Vector2 originalPosition;
        private Vector2? moveTargetCoordinate;
        private GameObject moveTargetObject;
        protected Vector2 lookDirection = Vector2.down;
        protected float currentSpeed;
        float targetDistanceTolerance = 0.15f;

        // Cached References
        protected Rigidbody2D rigidBody2D = null;

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
            originalPosition = transform.position;
            targetDistanceTolerance = defaultTargetDistanceTolerance;
            rigidBody2D = GetComponent<Rigidbody2D>();
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
            moveTargetObject = target;
            targetDistanceTolerance = defaultTargetDistanceTolerance;
        }

        public void ClearMoveTargets()
        {
            SetAnimationAndSpeedForMovementEnd();
            targetDistanceTolerance = defaultTargetDistanceTolerance;
            moveTargetCoordinate = null;
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
            if (IsStaticForNoTarget()) { return null; }

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
                target = moveTargetObject.transform.position;
            }

            return target;
        }
        #endregion

        #region  PrivateMethods
        private bool IsStaticForNoTarget()
        {
            if (moveTargetCoordinate == null && moveTargetObject == null)
            {
                rigidBody2D.bodyType = RigidbodyType2D.Static;
                return true;
            }
            
            rigidBody2D.bodyType = RigidbodyType2D.Dynamic;
            return false;
        }
        
        private bool HasArrivedAtTarget(Vector2 target) => SmartVector2.CheckDistance(rigidBody2D.position, target, targetDistanceTolerance);
        
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
        class MoverSaveData
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
            SaveState saveState = new SaveState(GetLoadPriority(), data);
            return saveState;
        }

        void ISaveable.RestoreState(SaveState saveState)
        {
            var moverSaveData = saveState.GetState(typeof(MoverSaveData)) as MoverSaveData;
            if (moverSaveData == null) { return; }

            if (rigidBody2D == null) { Awake(); } // Force initialization for objects set to disable
            transform.position = moverSaveData.position.ToVector();
            SetLookDirection(Vector2.down);
        }
        #endregion
    }
}

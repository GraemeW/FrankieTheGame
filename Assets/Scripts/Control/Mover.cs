using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frankie.Saving;
using Frankie.Utils;

namespace Frankie.Control
{
    public class Mover : MonoBehaviour, ISaveable
    {
        // Tunables
        [SerializeField] protected float movementSpeed = 1.0f;
        [SerializeField] protected float defaultTargetDistanceTolerance = 0.15f;
        [SerializeField] bool resetPositionOnEnable = false;

        // State
        protected Vector2 originalPosition = new Vector2();
        protected Vector2? moveTargetCoordinate = null;
        protected GameObject moveTargetObject = null;
        protected Vector2 lookDirection = new Vector2();
        protected float currentSpeed = 0;
        float targetDistanceTolerance = 0.15f;

        // Cached References
        protected Rigidbody2D rigidBody2D = null;

        // Static
        static float SIGN_FLOOR_THRESHOLD = 0.1f;

        protected static float Sign(float number)
        {
            return number < 0 ? -1 : (number > 0 ? 1 : 0);
        }

        protected static float SignFloored(float number)
        {
            if (Mathf.Abs(number) < SIGN_FLOOR_THRESHOLD) { return 0; }
            else { return Sign(number); }
        }

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

            SetLookDirection(Vector2.down); // Initialize look direction to avoid wonky
        }

        protected virtual void FixedUpdate()
        {
            MoveToTarget();
        }

        private void OnEnable()
        {
            if (resetPositionOnEnable)
            {
                transform.position = originalPosition;
                SetLookDirection(Vector2.down);
            }
        }

        public void SetLookDirection(Vector2 lookDirection)
        {
            this.lookDirection = lookDirection;
            UpdateAnimator();
        }

        public Vector2 GetLookDirection()
        {
            return lookDirection;
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
            targetDistanceTolerance = 0f;
        }

        public void ClearMoveTargets()
        {
            SetAnimationAndSpeedForMovementEnd();
            targetDistanceTolerance = defaultTargetDistanceTolerance;
            moveTargetCoordinate = null;
            moveTargetObject = null;
        }

        public bool HasMoveTarget()
        {
            return (moveTargetCoordinate != null || moveTargetObject != null);
        }

        public void MoveToOriginalPosition()
        {
            targetDistanceTolerance = defaultTargetDistanceTolerance;
            SetMoveTarget(originalPosition);
        }

        protected bool? MoveToTarget()
        {
            if (IsStaticForNoTarget()) { return null; }

            Vector2 position = rigidBody2D.position;
            Vector2 target = ReckonTarget();
            if (ArrivedAtTarget(target)) { return false; }

            Vector2 direction = target - position;
            lookDirection.Set(direction.x, direction.y);
            lookDirection.Normalize();
            currentSpeed = movementSpeed;

            position.x = position.x + currentSpeed * SignFloored(lookDirection.x) * Time.deltaTime;
            position.y = position.y + currentSpeed * SignFloored(lookDirection.y) * Time.deltaTime;
            rigidBody2D.MovePosition(position);
            UpdateAnimator();

            return true;
        }

        private Vector2 ReckonTarget()
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

        private bool IsStaticForNoTarget()
        {
            if (moveTargetCoordinate == null && moveTargetObject == null)
            {
                rigidBody2D.bodyType = RigidbodyType2D.Static;
                return true;
            }
            else
            {
                rigidBody2D.bodyType = RigidbodyType2D.Dynamic;
                return false;
            }
        }

        private bool ArrivedAtTarget(Vector2 target)
        {
            if (SmartVector2.CheckDistance(rigidBody2D.position, target, targetDistanceTolerance))
            {
                return true;
            }
            return false;
        }

        protected void SetAnimationAndSpeedForMovementEnd()
        {
            SetLookDirection(Vector2.down);
            currentSpeed = 0f;
            UpdateAnimator();
        }

        protected virtual void UpdateAnimator()
        {
            // Base implementation blank
        }

        #region Interfaces
        // Save State
        [System.Serializable]
        struct MoverSaveData
        {
            public SerializableVector2 position;
        }

        public LoadPriority GetLoadPriority()
        {
            return LoadPriority.ObjectProperty;
        }

        SaveState ISaveable.CaptureState()
        {
            MoverSaveData data = new MoverSaveData
            {
                position = new SerializableVector2(transform.position)
            };
            SaveState saveState = new SaveState(GetLoadPriority(), data);
            return saveState;
        }

        void ISaveable.RestoreState(SaveState saveState)
        {
            MoverSaveData data = (MoverSaveData)saveState.GetState();
            transform.position = data.position.ToVector();
        }
        #endregion
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frankie.Saving;

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
            ClearMoveTargets();
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
                lookDirection = Vector2.down;
            }
        }

        public void SetLookDirection(Vector2 lookDirection)
        {
            this.lookDirection = lookDirection;
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

        public void SetMoveTarget(GameObject gameObject)
        {
            moveTargetCoordinate = null;
            moveTargetObject = gameObject;
            targetDistanceTolerance = 0f;
        }

        public void ClearMoveTargets()
        {
            SetAnimationAndSpeedForMovementEnd();
            targetDistanceTolerance = defaultTargetDistanceTolerance;
            moveTargetCoordinate = null;
            moveTargetObject = null;
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
            if (Mathf.Abs(Vector2.Distance(rigidBody2D.position, target)) < targetDistanceTolerance)
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

        // Save State
        [System.Serializable]
        struct MoverSaveData
        {
            public SerializableVector2 position;
        }

        public object CaptureState()
        {
            MoverSaveData data = new MoverSaveData
            {
                position = new SerializableVector2(transform.position)
            };
            return data;
        }

        public void RestoreState(object state)
        {
            MoverSaveData data = (MoverSaveData)state;
            transform.position = data.position.ToVector();
        }
    }
}
using UnityEngine;
using LowDefMustard.Control;

namespace Frankie.Utils.UI
{
    [RequireComponent(typeof(Animator))]
    public class UICharacter : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 100f;
        [SerializeField] private float distanceSquaredThreshold = 0.1f;
        
        // State
        private Vector2 initialPosition;
        private Vector2 targetPosition;
        private float totalMoveDistance = 1f;
        private float moveFraction = 0f;
        private bool hasArrived = true;
        
        // Cached References
        private Animator animator;

        #region UnityMethods
        private void Awake()
        {
            animator = GetComponent<Animator>();
            initialPosition = transform.position;
        }

        private void Update()
        {
            if (hasArrived) { return; }
            
            moveFraction += Time.deltaTime * moveSpeed / totalMoveDistance;
            transform.position = Vector2.Lerp(initialPosition, targetPosition, moveFraction);
            hasArrived = Mathf.Pow(targetPosition.x - transform.position.x, 2) + Mathf.Pow(targetPosition.y - transform.position.y,2) < distanceSquaredThreshold;

            if (!hasArrived) { return; }
            LookAt(Vector2.down);
            Stop();
        }
        #endregion

        public void MoveTowards(Vector2 position)
        {
            totalMoveDistance = Vector2.Distance(transform.position, position);
            if (totalMoveDistance < distanceSquaredThreshold) { return; }
            
            Vector2 lookDirection = (position - (Vector2)transform.position).normalized;
            Mover.SetAnimatorSpeed(animator, moveSpeed);
            Mover.SetAnimatorXLook(animator, lookDirection.x);
            Mover.SetAnimatorYLook(animator, lookDirection.y);
            
            hasArrived = false;
            moveFraction = 0f;
            initialPosition = transform.position;
            targetPosition = position;
        }

        public void LookAt(Vector2 direction)
        {
            Mover.SetAnimatorXLook(animator, direction.x);
            Mover.SetAnimatorYLook(animator, direction.y);
        }

        public void Stop()
        {
            Mover.SetAnimatorSpeed(animator, 0f);
            initialPosition = transform.position;
            targetPosition = transform.position;
            hasArrived = true;
        }
    }
}
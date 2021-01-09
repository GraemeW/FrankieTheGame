using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Control
{
    public class PlayerController : MonoBehaviour
    {
        // Tunables
        [SerializeField] float movementSpeed = 1.0f;
        [SerializeField] float speedMoveThreshold = 0.05f;

        // State
        float inputHorizontal;
        float inputVertical;
        Vector2 lookDirection = new Vector2();
        float currentSpeed;

        // Cached References
        Rigidbody2D playerRigidbody2D = null;
        Animator animator = null;

        // Internal functions
        static float Sign(float number)
        {
            return number < 0 ? -1 : (number > 0 ? 1 : 0);
        }


        private void Start()
        {
            animator = GetComponent<Animator>();
            playerRigidbody2D = GetComponent<Rigidbody2D>();
            InitializeLookDirection();
        }

        private void Update()
        {
            inputHorizontal = Input.GetAxis("Horizontal");
            inputVertical = Input.GetAxis("Vertical");
        }

        private void FixedUpdate()
        {
            InteractWithMovement();
        }

        private void InitializeLookDirection()
        {
            lookDirection = Vector2.down;
        }

        private void InteractWithMovement()
        {
            SetMovementParameters();
            UpdateAnimator();
            if (currentSpeed > speedMoveThreshold)
            {
                MovePlayer();
            }

        }

        private void SetMovementParameters()
        {
            Vector2 move = new Vector2(inputHorizontal, inputVertical);
            if (!Mathf.Approximately(move.x, 0.0f) || !Mathf.Approximately(move.y, 0.0f))
            {
                lookDirection.Set(move.x, move.y);
                lookDirection.Normalize();
            }
            currentSpeed = move.magnitude;
        }

        private void MovePlayer()
        {
            Vector2 position = playerRigidbody2D.position;
            position.x = position.x + movementSpeed * Sign(inputHorizontal) * Time.deltaTime;
            position.y = position.y + movementSpeed * Sign(inputVertical) * Time.deltaTime;
            playerRigidbody2D.MovePosition(position);
        }

        private void UpdateAnimator()
        {
            animator.SetFloat("Speed", currentSpeed);
            animator.SetFloat("xLook", lookDirection.x);
            animator.SetFloat("yLook", lookDirection.y);
        }
    }
}

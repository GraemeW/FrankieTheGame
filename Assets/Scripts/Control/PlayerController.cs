using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Control
{
    public class PlayerController : MonoBehaviour
    {
        // Tunables
        [SerializeField] float movementSpeed = 3.0f;
        [SerializeField] float currentSpeed;

        // State
        float inputHorizontal;
        float inputVertical;
        Vector2 lookDirection = new Vector2();

        // Cached References
        Rigidbody2D playerRigidbody2D = null;
        Animator animator = null;

        private void Start()
        {
            animator = GetComponent<Animator>();
            playerRigidbody2D = GetComponent<Rigidbody2D>();
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

        private void InteractWithMovement()
        {
            Vector2 move = new Vector2(inputHorizontal, inputVertical);

            if (!Mathf.Approximately(move.x, 0.0f) || !Mathf.Approximately(move.y, 0.0f))
            {
                lookDirection.Set(move.x, move.y);
                lookDirection.Normalize();
            }
            animator.SetFloat("Speed", move.magnitude);
            currentSpeed = move.magnitude;

            /* TODO:  Make assets for multi-dimensional looking ++ animation
            animator.SetFloat("Look X", lookDirection.x);
            animator.SetFloat("Look Y", lookDirection.y);
            */

            Vector2 position = playerRigidbody2D.position;
            position.x = position.x + movementSpeed * inputHorizontal * Time.deltaTime;
            position.y = position.y + movementSpeed * inputVertical * Time.deltaTime;

            playerRigidbody2D.MovePosition(position);
        }
    }
}

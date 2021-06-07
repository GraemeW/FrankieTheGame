using Frankie.Stats;
using UnityEngine;
using Frankie.Saving;

namespace Frankie.Control
{
    [RequireComponent(typeof(PlayerController))]
    public class PlayerMover : Mover
    {
        // Tunables
        [SerializeField] float speedMoveThreshold = 0.05f;

        // State
        float inputHorizontal;
        float inputVertical;

        // Cached References
        PlayerStateHandler playerStateHandler = null;
        Party party = null;

        protected override void Awake()
        {
            base.Awake();
            playerStateHandler = GetComponent<PlayerStateHandler>();
            party = GetComponent<Party>();
        }

        protected override void FixedUpdate()
        {
            // TODO:  Add cutscene support (override user input)

            if (playerStateHandler.GetPlayerState() == PlayerState.inWorld)
            {
                InteractWithMovement();
            }
        }
        public void ParseMovement(Vector2 directionalInput)
        {
            inputHorizontal = Vector2.Dot(directionalInput, Vector2.right);
            inputVertical = Vector2.Dot(directionalInput, Vector2.up);
        }

        private void InteractWithMovement()
        {
            SetMovementParameters();
            party.UpdatePartyAnimation(currentSpeed, lookDirection.x, lookDirection.y);
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
            Vector2 position = rigidBody2D.position;

            position.x = position.x + movementSpeed * Sign(inputHorizontal) * Time.deltaTime;
            position.y = position.y + movementSpeed * Sign(inputVertical) * Time.deltaTime;
            rigidBody2D.MovePosition(position);
        }

        protected override void UpdateAnimator()
        {
            playerStateHandler.GetParty().UpdatePartyAnimation(movementSpeed, lookDirection.x, lookDirection.y);
        }
    }
}

using Frankie.Stats;
using UnityEngine;
using Frankie.Saving;
using Frankie.Utils;

namespace Frankie.Control
{
    [RequireComponent(typeof(PlayerController))]
    public class PlayerMover : Mover
    {
        // Tunables
        [SerializeField] float speedMoveThreshold = 0.05f;
        [SerializeField] int playerMovementHistoryLength = 256;

        // State
        float inputHorizontal;
        float inputVertical;
        CircularBuffer<Vector2> movementHistory;

        // Cached References
        PlayerStateHandler playerStateHandler = null;
        Party party = null;

        protected override void Awake()
        {
            base.Awake();
            playerStateHandler = GetComponent<PlayerStateHandler>();
            party = GetComponent<Party>();
            movementHistory = new CircularBuffer<Vector2>(playerMovementHistoryLength);
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

        public void ResetHistory(Vector2 newPosition)
        {
            movementHistory.Clear();
            movementHistory.Add(newPosition);
            party.ResetPartyOffsets();
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
            movementHistory.Add(position);

            position.x = position.x + movementSpeed * Sign(inputHorizontal) * Time.deltaTime;
            position.y = position.y + movementSpeed * Sign(inputVertical) * Time.deltaTime;
            rigidBody2D.MovePosition(position);
            party.UpdatePartyOffsets(movementHistory);
        }

        protected override void UpdateAnimator()
        {
            party.UpdatePartyAnimation(movementSpeed, lookDirection.x, lookDirection.y);
        }
    }
}

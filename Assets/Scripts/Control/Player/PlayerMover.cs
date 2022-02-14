using Frankie.Stats;
using UnityEngine;
using Frankie.Saving;
using Frankie.Utils;
using System;

namespace Frankie.Control
{
    [RequireComponent(typeof(PlayerController))]
    [RequireComponent(typeof(PlayerStateHandler))]
    public class PlayerMover : Mover
    {
        // Tunables
        [SerializeField] float speedMoveThreshold = 0.05f;
        [SerializeField] int playerMovementHistoryLength = 128;

        // State
        bool inWorld = true;
        float inputHorizontal;
        float inputVertical;
        CircularBuffer<Tuple<Vector2, Vector2>> movementHistory;
        bool historyResetThisFrame = false;

        // Cached References
        PlayerStateHandler playerStateHandler = null;

        // Events
        public event Action movementHistoryReset;
        public event Action<float, float, float> leaderAnimatorUpdated;
        public event Action<CircularBuffer<Tuple<Vector2, Vector2>>> playerMoved;

        protected override void Awake()
        {
            base.Awake();
            playerStateHandler = GetComponent<PlayerStateHandler>();
            movementHistory = new CircularBuffer<Tuple<Vector2, Vector2>>(playerMovementHistoryLength);
        }

        private void OnEnable()
        {
            playerStateHandler.playerStateChanged += ParsePlayerStateChange;
        }

        private void OnDisable()
        {
            playerStateHandler.playerStateChanged -= ParsePlayerStateChange;
        }

        private void ParsePlayerStateChange(PlayerStateType playerStateType)
        {
            inWorld = (playerStateType == PlayerStateType.inWorld);
        }

        protected override void FixedUpdate()
        {
            // TODO:  Add cutscene support (override user input)
            if (inWorld) { InteractWithMovement(); }
        }

        public void ParseMovement(Vector2 directionalInput)
        {
            inputHorizontal = Vector2.Dot(directionalInput, Vector2.right);
            inputVertical = Vector2.Dot(directionalInput, Vector2.up);
        }

        public void ResetHistory(Vector2 newPosition)
        {
            movementHistory.Clear();
            movementHistory.Add(new Tuple<Vector2, Vector2>(newPosition, new Vector2(lookDirection.x, lookDirection.y)));
            movementHistoryReset?.Invoke();

            historyResetThisFrame = true;
        }

        private void InteractWithMovement()
        {
            if (historyResetThisFrame) { historyResetThisFrame = false; return; }

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
            Vector2 position = rigidBody2D.position;
            position.x = position.x + movementSpeed * Sign(inputHorizontal) * Time.deltaTime;
            position.y = position.y + movementSpeed * Sign(inputVertical) * Time.deltaTime;
            rigidBody2D.MovePosition(position);

            movementHistory.Add(new Tuple<Vector2, Vector2>(position, new Vector2(lookDirection.x, lookDirection.y)));
            playerMoved?.Invoke(movementHistory);
        }

        protected override void UpdateAnimator()
        {
            leaderAnimatorUpdated?.Invoke(currentSpeed, lookDirection.x, lookDirection.y);
        }
    }
}

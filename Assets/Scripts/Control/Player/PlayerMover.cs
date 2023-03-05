using Frankie.Stats;
using UnityEngine;
using Frankie.Saving;
using Frankie.Utils;
using System;

namespace Frankie.Control
{
    [RequireComponent(typeof(PlayerController))]
    [RequireComponent(typeof(PlayerStateMachine))]
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
        PlayerStateMachine playerStateHandler = null;

        // Events
        public event Action movementHistoryReset;
        public event Action<float, float, float> leaderAnimatorUpdated;
        public event Action<CircularBuffer<Tuple<Vector2, Vector2>>> playerMoved;

        protected override void Awake()
        {
            base.Awake();
            playerStateHandler = GetComponent<PlayerStateMachine>();
            movementHistory = new CircularBuffer<Tuple<Vector2, Vector2>>(playerMovementHistoryLength);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            playerStateHandler.playerStateChanged += ParsePlayerStateChange;
        }

        private void OnDisable()
        {
            playerStateHandler.playerStateChanged -= ParsePlayerStateChange;
        }

        private void ParsePlayerStateChange(PlayerStateType playerStateType)
        {
            inWorld = (playerStateType == PlayerStateType.inWorld);
            GetPlayerMovementSpeed(); // Called in parse player state change to avoid having to fetch modifiers on every move update call
        }

        protected override void FixedUpdate()
        {
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

        private float GetPlayerMovementSpeed()
        {
            if (playerStateHandler == null) { return movementSpeed; }

            float modifier = playerStateHandler.GetParty().GetPartyLeader().GetCalculatedStat(CalculatedStat.MoveSpeed);
            return movementSpeed * modifier;
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
            position.x = position.x + GetPlayerMovementSpeed() * Sign(inputHorizontal) * Time.deltaTime;
            position.y = position.y + GetPlayerMovementSpeed() * Sign(inputVertical) * Time.deltaTime;
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

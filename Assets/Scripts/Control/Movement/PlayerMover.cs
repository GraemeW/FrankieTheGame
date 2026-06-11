using System;
using UnityEngine;
using Frankie.Utils;
using Frankie.Stats;

namespace Frankie.Control
{
    [RequireComponent(typeof(PlayerController))]
    [RequireComponent(typeof(PlayerStateMachine))]
    public class PlayerMover : Mover
    {
        // Tunables
        [SerializeField] private float speedMoveThreshold = 0.05f;
        [SerializeField] private int playerMovementHistoryLength = 128;

        // State
        private bool inWorld = true;
        private float inputHorizontal;
        private float inputVertical;
        private CircularBuffer<Tuple<Vector2, Vector2>> movementHistory;
        private bool historyResetThisFrame = false;

        // Cached References
        private PlayerStateMachine playerStateMachine;
        private Party party;

        // Events
        public event Action movementHistoryReset;
        public event Action<float, float, float> leaderAnimatorUpdated;
        public event Action<CircularBuffer<Tuple<Vector2, Vector2>>> playerMoved;

        protected override void Awake()
        {
            base.Awake();
            playerStateMachine = GetComponent<PlayerStateMachine>();
            party = GetComponent<Party>();
            movementHistory = new CircularBuffer<Tuple<Vector2, Vector2>>(playerMovementHistoryLength);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            playerStateMachine.playerStateChanged += ParsePlayerStateChange;
        }

        private void OnDisable()
        {
            playerStateMachine.playerStateChanged -= ParsePlayerStateChange;
        }

        private void ParsePlayerStateChange(PlayerStateType playerStateType, IPlayerStateContext playerStateContext)
        {
            inWorld = (playerStateType == PlayerStateType.InWorld);
            if (playerStateType == PlayerStateType.InCutScene) { inWorld = playerStateContext.CanMoveInCutscene(); }
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
            float modifier = party.GetPartyLeader().GetCalculatedStat(CalculatedStat.MoveSpeed);
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
            var move = new Vector2(inputHorizontal, inputVertical);
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

            position.x = Mathf.Round(pixelsPerUnit * (position.x + GetPlayerMovementSpeed()  * SignFloored(lookDirection.x) * Time.deltaTime)) / pixelsPerUnit;
            position.y = Mathf.Round(pixelsPerUnit * (position.y + GetPlayerMovementSpeed()  * SignFloored(lookDirection.y) * Time.deltaTime)) / pixelsPerUnit;
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

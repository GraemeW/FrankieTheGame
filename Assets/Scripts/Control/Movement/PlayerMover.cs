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
        [SerializeField] private float speedPollingTime = 0.5f;
        [SerializeField] private float speedMoveThreshold = 0.05f;
        [SerializeField] private int playerMovementHistoryLength = 128;

        // State
        private bool inWorld = true;
        private float cachedSpeed = 1.0f;
        private float timeSinceSpeedPolled = Mathf.Infinity;
        
        private float inputHorizontal;
        private float inputVertical;
        private CircularBuffer<Tuple<Vector2, Vector2>> movementHistory;
        private bool historyResetThisFrame = false;

        // Cached References
        private PlayerStateMachine playerStateMachine;
        private Party party;

        // Events
        public event Action movementHistoryReset;
        public event Action<MovementAnimationParameters> leadAnimationParametersUpdated;
        public event Action<CircularBuffer<Tuple<Vector2, Vector2>>> playerMoved;

        #region UnityMethods
        protected override void Awake()
        {
            playerStateMachine = GetComponent<PlayerStateMachine>();
            party = GetComponent<Party>();
            movementHistory = new CircularBuffer<Tuple<Vector2, Vector2>>(playerMovementHistoryLength);
            base.Awake();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            playerStateMachine.playerStateChanged += ParsePlayerStateChange;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            playerStateMachine.playerStateChanged -= ParsePlayerStateChange;
        }
        
        protected override void FixedUpdate()
        {
            base.FixedUpdate();
            if (inWorld) { InteractWithMovement(); }
            timeSinceSpeedPolled += Time.deltaTime;
        }
        #endregion

        #region EventListeners
        private void ParsePlayerStateChange(PlayerStateType playerStateType, IPlayerStateContext playerStateContext)
        {
            inWorld = (playerStateType == PlayerStateType.InWorld);
            if (playerStateType == PlayerStateType.InCutScene) { inWorld = playerStateContext.CanMoveInCutscene(); }
            GetCurrentSpeed(); // Called in parse player state change to avoid having to fetch modifiers on every move update call
        }
        #endregion

        #region PublicMethods
        public void ParseMovement(Vector2 directionalInput)
        {
            inputHorizontal = Vector2.Dot(directionalInput, Vector2.right);
            inputVertical = Vector2.Dot(directionalInput, Vector2.up);
        }

        public void ResetHistory(Vector2 newPosition)
        {
            movementHistory.Clear();
            AddToMovementHistory(newPosition);
            movementHistoryReset?.Invoke();

            historyResetThisFrame = true;
        }

        public override float GetCurrentSpeed()
        {
            if (timeSinceSpeedPolled < speedPollingTime) { return cachedSpeed; }
            
            float modifier = party.GetPartyLeader().GetCalculatedStat(CalculatedStat.MoveSpeed);
            cachedSpeed = movementConfiguration.baseMovementSpeed * modifier;
            timeSinceSpeedPolled = 0f;

            return cachedSpeed;
        }
        #endregion
        
        #region ProtectedPrivateMethods
        protected override void UpdateAnimatorParameters(bool useCardinalLookDelay = false)
        {
            leadAnimationParametersUpdated?.Invoke(new MovementAnimationParameters(currentSpeed, lookDirection.x, lookDirection.y, GetSpritePositionOffset()));
        }
        
        private void InteractWithMovement()
        {
            if (historyResetThisFrame) { historyResetThisFrame = false; return; }
            
            SetMovementParameters();
            UpdateAnimatorParameters();
            if (currentSpeed > speedMoveThreshold)
            {
                MovePlayer();
            }
        }

        private void SetMovementParameters()
        {
            var move = new Vector2(inputHorizontal, inputVertical);
            currentSpeed = move.magnitude;
            if (!Mathf.Approximately(move.x, 0.0f) || !Mathf.Approximately(move.y, 0.0f))
            {
                SetLookDirection(move, false);
            }
        }

        private void MovePlayer()
        {
            currentSpeed = GetCurrentSpeed();
            if (!movementConfiguration.MoveToTarget(this, Time.deltaTime, out Vector2 newPosition)) { return; }
            AddToMovementHistory(newPosition);
            playerMoved?.Invoke(movementHistory);
        }

        private void AddToMovementHistory(Vector2 newPosition)
        {
            movementHistory.Add(new Tuple<Vector2, Vector2>(RoundToPixelPerfect(newPosition), new Vector2(lookDirection.x, lookDirection.y)));
        }
        #endregion
    }
}

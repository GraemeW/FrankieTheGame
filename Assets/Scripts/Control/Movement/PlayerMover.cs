using System;
using UnityEngine;
using LowDefMustard.Control;
using LowDefMustard.Saving;
using LowDefMustard.Utils;
using Frankie.Core;
using Frankie.Stats;

namespace Frankie.Control
{
    [RequireComponent(typeof(PlayerController))]
    [RequireComponent(typeof(PlayerStateMachine))]
    [RequireComponent(typeof(Party))]
    public class PlayerMover : Mover, ISaveable<SerializableVector2>
    {
        // Tunables
        [SerializeField] private bool snapPlayerToPixelPerfect = false;
        [SerializeField] private float speedMoveThreshold = 0.05f;
        [SerializeField] private int playerMovementHistoryLength = 256;
        [SerializeField] private float speedPollingPeriod = 0.25f;

        // State
        private bool inWorld = true;
        private float timeSinceSpeedRefreshed = Mathf.Infinity;
        private float cachedSpeed = 1.0f;
        
        private float inputHorizontal;
        private float inputVertical;
        private CircularBuffer<Tuple<Vector2, Vector2>> movementHistory;
        private bool historyResetThisFrame = false;

        // Cached References
        private PlayerStateMachine playerStateMachine;
        private BaseStats partyLeader;

        // Events
        public event Action<Vector2> movementHistoryReset;
        public event Action<MovementAnimationParameters> leadAnimationParametersUpdated;
        public event Action<CircularBuffer<Tuple<Vector2, Vector2>>> playerMoved;

        #region UnityMethods
        protected override void Awake()
        {
            playerStateMachine = GetComponent<PlayerStateMachine>();
            movementHistory = new CircularBuffer<Tuple<Vector2, Vector2>>(playerMovementHistoryLength);
            
            base.Awake();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            playerStateMachine.playerStateChanged += ParsePlayerStateChange;
            if (TryGetComponent(out Party party)) { party.SubscribeToMembersAlteredUpdates(true, HandlePartyUpdate); }
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            playerStateMachine.playerStateChanged -= ParsePlayerStateChange;
            if (TryGetComponent(out Party party)) { party.SubscribeToMembersAlteredUpdates(false, HandlePartyUpdate); }
        }
        
        protected override void FixedUpdate()
        {
            if (!isRigidBodyInitialized) { return; }
            base.FixedUpdate();
            
            if (inWorld) { InteractWithMovement(); }
            PollForSpeedRefresh(Time.deltaTime);
        }
        #endregion

        #region EventListeners
        private void ParsePlayerStateChange(PlayerStateType playerStateType, IPlayerStateContext playerStateContext)
        {
            inWorld = (playerStateType == PlayerStateType.InWorld);
            if (playerStateType == PlayerStateType.InCutScene) { inWorld = playerStateContext.CanMoveInCutscene(); }
            RefreshMoverSpeed();
        }

        private void HandlePartyUpdate(PartyAlteredData partyAlteredData)
        {
            SetMoverToNewLeader(partyAlteredData.GetPartyLeader());
            InitializeRigidBody();
        }
        #endregion

        #region PublicMethods
        public override float GetCurrentSpeed() => cachedSpeed;
        public void ParseMovement(Vector2 directionalInput)
        {
            inputHorizontal = Vector2.Dot(directionalInput, Vector2.right);
            inputVertical = Vector2.Dot(directionalInput, Vector2.up);
        }

        public void ResetHistory(Vector2 newPosition)
        {
            movementHistory.Clear();
            AddToMovementHistory(newPosition);
            movementHistoryReset?.Invoke(newPosition);

            historyResetThisFrame = true;
        }
        #endregion
        
        #region ProtectedPrivateMethods
        protected override void SelfInitializeRigidBody()
        {
            if (!TryGetComponent(out Party party)) { return; }
            if (party.TryGetPartyLeader(out BaseStats newPartyLeader)) { SetMoverToNewLeader(newPartyLeader); }
            InitializeRigidBody();
        }
        
        protected override void UpdateAnimatorParameters(bool useCardinalLookDelay = false)
        {
            leadAnimationParametersUpdated?.Invoke(new MovementAnimationParameters(currentSpeed, lookDirection, snapPlayerToPixelPerfect ? GetSpritePositionOffset() : Vector2.zero));
        }
        
        private void SetMoverToNewLeader(BaseStats newPartyLeader)
        {
            partyLeader = newPartyLeader;
            RefreshMoverSpeed();
        }

        private void InitializeRigidBody()
        {
            if (partyLeader == null) { return; }
            rigidBody2D = partyLeader.GetComponent<Rigidbody2D>();
            isRigidBodyInitialized = rigidBody2D != null;
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
        
        private void MovePlayer()
        {
            currentSpeed = GetCurrentSpeed();
            if (!movementConfiguration.MoveToTarget(this, Time.deltaTime, out Vector2 newPosition)) { return; }
            AddToMovementHistory(newPosition);
            playerMoved?.Invoke(movementHistory);
        }

        private void AddToMovementHistory(Vector2 newPosition)
        {
            if (snapPlayerToPixelPerfect)
            {
                // Note:  We add SpritePositionOffset here so that party members track the lead character sprite visually to mitigate pixel snapping that appears as judder
                movementHistory.Add(new Tuple<Vector2, Vector2>(RoundToPixelPerfect(newPosition + GetSpritePositionOffset()), new Vector2(lookDirection.x, lookDirection.y)));
            }
            else
            {
                movementHistory.Add(new Tuple<Vector2, Vector2>(newPosition, new Vector2(lookDirection.x, lookDirection.y)));
            }
        }

        private void PollForSpeedRefresh(float deltaTime)
        {
            timeSinceSpeedRefreshed += deltaTime;
            if (timeSinceSpeedRefreshed < speedPollingPeriod) { return; }
            RefreshMoverSpeed();
        }

        private void RefreshMoverSpeed()
        {
            if (partyLeader == null) { return; }
            cachedSpeed = movementConfiguration.baseMovementSpeed * partyLeader.GetCalculatedStat(CalculatedStat.MoveSpeed);
            timeSinceSpeedRefreshed = 0f;
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
        #endregion
        
        #region SaveInterface
        public LoadPriority GetLoadPriority() => LoadPriority.ObjectProperty;

        public SaveState CaptureState()
        {
            return partyLeader != null ? ManualGetStateFromData(new SerializableVector2(partyLeader.transform.position)) : null;
        }

        public void RestoreState(SaveState saveState)
        {
            if (!TryManualGetDataFromState(saveState, out SerializableVector2 savedPosition)) { return; }

            // Force initialization for objects set to disable
            SelfInitializeRigidBody();
            SetupInitialState();
            if (partyLeader != null) { partyLeader.transform.position = savedPosition.ToVector(); }
            SetLookDirection(Vector2.down);
        }

        public SaveState ManualGetStateFromData(SerializableVector2 data) => new(GetLoadPriority(), data);

        public bool TryManualGetDataFromState(SaveState saveState, out SerializableVector2 serializableVector2)
        {
            if (saveState != null && saveState.TryGetState(out serializableVector2)) { return true; }
            serializableVector2 = null;
            return false;
        }
        #endregion
    }
}

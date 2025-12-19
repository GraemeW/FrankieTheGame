using System;
using System.Collections.Generic;
using UnityEngine;
using Frankie.ZoneManagement;
using Frankie.Stats;
using Frankie.Combat;
using Frankie.Speech;
using Frankie.Utils;
using Frankie.Core;

namespace Frankie.Control
{
    public class NPCStateHandler : MonoBehaviour
    {
        // Tunables
        [SerializeField] private bool willForceCombat = false;
        [SerializeField] private bool willDestroyIfInvisible = false;
        [Min(0)][Tooltip("in seconds")][SerializeField] private float delayToDestroyAfterInvisible = 2f;
        [Tooltip("Include {0} for enemy name")][SerializeField] private string messageCannotFight = "{0} is wounded and cannot fight.";

        // State
        private NPCStateType npcState = NPCStateType.Idle;
        private bool npcOccupied = false;
        private bool queueDeathOnNextPlayerStateChange = false;
        private bool isNPCVisible = true;
        private float timeSinceInvisible;

        // Cached References
        private SpriteVisibilityAnnouncer spriteVisibilityAnnouncer;
        private CombatParticipant combatParticipant;
        private ReInitLazyValue<GameObject> player;
        private ReInitLazyValue<PlayerStateMachine> playerStateMachine;
        private ReInitLazyValue<PlayerController> playerController;

        // Events
        public event Action<NPCStateType, bool> npcStateChanged;

        #region UnityMethods
        private void Awake()
        {
            // Not strictly necessary -- will fail elegantly
            combatParticipant = GetComponent<CombatParticipant>();
            spriteVisibilityAnnouncer = GetComponentInChildren<SpriteVisibilityAnnouncer>();

            // Cached
            player = new ReInitLazyValue<GameObject>(Player.FindPlayerObject);
            playerStateMachine = new ReInitLazyValue<PlayerStateMachine>(SetupPlayerStateMachine);
            playerController = new ReInitLazyValue<PlayerController>(SetupPlayerController);
        }

        private void Start()
        {
            player.ForceInit();
            playerStateMachine.ForceInit();
            playerController.ForceInit();
        }

        private PlayerStateMachine SetupPlayerStateMachine() => player.value?.GetComponent<PlayerStateMachine>();
        private PlayerController SetupPlayerController() => player.value?.GetComponent<PlayerController>();

        private void OnEnable()
        {
            playerStateMachine.value.playerStateChanged += ParsePlayerStateChange;
            if (combatParticipant != null) { combatParticipant.SubscribeToStateUpdates(HandleNPCCombatStateChange); }
            if (spriteVisibilityAnnouncer != null) { spriteVisibilityAnnouncer.spriteVisibilityStatus += HandleSpriteVisibility; }
            SetNPCState(NPCStateType.Idle);
        }

        private void OnDisable()
        {
            playerStateMachine.value.playerStateChanged -= ParsePlayerStateChange;
            if (combatParticipant != null) { combatParticipant.UnsubscribeToStateUpdates(HandleNPCCombatStateChange); }
            if (spriteVisibilityAnnouncer != null) { spriteVisibilityAnnouncer.spriteVisibilityStatus -= HandleSpriteVisibility; }
        }

        private void Update()
        {
            UpdateSpriteInvisibilityTimerToDestroy();
        }
        #endregion

        #region PublicMethods
        public CombatParticipant GetCombatParticipant() => combatParticipant;
        public GameObject GetPlayer() => player.value;
        public Vector2 GetPlayerLookDirection() => playerController.value.GetPlayerMover().GetLookDirection();
        public Vector2 GetPlayerInteractionPosition() => playerController.value.GetInteractionPosition();
        public bool WillForceCombat() => willForceCombat;
        #endregion

        #region StateUtilityMethods
        public void SetNPCIdle() => SetNPCState(NPCStateType.Idle); // Callable via Unity Event
        public void SetNPCSuspicious() => SetNPCState(NPCStateType.Suspicious); // Callable via Unity Event

        public void SetNPCAggravated() // Callable via Unity Event
        {
            SetNPCState(NPCStateType.Aggravated);
        }

        public void SetNPCFrenzied() // Callable via Unity Event
        {
            SetNPCState(NPCStateType.Frenzied);
        }

        public void InitiateCombat(PlayerStateMachine playerStateHandler)  // called via Unity Event
        {
            InitiateCombat(playerStateHandler, TransitionType.BattleNeutral);
        }

        public void InitiateCombatAdvantaged(PlayerStateMachine playerStateHandler)  // called via Unity Event
        {
            InitiateCombat(playerStateHandler, TransitionType.BattleGood);
        }

        public void InitiateCombatDisadvantaged(PlayerStateMachine playerStateHandler)  // called via Unity Event
        {
            InitiateCombat(playerStateHandler, TransitionType.BattleBad);
        }

        public void InitiateCombat(TransitionType transitionType) // called via Unity Event
        {
            InitiateCombat(playerStateMachine.value, transitionType);
        }

        public void InitiateCombat(TransitionType transitionType, List<NPCStateHandler> npcMob) // Aggro, not callable via Unity Events
        {
            InitiateCombat(playerStateMachine.value, transitionType, npcMob);
        }

        public void InitiateDialogue(TransitionType transitionType) // called via Unity Event
        {
            InitiateDialogue(playerStateMachine.value);
        }

        public void ForceNPCOccupied()
        {
            npcOccupied = true;
        }
        #endregion

        #region PrivateMethods
        private void SetNPCState(NPCStateType setNPCState)
        {
            bool occupiedStatusChange = (setNPCState == NPCStateType.Occupied) ^ npcOccupied;
            if (npcState == setNPCState && !occupiedStatusChange) { return; }

            // Occupied treated as a pseudo-state to allow for state persistence
            // i.e. State reset viable on SetNPCState(this.npcState)
            if (setNPCState == NPCStateType.Occupied) { npcOccupied = true; }
            else
            {
                npcOccupied = false;

                // Set State
                npcState = setNPCState;
            }

            bool isNPCAfraid = CheckForNPCAfraid();
            npcStateChanged?.Invoke(npcOccupied ? NPCStateType.Occupied : setNPCState, isNPCAfraid);
            Debug.Log($"Updating {gameObject.name} NPC state to: {Enum.GetName(typeof(NPCStateType), npcOccupied ? NPCStateType.Occupied : setNPCState)}");
        }

        private void InitiateCombat(PlayerStateMachine playerStateHandler, TransitionType transitionType, List<NPCStateHandler> npcMob = null)
        {
            if (combatParticipant == null) { return; }

            if (combatParticipant.IsDead())
            {
                playerStateHandler.EnterDialogue(string.Format(messageCannotFight, combatParticipant.GetCombatName()));
                SetNPCState(NPCStateType.Occupied);
            }
            else
            {
                var enemies = new List<CombatParticipant> { combatParticipant };

                if (npcMob != null)
                {
                    foreach (NPCStateHandler npcInContact in npcMob)
                    {
                        enemies.Add(npcInContact.GetCombatParticipant());
                        npcInContact.SetNPCState(NPCStateType.Occupied); // Occupy NPCs as they're entered into combat
                    }
                }

                playerStateHandler.EnterCombat(enemies, transitionType);
                SetNPCState(NPCStateType.Occupied); // Occupy calling NPC as it's entered into combat
            }
        }

        private void InitiateDialogue(PlayerStateMachine playerStateHandler)
        {
            var aiConversant = GetComponentInChildren<AIConversant>();
            if (aiConversant == null) { return; }

            Dialogue dialogue = aiConversant.GetDialogue();
            if (dialogue == null) { return; }

            playerStateHandler.EnterDialogue(aiConversant, dialogue);
            SetNPCState(NPCStateType.Occupied);
        }
        
        private bool CheckForNPCAfraid()
        {
            if (npcState is not (NPCStateType.Aggravated or NPCStateType.Suspicious)) { return false; }
            if (combatParticipant == null) { return false; }
            
            Party party = playerStateMachine.value.GetParty();
            if (party == null) { return false; }
            
            bool isNPCAfraid = false;
            if (party.TryGetComponent(out PartyCombatConduit partyCombatConduit))
            {
                if (!partyCombatConduit.IsAnyMemberAlive()) { return false; }
                isNPCAfraid = partyCombatConduit.IsFearsome(combatParticipant);
            }
            return isNPCAfraid;
        }

        private void HandleSpriteVisibility(bool isVisible)
        {
            isNPCVisible = isVisible;
            if (!isVisible) { timeSinceInvisible = 0f; }
        }

        private void UpdateSpriteInvisibilityTimerToDestroy()
        {
            if (!willDestroyIfInvisible) { return; }
            if (isNPCVisible) { return; }

            if (timeSinceInvisible < delayToDestroyAfterInvisible)
            {
                timeSinceInvisible += Time.deltaTime;
            }
            else
            {
                Debug.Log($"NPC {gameObject.name} invisible for {delayToDestroyAfterInvisible} seconds.  Destroying.");
                Destroy(gameObject);
            }
        }
        #endregion

        #region EventListeners
        private void ParsePlayerStateChange(PlayerStateType playerState)
        {
            if (queueDeathOnNextPlayerStateChange)
            {
                Destroy(gameObject);
            }

            switch (playerState)
            {
                case PlayerStateType.inDialogue:
                case PlayerStateType.inBattle:
                case PlayerStateType.inMenus:
                    SetNPCState(NPCStateType.Occupied);
                    break;
                case PlayerStateType.inTransition:
                    if (playerStateMachine.value.InZoneTransition())
                    {
                        SetNPCState(NPCStateType.Occupied);
                    }
                    else if (playerStateMachine.value.InBattleExitTransition())
                    {
                        SetNPCState(NPCStateType.Idle);
                        SetNPCState(NPCStateType.Occupied);
                    }
                    // other transitions allow enemy movement -- swarm mechanic
                    break;
                case PlayerStateType.inCutScene:
                case PlayerStateType.inWorld:
                default:
                    SetNPCState(npcState);
                    break;
            }
        }

        private void HandleNPCCombatStateChange(StateAlteredInfo stateAlteredInfo)
        {
            if (combatParticipant == null) { return; }

            if (stateAlteredInfo.stateAlteredType == StateAlteredType.Dead && combatParticipant.ShouldDestroySelfOnDeath())
            {
                queueDeathOnNextPlayerStateChange = true;
            }
        }
        #endregion
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;
using Frankie.ZoneManagement;
using Frankie.Combat;
using Frankie.Speech;
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
        private bool isNPCAfraid = false;
        private bool isNPCVisible = true;
        private float timeSinceInvisible;

        // Cached References
        private SpriteVisibilityAnnouncer spriteVisibilityAnnouncer;
        private CombatParticipant combatParticipant;

        // Events
        public event Action<NPCStateType, bool> npcStateChanged;

        #region UnityMethods
        private void Awake()
        {
            // Not strictly necessary -- will fail elegantly
            combatParticipant = GetComponent<CombatParticipant>();
            spriteVisibilityAnnouncer = GetComponentInChildren<SpriteVisibilityAnnouncer>();
        }

        private void Start()
        {
            // Must init in Start due to object readiness
            InitializeNPCRunDisposition();
        }

        private void OnEnable()
        {
            SetupPlayerListener(true);
            if (combatParticipant != null) { combatParticipant.SubscribeToStateUpdates(HandleNPCCombatStateChange); }
            if (spriteVisibilityAnnouncer != null) { spriteVisibilityAnnouncer.spriteVisibilityStatus += HandleSpriteVisibility; }
            SetNPCState(NPCStateType.Idle);
        }

        private void OnDisable()
        {
            SetupPlayerListener(false);
            if (combatParticipant != null) { combatParticipant.UnsubscribeToStateUpdates(HandleNPCCombatStateChange); }
            if (spriteVisibilityAnnouncer != null) { spriteVisibilityAnnouncer.spriteVisibilityStatus -= HandleSpriteVisibility; }
        }

        private void SetupPlayerListener(bool enable)
        {
            PlayerStateMachine playerStateMachine = Player.FindPlayerStateMachine();
            if (playerStateMachine == null) { return; }
            
            if (enable) { playerStateMachine.playerStateChanged += ParsePlayerStateChange; }
            else { playerStateMachine.playerStateChanged -= ParsePlayerStateChange; }
        }

        private void Update()
        {
            UpdateSpriteInvisibilityTimerToDestroy();
        }
        #endregion

        #region PublicMethods
        public bool WillForceCombat() => willForceCombat;

        public void SetNPCIdle() // Callable via Unity Event
        {
            SetNPCState(NPCStateType.Idle); 
        }
        public void SetNPCSuspicious() // Callable via Unity Event
        {
            SetNPCState(NPCStateType.Suspicious);
        }
        public void SetNPCAggravated() // Callable via Unity Event
        {
            SetNPCState(NPCStateType.Aggravated);
        }
        public void SetNPCFrenzied() // Callable via Unity Event
        {
            SetNPCState(NPCStateType.Frenzied);
        }
        public void ForceNPCOccupied()
        {
            npcOccupied = true;
        }

        public void InitiateCombat(PlayerStateMachine playerStateMachine)  // called via Unity Event
        {
            InitiateCombat(TransitionType.BattleNeutral);
        }

        public void InitiateCombatAdvantaged(PlayerStateMachine playerStateHandler)  // called via Unity Event
        {
            InitiateCombat(TransitionType.BattleGood);
        }

        public void InitiateCombatDisadvantaged(PlayerStateMachine playerStateHandler)  // called via Unity Event
        {
            InitiateCombat(TransitionType.BattleBad);
        }

        public void InitiateCombat(TransitionType transitionType) // called via Unity Event
        {
            InitiateCombat(transitionType, new List<NPCStateHandler>());
        }

        public void InitiateDialogue(TransitionType transitionType) // called via Unity Event
        {
            InitiateDialogue();
        }
        
        public void SelfDestruct() // called via Unity Event
        {
            Destroy(gameObject);
        }
        #endregion

        #region PrivateMethods
        private CombatParticipant GetCombatParticipant() => combatParticipant;
        
        private void InitializeNPCRunDisposition()
        {
            PlayerStateMachine playerStateMachine = Player.FindPlayerStateMachine();
            if (playerStateMachine == null) { return; }
            
            CheckForNPCAfraid(playerStateMachine, true);
            SetNPCState(npcState, true);
        }
        
        private void SetNPCState(NPCStateType setNPCState, bool overrideStateCheck = false)
        {
            if (!overrideStateCheck)
            {
                bool occupiedStatusChange = (setNPCState == NPCStateType.Occupied) ^ npcOccupied;
                if (npcState == setNPCState && !occupiedStatusChange) { return; }
            }

            // Occupied treated as a pseudo-state to allow for state persistence
            // i.e. State reset viable on SetNPCState(this.npcState)
            if (setNPCState == NPCStateType.Occupied) { npcOccupied = true; }
            else
            {
                npcOccupied = false;

                // Set State
                npcState = setNPCState;
            }
            
            npcStateChanged?.Invoke(npcOccupied ? NPCStateType.Occupied : setNPCState, isNPCAfraid);
            Debug.Log($"Updating {gameObject.name} NPC state to: {Enum.GetName(typeof(NPCStateType), npcOccupied ? NPCStateType.Occupied : setNPCState)}");
        }

        public void InitiateCombat(TransitionType transitionType, List<NPCStateHandler> npcMob)
        {
            if (combatParticipant == null) { return; }
            PlayerStateMachine playerStateMachine = Player.FindPlayerStateMachine();
            if (playerStateMachine == null) { return; }

            if (combatParticipant.IsDead())
            {
                playerStateMachine.EnterDialogue(string.Format(messageCannotFight, combatParticipant.GetCombatName()));
                SetNPCState(NPCStateType.Occupied);
            }
            else
            {
                var enemies = new List<CombatParticipant> { combatParticipant };
                if (npcMob is { Count: > 0 })
                {
                    foreach (NPCStateHandler npcInContact in npcMob)
                    {
                        enemies.Add(npcInContact.GetCombatParticipant());
                        npcInContact.SetNPCState(NPCStateType.Occupied); // Occupy NPCs as they're entered into combat
                    }
                }

                playerStateMachine.EnterCombat(enemies, transitionType);
                SetNPCState(NPCStateType.Occupied); // Occupy calling NPC as it's entered into combat
            }
        }

        private void InitiateDialogue()
        {
            var aiConversant = GetComponentInChildren<AIConversant>();
            if (aiConversant == null) { return; }
            
            PlayerStateMachine playerStateMachine = Player.FindPlayerStateMachine();
            if (playerStateMachine == null) { return; }

            Dialogue dialogue = aiConversant.GetDialogue();
            if (dialogue == null) { return; }

            playerStateMachine.EnterDialogue(aiConversant, dialogue);
            SetNPCState(NPCStateType.Occupied);
        }
        
        private void CheckForNPCAfraid(IPlayerStateContext playerStateContext, bool overrideStateCheck = false)
        {
            if (!overrideStateCheck && npcState is not (NPCStateType.Aggravated or NPCStateType.Suspicious)) { return; }
            if (combatParticipant == null) { return; }
            
            if (!playerStateContext.IsAnyPartyMemberAlive()) { return; }
            isNPCAfraid = playerStateContext.IsPlayerFearsome(combatParticipant);
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
        private void ParsePlayerStateChange(PlayerStateType playerState, IPlayerStateContext playerStateContext)
        {
            if (queueDeathOnNextPlayerStateChange) { Destroy(gameObject); }
            
            switch (playerState)
            {
                case PlayerStateType.inDialogue:
                case PlayerStateType.inBattle:
                case PlayerStateType.inMenus:
                    SetNPCState(NPCStateType.Occupied);
                    break;
                case PlayerStateType.inTransition:
                    if (playerStateContext.InZoneTransition())
                    {
                        SetNPCState(NPCStateType.Occupied);
                    }
                    else if (playerStateContext.InBattleExitTransition())
                    {
                        SetNPCState(NPCStateType.Idle);
                        SetNPCState(NPCStateType.Occupied);
                    }
                    // other transitions allow enemy movement -- swarm mechanic
                    break;
                case PlayerStateType.inCutScene:
                case PlayerStateType.inWorld:
                default:
                    CheckForNPCAfraid(playerStateContext);
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

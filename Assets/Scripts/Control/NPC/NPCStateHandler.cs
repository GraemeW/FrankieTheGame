using System;
using System.Collections.Generic;
using UnityEngine;
using Frankie.Core;
using Frankie.Combat;
using Frankie.Speech;
using Frankie.ZoneManagement;

namespace Frankie.Control
{
    public class NPCStateHandler : MonoBehaviour
    {
        // Tunables
        [SerializeField] private bool willForceCombat = false;
        [SerializeField] private bool willDestroyIfInvisible = false;
        [Min(0)][Tooltip("in seconds")][SerializeField] private float delayToDestroyAfterInvisible = 2f;

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
        public void ForceNPCOccupied() => npcOccupied = true;
        public void RetransmitState() => SetNPCState(npcState, true);
        
        // Callable via Unity Events
        public void SetNPCIdle() => SetNPCState(NPCStateType.Idle);
        public void SetNPCSuspicious() => SetNPCState(NPCStateType.Suspicious);
        public void SetNPCAggravated() => SetNPCState(NPCStateType.Aggravated);
        public void SetNPCFrenzied() => SetNPCState(NPCStateType.Frenzied);
        public void InitiateCombat(PlayerStateMachine playerStateMachine) => InitiateCombat(TransitionType.BattleNeutral);
        public void InitiateCombatAdvantaged(PlayerStateMachine playerStateMachine) => InitiateCombat(TransitionType.BattleGood);
        public void InitiateCombatDisadvantaged(PlayerStateMachine playerStateMachine) => InitiateCombat(TransitionType.BattleBad);
        public void InitiateCombat(TransitionType transitionType) => InitiateCombat(transitionType, new List<NPCStateHandler>());
        public void InitiateDialogue(TransitionType transitionType) => InitiateDialogue();
        public void SelfDestruct() => Destroy(gameObject);
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
                playerStateMachine.SetupCannotFightPrompt(combatParticipant.GetCombatName());
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

            aiConversant.ForceInteractionEvent(playerStateMachine);
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
                case PlayerStateType.InDialogue:
                case PlayerStateType.InBattle:
                case PlayerStateType.InMenus:
                    SetNPCState(NPCStateType.Occupied);
                    break;
                case PlayerStateType.InTransition:
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
                case PlayerStateType.InCutScene:
                case PlayerStateType.InWorld:
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

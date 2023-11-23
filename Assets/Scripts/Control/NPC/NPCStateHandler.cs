using System.Collections.Generic;
using UnityEngine;
using Frankie.ZoneManagement;
using Frankie.Stats;
using Frankie.Combat;
using System;
using Frankie.Speech;
using Frankie.Utils;
using System.Linq;

namespace Frankie.Control
{
    public class NPCStateHandler : MonoBehaviour
    {
        // Tunables
        [SerializeField] bool willForceCombat = false;
        [SerializeField] bool willDestroySelfOnDeath = true;
        [SerializeField] bool willDestroyIfInvisible = false;
        [Min(0)][Tooltip("in seconds")][SerializeField] float delayToDestroyAfterInvisible = 2f;
        [Tooltip("Include {0} for enemy name")] [SerializeField] string messageCannotFight = "{0} is wounded and cannot fight.";

        // State
        NPCStateType npcState = NPCStateType.idle;
        bool npcOccupied = false;
        bool queueDeathOnNextPlayerStateChange = false;
        bool isNPCVisible = true;
        float timeSinceInvisible = 0f;

        // Cached References
        SpriteVisibilityAnnouncer spriteVisibilityAnnouncer;
        CombatParticipant combatParticipant = null;
        GameObject player = null;
        ReInitLazyValue<PlayerStateMachine> playerStateHandler;
        ReInitLazyValue<PlayerController> playerController;

        // Events
        public event Action<NPCStateType, bool> npcStateChanged;

        #region UnityMethods
        private void Awake()
        {
            // Not strictly necessary -- will fail elegantly
            combatParticipant = GetComponent<CombatParticipant>();
            spriteVisibilityAnnouncer = GetComponentInChildren<SpriteVisibilityAnnouncer>();

            // Cached
            player = GameObject.FindGameObjectWithTag("Player");
            playerStateHandler = new ReInitLazyValue<PlayerStateMachine>(SetupPlayerStateHandler);
            playerController = new ReInitLazyValue<PlayerController>(SetupPlayerController);
        }

        private void Start()
        {
            playerStateHandler.ForceInit();
            playerController.ForceInit();
        }

        private PlayerStateMachine SetupPlayerStateHandler()
        {
            if (player == null) { player = GameObject.FindGameObjectWithTag("Player"); }
            return player?.GetComponent<PlayerStateMachine>();
        }

        private PlayerController SetupPlayerController()
        {
            if (player == null) { player = GameObject.FindGameObjectWithTag("Player"); }
            return player?.GetComponent<PlayerController>();
        }

        private void OnEnable()
        {
            playerStateHandler.value.playerStateChanged += ParsePlayerStateChange;
            if (combatParticipant != null) { combatParticipant.stateAltered += HandleNPCCombatStateChange; }
            if (spriteVisibilityAnnouncer != null) { spriteVisibilityAnnouncer.spriteVisibilityStatus += HandleSpriteVisibility; }
            SetNPCState(NPCStateType.idle);
        }

        private void OnDisable()
        {
            playerStateHandler.value.playerStateChanged -= ParsePlayerStateChange;
            if (combatParticipant != null) { combatParticipant.stateAltered -= HandleNPCCombatStateChange; }
            if (spriteVisibilityAnnouncer != null) { spriteVisibilityAnnouncer.spriteVisibilityStatus -= HandleSpriteVisibility; }
        }

        private void Update()
        {
            UpdateSpriteInvisibilityTimerToDestroy();
        }
        #endregion

        #region PublicMethods
        public CombatParticipant GetCombatParticipant() => combatParticipant;
        public GameObject GetPlayer() => player;
        public Vector2 GetPlayerLookDirection() => playerController.value.GetPlayerMover().GetLookDirection();
        public Vector2 GetPlayerInteractionPosition() => playerController.value.GetInteractionPosition();
        public bool WillForceCombat() => willForceCombat;
        public bool WillDestroySelfOnDeath() => willDestroySelfOnDeath;
        #endregion

        #region StateUtilityMethods
        public void SetNPCIdle() => SetNPCState(NPCStateType.idle); // Callable via Unity Event
        public void SetNPCSuspicious() => SetNPCState(NPCStateType.suspicious); // Callable via Unity Event

        public void SetNPCAggravated() // Callable via Unity Event
        {
            SetNPCState(NPCStateType.aggravated);
        }

        public void SetNPCFrenzied() // Callable via Unity Event
        {
            SetNPCState(NPCStateType.frenzied);
        }

        public void InitiateCombat(PlayerStateMachine playerStateHandler)  // called via Unity Event
        {
            InitiateCombat(playerStateHandler, TransitionType.BattleNeutral);
        }

        public void InitiateCombat(TransitionType transitionType) // called via Unity Event
        {
            InitiateCombat(playerStateHandler.value, transitionType);
        }

        public void InitiateCombat(TransitionType transitionType, List<NPCStateHandler> npcMob) // Aggro, not callable via Unity Events
        {
            InitiateCombat(playerStateHandler.value, transitionType, npcMob);
        }

        public void InitiateDialogue(TransitionType transitionType) // called via Unity Event
        {
            InitiateDialogue(playerStateHandler.value);
        }

        public void ForceNPCOccupied()
        {
            npcOccupied = true;
        }
        #endregion

        #region PrivateMethods
        private void SetNPCState(NPCStateType npcState)
        {
            bool isNPCAfraid = false;
            if (npcState == NPCStateType.aggravated || npcState == NPCStateType.suspicious)
            {
                Party party = playerStateHandler.value.GetParty();
                if (party == null) { return; }

                if (party.TryGetComponent(out PartyCombatConduit partyCombatConduit))
                {
                    if (!partyCombatConduit.IsAnyMemberAlive()) { return; }
                    isNPCAfraid = partyCombatConduit.IsFearsome(combatParticipant);
                }
            }

            bool occupiedStatusChange = (npcState == NPCStateType.occupied) ^ npcOccupied;
            if (this.npcState == npcState && !occupiedStatusChange) { return; }

            // Occupied treated as a pseudo-state to allow for state persistence
            // i.e. State reset viable on SetNPCState(this.npcState)
            if (npcState == NPCStateType.occupied) { npcOccupied = true; }
            else
            {
                npcOccupied = false;

                // Set State
                this.npcState = npcState;
            }

            UnityEngine.Debug.Log($"Updating {gameObject.name} NPC state to: {Enum.GetName(typeof(NPCStateType), npcOccupied ? NPCStateType.occupied : npcState)}");
            npcStateChanged?.Invoke(npcOccupied ? NPCStateType.occupied : npcState, isNPCAfraid);

            return;
        }

        private void InitiateCombat(PlayerStateMachine playerStateHandler, TransitionType transitionType, List<NPCStateHandler> npcMob = null)
        {
            if (combatParticipant == null) { return; }

            if (combatParticipant.IsDead())
            {
                playerStateHandler.EnterDialogue(string.Format(messageCannotFight, combatParticipant.GetCombatName()));
                SetNPCState(NPCStateType.occupied);
            }
            else
            {
                List<CombatParticipant> enemies = new List<CombatParticipant>();
                enemies.Add(combatParticipant);

                if (npcMob != null)
                {
                    foreach (NPCStateHandler npcInContact in npcMob)
                    {
                        enemies.Add(npcInContact.GetCombatParticipant());
                        npcInContact.SetNPCState(NPCStateType.occupied); // Occupy NPCs as they're entered into combat
                    }
                }

                playerStateHandler.EnterCombat(enemies, transitionType);
                SetNPCState(NPCStateType.occupied); // Occupy calling NPC as it's entered into combat
            }
        }

        private void InitiateDialogue(PlayerStateMachine playerStateHandler)
        {
            AIConversant aiConversant = GetComponentInChildren<AIConversant>();
            if (aiConversant == null) { return; }

            Dialogue dialogue = aiConversant.GetDialogue();
            if (dialogue == null) { return; }

            playerStateHandler.EnterDialogue(aiConversant, dialogue);
            SetNPCState(NPCStateType.occupied);
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
                UnityEngine.Debug.Log($"NPC {gameObject.name} invisible for {delayToDestroyAfterInvisible} seconds.  Destroying.");
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
                    SetNPCState(NPCStateType.occupied);
                    break;
                case PlayerStateType.inTransition:
                    if (playerStateHandler.value.InZoneTransition())
                    {
                        SetNPCState(NPCStateType.occupied);
                    }
                    else if (playerStateHandler.value.InBattleExitTransition())
                    {
                        SetNPCState(NPCStateType.idle);
                        SetNPCState(NPCStateType.occupied);
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

        private void HandleNPCCombatStateChange(CombatParticipant combatParticipant, StateAlteredData stateAlteredData)
        {
            if (stateAlteredData.stateAlteredType == StateAlteredType.Dead && willDestroySelfOnDeath)
            {
                queueDeathOnNextPlayerStateChange = true;
            }
        }
        #endregion
    }
}
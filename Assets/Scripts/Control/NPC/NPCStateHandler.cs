using System.Collections.Generic;
using UnityEngine;
using Frankie.ZoneManagement;
using Frankie.Stats;
using Frankie.Combat;
using UnityEngine.Events;
using System;
using Frankie.Speech;
using Frankie.Utils;
using System.Linq;

namespace Frankie.Control
{
    public class NPCStateHandler : MonoBehaviour
    {
        // Tunables
        [Header("Base Properties")]
        [Tooltip("Only used if not found via base stats")] [SerializeField] string defaultName = "";
        [Tooltip("Include {0} for enemy name")] [SerializeField] string messageCannotFight = "{0} is wounded and cannot fight.";
        [Header("Chase Properties")]
        [SerializeField] bool willChasePlayer = false;
        [SerializeField] bool disableCollisionEventsWhenDead = true;
        [SerializeField] bool disableCollisionEventsWhenIdle = true;
        [SerializeField] float chaseDistance = 3.0f;
        [SerializeField] float aggravationTime = 3.0f;
        [SerializeField] float suspicionTime = 3.0f;
        [Header("Other Mob Properties")]
        [Tooltip("Must be true to be shouted at, regardless of group")] [SerializeField] bool canBeShoutedAt = true;
        [Tooltip("From interaction center point of NPC")] [SerializeField] float shoutDistance = 2.0f;
        [Tooltip("Set to nothing to aggro everything shoutable")] [SerializeField] NPCStateHandler[] shoutGroup = null;

        // State
        NPCState npcState = NPCState.idle;
        bool npcOccupied = false;
        float timeSinceLastSawPlayer = Mathf.Infinity;
        bool currentChasePlayerDisposition = false;
        bool collisionsOverriddenToEnterCombat = false;

        bool touchingPlayer = false;
        List<NPCStateHandler> currentNPCCollisions = new List<NPCStateHandler>();

        // Cached References
        BaseStats baseStats = null;
        NPCMover npcMover = null;
        CombatParticipant combatParticipant = null;
        GameObject player = null;
        ReInitLazyValue<PlayerStateHandler> playerStateHandler;
        ReInitLazyValue<PlayerController> playerController;

        // Events
        public CollisionEvent collidedWithPlayer;

        // Data Structures
        [System.Serializable]
        public class CollisionEvent : UnityEvent<PlayerStateHandler, TransitionType>
        {
        }

        #region UnityStandardMethods
        private void Awake()
        {
            baseStats = GetComponent<BaseStats>();
            npcMover = GetComponent<NPCMover>();
            combatParticipant = GetComponent<CombatParticipant>();

            player = GameObject.FindGameObjectWithTag("Player");
            playerStateHandler = new ReInitLazyValue<PlayerStateHandler>(SetupPlayerStateHandler);
            playerController = new ReInitLazyValue<PlayerController>(SetupPlayerController);
        }

        private void Start()
        {
            playerStateHandler.ForceInit();
            playerController.ForceInit();
        }

        private PlayerStateHandler SetupPlayerStateHandler()
        {
            if (player == null) { player = GameObject.FindGameObjectWithTag("Player"); }
            return player.GetComponent<PlayerStateHandler>();
        }

        private PlayerController SetupPlayerController()
        {
            if (player == null) { player = GameObject.FindGameObjectWithTag("Player"); }
            return player.GetComponent<PlayerController>();
        }

        private void OnEnable()
        {
            playerStateHandler.value.playerStateChanged += HandlePlayerStateChange;
            ResetNPCState();
        }

        private void OnDisable()
        {
            playerStateHandler.value.playerStateChanged -= HandlePlayerStateChange;
        }

        private void ResetNPCState()
        {
            SetNPCState(NPCState.idle);
            timeSinceLastSawPlayer = Mathf.Infinity;
            currentChasePlayerDisposition = willChasePlayer;
        }

        private void Update() 
        {
            // Calling as late update instead of late to prevent Start() methods on other objects getting called AFTER another object's Update()
            // i.e. for shouting behavior -- yes, this actually happens after scene loading

            if (!currentChasePlayerDisposition || GetNPCState() == NPCState.occupied) { return; }

            CheckForPlayerProximity();
            timeSinceLastSawPlayer += Time.deltaTime;
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (disableCollisionEventsWhenDead && combatParticipant.IsDead()) { return; }
            if (disableCollisionEventsWhenIdle && GetNPCState() == NPCState.idle) { return; }

            if (HandlePlayerCollisions(collision)) { return; }
            if (HandleNPCCollisions(collision)) { return; }
        }

        private void OnCollisionExit2D(Collision2D collision)
        {
            if (collision.gameObject.CompareTag("Player")) { touchingPlayer = false; }

            collision.gameObject.TryGetComponent(out NPCStateHandler collisionNPC);
            if (collisionNPC != null)
            {
                currentNPCCollisions.Remove(collisionNPC);
            }

        }

        private bool HandlePlayerCollisions(Collision2D collision)
        {
            if (!collision.gameObject.CompareTag("Player")) { return false; }
            touchingPlayer = true;

            if (collisionsOverriddenToEnterCombat) // Applied for aggro situations
            {
                TransitionType battleEntryType = GetBattleEntryType(collision);
                InitiateCombat(playerStateHandler.value, battleEntryType);
                return true;
            }
            else if (collidedWithPlayer != null) // Event hooked up in Unity
            {
                TransitionType battleEntryType = GetBattleEntryType(collision);
                collidedWithPlayer.Invoke(playerStateHandler.value, battleEntryType);
                return true;
            }
            return false;
        }

        private bool HandleNPCCollisions(Collision2D collision)
        {
            if (!collisionsOverriddenToEnterCombat) { return false; }

            collision.gameObject.TryGetComponent(out NPCStateHandler collisionNPC);
            if (collisionNPC == null) { return false; }

            if (collisionNPC.IsTouchingPlayer())
            {
                if (!currentNPCCollisions.Contains(collisionNPC)) { currentNPCCollisions.Add(collisionNPC); }

                TransitionType battleEntryType = GetBattleEntryType(collision);
                InitiateCombat(playerStateHandler.value, battleEntryType);
                return true;
            }
            return false;
        }
        #endregion

        #region PublicMethods
        public string GetName()
        {
            if (baseStats != null)
            {
                // Split apart name on lower case followed by upper case w/ or w/out underscores
                return baseStats.GetCharacterProperties().GetCharacterNamePretty();
            }
            return defaultName;
        }

        public NPCState GetNPCState()
        {
            if (npcOccupied) { return NPCState.occupied; }
            return npcState;
        }

        public bool IsShoutable()
        {
            return canBeShoutedAt;
        }

        public bool SetNPCState(NPCState npcState, bool shoutOnAggravation = true)
        {
            if (GetNPCState() == npcState) { return false; }
            if (npcState == NPCState.aggravated && !playerStateHandler.value.GetParty().IsAnyMemberAlive()) { return false; }

            // Occupied treated as a pseudo-state to allow for state persistence
            if (npcState == NPCState.occupied) { npcOccupied = true; }
            else
            {
                npcOccupied = false;

                // Set State
                this.npcState = npcState;
            }

            // Adjust reaction
            AdjustReactionForState(shoutOnAggravation);

            return true;
        }

        public void OverrideCollisionToEnterCombat(bool enable)
        {
            collisionsOverriddenToEnterCombat = enable;
        }

        private void ShoutToNearbyNPCs()
        {
            if (combatParticipant.IsDead()) { return; }

            RaycastHit2D[] hits = npcMover.NPCCastFromSelf(shoutDistance);
            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider.gameObject.TryGetComponent(out NPCStateHandler npcInRange))
                {
                    if (!npcInRange.IsShoutable()) { continue; }
                    if (shoutGroup.Length == 0 || shoutGroup.Contains(npcInRange)) // Default behavior, not set, aggro everything shoutable
                    {
                        if (npcInRange.SetNPCState(NPCState.aggravated, false)) // Do not chain shouts (shout on aggravation set to false)
                        {
                            // Override colissions if successfully aggro'd to allow swarm
                            npcInRange.OverrideCollisionToEnterCombat(true);
                        }
                    }
                }
            }
        }

        public bool IsTouchingPlayer()
        {
            bool touchingNPCTouchingPlayer = currentNPCCollisions.Any(x => x.IsTouchingPlayer());
            return touchingPlayer || touchingNPCTouchingPlayer;
        }
        #endregion

        #region UnityCalledMethods
        public void SetChasePlayerDisposition(bool enable) // Called via Unity Event
        {
            currentChasePlayerDisposition = enable;
        }

        public void InitiateCombat(PlayerStateHandler playerStateHandler)  // called via Unity Event
        {
            if (playerStateHandler.GetPlayerState() != PlayerState.inWorld) { return; }

            InitiateCombat(playerStateHandler, TransitionType.BattleNeutral);
        }

        public void InitiateCombat(PlayerStateHandler playerStateHandler, TransitionType transitionType)  // called via Unity Event
        {
            if (playerStateHandler.GetPlayerState() == PlayerState.inBattle) { return; }

            CombatParticipant enemy = GetComponent<CombatParticipant>();
            if (enemy.IsDead())
            {
                playerStateHandler.EnterDialogue(string.Format(messageCannotFight, enemy.GetCombatName()));
                SetNPCState(NPCState.idle);
            }
            else
            {
                List<CombatParticipant> enemies = new List<CombatParticipant>();
                enemies.Add(enemy);
                bool enteredCombat = playerStateHandler.EnterCombat(enemies, transitionType);

                if (!enteredCombat) { SetNPCState(NPCState.idle); }
            }
        }

        public void InitiateDialogue(PlayerStateHandler playerStateHandler) // called via Unity Event
        {
            InitiateDialogue(playerStateHandler, TransitionType.None);
        }

        public void InitiateDialogue(PlayerStateHandler playerStateHandler, TransitionType transitionType) // called via Unity Event
        {
            AIConversant aiConversant = GetComponentInChildren<AIConversant>();
            if (aiConversant == null) { return; }

            Dialogue dialogue = aiConversant.GetDialogue();
            if (dialogue == null) { return; }

            playerStateHandler.EnterDialogue(aiConversant, dialogue);
        }
        #endregion

        #region PrivateMethods
        private void CheckForPlayerProximity()
        {
            if (SmartVector2.CheckDistance(npcMover.GetInteractionPosition(), playerController.value.GetInteractionPosition(), chaseDistance))
            {
                timeSinceLastSawPlayer = 0f;
            }

            if (timeSinceLastSawPlayer < aggravationTime)
            {
                SetNPCState(NPCState.aggravated);
            }
            else if (timeSinceLastSawPlayer > aggravationTime && (timeSinceLastSawPlayer - aggravationTime) < suspicionTime)
            {
                SetNPCState(NPCState.suspicious);
            }
            else if ((timeSinceLastSawPlayer - aggravationTime) > suspicionTime)
            {
                SetNPCState(NPCState.idle);
            }

            // Reset player target if already aggravated (avoid loss of target)
            if (GetNPCState() == NPCState.aggravated && !npcMover.HasMoveTarget())
            {
                npcMover.SetMoveTarget(playerController.value.gameObject);
            }

            timeSinceLastSawPlayer += Time.deltaTime;
        }

        private void HandlePlayerStateChange(PlayerState playerState)
        {
            if (playerState == PlayerState.inTransition)
            {
                TransitionType transitionType = playerStateHandler.value.GetTransitionType();
                if (transitionType == TransitionType.Zone)
                {
                    SetNPCState(NPCState.occupied);
                }
                else if (transitionType == TransitionType.BattleComplete)
                {
                    SetNPCState(NPCState.idle);
                    SetNPCState(NPCState.occupied);
                    OverrideCollisionToEnterCombat(false);
                }
                // Non-zone & battle-end transitions, allow enemy movement -- swarm mechanic
            }
            else if (playerState == PlayerState.inWorld)
            {
                npcOccupied = false;
            }
            else
            {
                SetNPCState(NPCState.occupied);
            }
        }

        private void AdjustReactionForState(bool shoutOnAggravation)
        {
            if (GetNPCState() == NPCState.aggravated)
            {
                if (shoutOnAggravation && shoutDistance > 0f)
                {
                    ShoutToNearbyNPCs();
                }

                npcMover.SetMoveTarget(playerController.value.gameObject);
            }
            else if (GetNPCState() == NPCState.suspicious)
            {
                npcMover.ClearMoveTargets();
            }
            else if (GetNPCState() == NPCState.idle)
            {
                npcMover.MoveToOriginalPosition();
            }
        }

        private TransitionType GetBattleEntryType(Collision2D collision)
        {
            Vector2 contactPoint = collision.GetContact(0).point;
            Vector2 npcPosition = collision.otherCollider.bounds.center;
            Vector2 playerPosition = collision.collider.bounds.center;

            float npcLookMagnitudeToContact = Vector2.Dot(contactPoint - npcPosition, npcMover.GetLookDirection());
            float playerLookMagnitudeToContact = Vector2.Dot(contactPoint - playerPosition, playerController.value.GetPlayerMover().GetLookDirection());

            if (playerLookMagnitudeToContact > 0 && npcLookMagnitudeToContact < 0)
            {
                return TransitionType.BattleGood;
            }
            else if (npcLookMagnitudeToContact > 0 && playerLookMagnitudeToContact < 0)
            {
                return TransitionType.BattleBad;
            }
            else
            {
                return TransitionType.BattleNeutral;
            }
        }
        #endregion

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, chaseDistance);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, shoutDistance);
        }
#endif
    }
}
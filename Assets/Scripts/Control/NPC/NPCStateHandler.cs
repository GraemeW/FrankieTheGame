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
        [SerializeField] LayerMask playerCollisionMask = new LayerMask();
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
        [SerializeField] bool willDestroySelfOnDeath = true;
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

        bool queueDeathOnNextPlayerStateChange = false;

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
            // Hard requirement
            npcMover = GetComponent<NPCMover>();
            // Not strictly necessary -- will fail elegantly
            baseStats = GetComponent<BaseStats>();
            combatParticipant = GetComponent<CombatParticipant>();

            // Cached
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
            return player?.GetComponent<PlayerStateHandler>();
        }

        private PlayerController SetupPlayerController()
        {
            if (player == null) { player = GameObject.FindGameObjectWithTag("Player"); }
            return player?.GetComponent<PlayerController>();
        }

        private void OnEnable()
        {
            playerStateHandler.value.playerStateChanged += HandlePlayerStateChange;
            if (combatParticipant != null) { combatParticipant.stateAltered += HandleNPCCombatStateChange; }
            ResetNPCState();
        }

        private void OnDisable()
        {
            playerStateHandler.value.playerStateChanged -= HandlePlayerStateChange;
            if (combatParticipant != null) { combatParticipant.stateAltered -= HandleNPCCombatStateChange; }
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
            if (disableCollisionEventsWhenDead && (combatParticipant != null && combatParticipant.IsDead())) { return; }
            if (disableCollisionEventsWhenIdle && GetNPCState() == NPCState.idle) { return; }

            Vector2 contactPoint = collision.GetContact(0).point;
            Vector2 npcPosition = collision.otherCollider.bounds.center;
            Vector2 playerPosition = collision.collider.bounds.center;

            HandleAllCollisionEntries(collision.gameObject, contactPoint, npcPosition, playerPosition);
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (disableCollisionEventsWhenDead && (combatParticipant != null && combatParticipant.IsDead())) { return; }
            if (disableCollisionEventsWhenIdle && GetNPCState() == NPCState.idle) { return; }

            Vector2 npcPosition = GetComponent<Collider2D>().bounds.center;
            Vector2 contactPoint = collision.ClosestPoint(npcPosition);
            Vector2 playerPosition = collision.GetComponent<Collider2D>().bounds.center;

            HandleAllCollisionEntries(collision.gameObject, contactPoint, npcPosition, playerPosition);
        }

        private void HandleAllCollisionEntries(GameObject collisionGameObject, Vector2 contactPoint, Vector2 npcPosition, Vector2 playerPosition)
        {
            if (playerCollisionMask == (playerCollisionMask | (1 << collisionGameObject.layer)))
            {
                if (HandlePlayerCollisions(contactPoint, npcPosition, playerPosition)) { return; }
            }

            collisionGameObject.TryGetComponent(out NPCStateHandler collisionNPC);
            if (collisionNPC != null)
            {
                if (HandleNPCCollisions(collisionNPC, contactPoint, npcPosition, playerPosition)) { return; }
            }
        }

        private void OnCollisionExit2D(Collision2D collision)
        {
            HandleAllCollisionExits(collision.gameObject);
        }

        private void OnTriggerExit2D(Collider2D collision)
        {
            HandleAllCollisionExits(collision.gameObject);
        }

        private void HandleAllCollisionExits(GameObject collisionGameObject)
        {
            if (collisionGameObject.CompareTag("Player")) { touchingPlayer = false; }

            collisionGameObject.TryGetComponent(out NPCStateHandler collisionNPC);
            if (collisionNPC != null)
            {
                currentNPCCollisions.Remove(collisionNPC);
            }
        }

        private bool HandlePlayerCollisions(Vector2 contactPoint, Vector2 npcPosition, Vector2 playerPosition)
        {
            touchingPlayer = true;

            if (collisionsOverriddenToEnterCombat) // Applied for aggro situations
            {
                TransitionType battleEntryType = GetBattleEntryType(contactPoint, npcPosition, playerPosition);
                InitiateCombat(playerStateHandler.value, battleEntryType);
                return true;
            }
            else if (collidedWithPlayer != null) // Event hooked up in Unity
            {
                TransitionType battleEntryType = GetBattleEntryType(contactPoint, npcPosition, playerPosition);
                collidedWithPlayer.Invoke(playerStateHandler.value, battleEntryType);
                return true;
            }
            return false;
        }

        private bool HandleNPCCollisions(NPCStateHandler collisionNPC, TransitionType battleEntryType)
        {
            if (!currentNPCCollisions.Contains(collisionNPC) && collisionNPC != this) { currentNPCCollisions.Add(collisionNPC); }
            if (!collisionsOverriddenToEnterCombat || GetNPCState() == NPCState.occupied) { return false; }

            if (touchingPlayer || collisionNPC.IsNPCGraphTouchingPlayer())
            {
                InitiateCombat(playerStateHandler.value, battleEntryType);
                return true;
            }
            return false;
        }

        private bool HandleNPCCollisions(NPCStateHandler collisionNPC, Vector2 contactPoint, Vector2 npcPosition, Vector2 playerPosition)
        {
            TransitionType battleEntryType = GetBattleEntryType(contactPoint, npcPosition, playerPosition);
            return HandleNPCCollisions(collisionNPC, battleEntryType);
        }
        #endregion

        #region PublicMethods
        public string GetName()
        {
            // Split apart name on lower case followed by upper case w/ or w/out underscores
            return (baseStats != null) ? baseStats.GetCharacterProperties().GetCharacterNamePretty() : defaultName;
        }

        public NPCState GetNPCState()
        {
            if (npcOccupied) { return NPCState.occupied; }
            return npcState;
        }

        public CombatParticipant GetCombatParticipant()
        {
            return combatParticipant;
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

        public bool IsTouchingPlayer()
        {
            return touchingPlayer;
        }

        public bool IsNPCGraphTouchingPlayer()
        {
            if (touchingPlayer) { return true; } // short circuit on simple condition

            List<NPCStateHandler> npcCollisionGraph = new List<NPCStateHandler>();
            GetNPCCollisionGraph(ref npcCollisionGraph);

            return npcCollisionGraph.Any(x => x.IsTouchingPlayer());
        }

        private void GetNPCCollisionGraph(ref List<NPCStateHandler> npcCollisionGraph)
        {
            foreach (NPCStateHandler npcInContact in currentNPCCollisions)
            {
                if (!npcCollisionGraph.Contains(npcInContact))
                {
                    npcCollisionGraph.Add(npcInContact);
                    npcInContact.GetNPCCollisionGraph(ref npcCollisionGraph);
                }
            }
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
            if (combatParticipant == null) { return; }
            if (playerStateHandler.GetPlayerState() == PlayerState.inBattle) { return; }
            if (GetNPCState() == NPCState.occupied) { return; }

            if (combatParticipant.IsDead())
            {
                playerStateHandler.EnterDialogue(string.Format(messageCannotFight, combatParticipant.GetCombatName()));
                SetNPCState(NPCState.idle);
            }
            else
            {
                List<CombatParticipant> enemies = new List<CombatParticipant>();
                enemies.Add(combatParticipant);
                foreach (NPCStateHandler npcInContact in currentNPCCollisions)
                {
                    enemies.Add(npcInContact.GetCombatParticipant());
                }

                bool enteredCombat = playerStateHandler.EnterCombat(enemies, transitionType);
                if (enteredCombat) { SetNPCState(NPCState.occupied); }
                else { SetNPCState(NPCState.idle); }
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
                GameObject moveTarget = playerController.value?.gameObject;
                if (moveTarget != null) { npcMover.SetMoveTarget(playerController.value.gameObject); }   
            }

            timeSinceLastSawPlayer += Time.deltaTime;
        }

        private void HandlePlayerStateChange(PlayerState playerState)
        {
            if (queueDeathOnNextPlayerStateChange)
            {
                Destroy(gameObject);
            }

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

        private TransitionType GetBattleEntryType(Vector2 contactPoint, Vector2 npcPosition, Vector2 playerPosition)
        {
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

        private void ShoutToNearbyNPCs()
        {
            if (combatParticipant != null && combatParticipant.IsDead()) { return; }

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

        private void HandleNPCCombatStateChange(CombatParticipant combatParticipant, StateAlteredData stateAlteredData)
        {
            if (stateAlteredData.stateAlteredType == StateAlteredType.Dead && willDestroySelfOnDeath)
            {
                queueDeathOnNextPlayerStateChange = true;
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
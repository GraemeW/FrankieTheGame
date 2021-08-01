using System.Collections.Generic;
using UnityEngine;
using Frankie.ZoneManagement;
using Frankie.Stats;
using Frankie.Combat;
using UnityEngine.Events;
using System;
using Frankie.Speech;
using Frankie.Utils;

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
        [Tooltip("Must be true to be shouted at, regardless of group")][SerializeField] bool canBeShoutedAt = true;
        [Tooltip("From interaction center point of NPC")][SerializeField] float shoutDistance = 2.0f;
        [Tooltip("If this is set, will ONLY aggro those in list")][SerializeField] List<NPCStateHandler> shoutGroup = new List<NPCStateHandler>();

        // State
        [SerializeField] NPCState npcState = NPCState.idle;
        [SerializeField] bool npcOccupied = false;
        float timeSinceLastSawPlayer = Mathf.Infinity;
        bool currentChasePlayerDisposition = false;
        bool collisionsOverriddenToEnterCombat = false;

        // Cached References
        BaseStats baseStats = null;
        NPCMover npcMover = null;
        CombatParticipant combatParticipant = null;
        PlayerStateHandler playerStateHandler = null;
        PlayerController playerController = null;

        // Events
        public CollisionEvent collidedWithPlayer;

        // Data Structures
        [System.Serializable]
        public class CollisionEvent : UnityEvent<PlayerStateHandler, TransitionType>
        {
        }

        private void Awake()
        {
            baseStats = GetComponent<BaseStats>();
            npcMover = GetComponent<NPCMover>();
            combatParticipant = GetComponent<CombatParticipant>();

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            playerStateHandler = player.GetComponent<PlayerStateHandler>();
            playerController = player.GetComponent<PlayerController>();
        }

        private void OnEnable()
        {
            playerStateHandler.playerStateChanged += HandlePlayerStateChange;
            ResetNPCState();

        }

        private void OnDisable()
        {
            playerStateHandler.playerStateChanged -= HandlePlayerStateChange;
        }

        private void ResetNPCState()
        {
            SetNPCState(NPCState.idle);
            timeSinceLastSawPlayer = Mathf.Infinity;
            currentChasePlayerDisposition = willChasePlayer;
        }

        private void Update()
        {
            if (!currentChasePlayerDisposition || GetNPCState() == NPCState.occupied) { return; }

            CheckForPlayerProximity();
            timeSinceLastSawPlayer += Time.deltaTime;
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (!collision.gameObject.CompareTag("Player")) { return; }
            if (disableCollisionEventsWhenDead && combatParticipant.IsDead()) { return; }
            if (disableCollisionEventsWhenIdle && GetNPCState() == NPCState.idle) { return; }

            if (collisionsOverriddenToEnterCombat)
            {
                TransitionType battleEntryType = GetBattleEntryType(collision);
                InitiateCombat(playerStateHandler, battleEntryType);
            }
            else if (collidedWithPlayer != null)
            {
                TransitionType battleEntryType = GetBattleEntryType(collision);
                collidedWithPlayer.Invoke(playerStateHandler, battleEntryType);
            }
        }

        private TransitionType GetBattleEntryType(Collision2D collision)
        {
            Vector2 contactPoint = collision.GetContact(0).point;
            Vector2 npcPosition = collision.otherCollider.bounds.center;
            Vector2 playerPosition = collision.collider.bounds.center; 

            float npcLookMagnitudeToContact = Vector2.Dot(contactPoint - npcPosition, npcMover.GetLookDirection());
            float playerLookMagnitudeToContact = Vector2.Dot(contactPoint - playerPosition, playerController.GetPlayerMover().GetLookDirection());

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

        private void HandlePlayerStateChange(PlayerState playerState)
        {
            if (playerState == PlayerState.inTransition)
            {
                TransitionType transitionType = playerStateHandler.GetTransitionType();
                if (transitionType == TransitionType.Zone)
                {
                    SetNPCState(NPCState.occupied);
                }
                else if (transitionType == TransitionType.BattleComplete)
                {
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

        public void SetNPCState(NPCState npcState, bool shoutOnAggravation = true)
        {
            if (GetNPCState() == npcState) { return; }

            // Occupied treated as a pseudo-state to allow for state persistence
            if (npcState == NPCState.occupied) { npcOccupied = true; }
            else
            {
                npcOccupied = false;
                this.npcState = npcState;
            }

            if (GetNPCState() == NPCState.aggravated)
            {
                if (shoutOnAggravation && shoutDistance > 0f)
                {
                    ShoutToNearbyNPCs();
                }

                npcMover.SetMoveTarget(playerController.gameObject);
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

        public void OverrideCollisionToEnterCombat(bool enable)
        {
            collisionsOverriddenToEnterCombat = enable;
        }

        private void ShoutToNearbyNPCs()
        {
            RaycastHit2D[] hits = npcMover.NPCCastFromSelf(shoutDistance);
            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider.gameObject.TryGetComponent(out NPCStateHandler npcInRange))
                {
                    if (!npcInRange.IsShoutable()) { continue; }
                    if (shoutGroup.Count == 0 || shoutGroup.Contains(npcInRange))
                    {
                        npcInRange.SetChasePlayerDisposition(true);
                        npcInRange.SetNPCState(NPCState.aggravated, false); // Do not chain shouts (shout on aggravation set to false)
                        npcInRange.OverrideCollisionToEnterCombat(true);
                    }
                }
            }
        }

        private void CheckForPlayerProximity()
        {
            if (SmartVector2.CheckDistance(npcMover.GetInteractionPosition(), playerController.GetInteractionPosition(), chaseDistance))
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
                npcMover.SetMoveTarget(playerController.gameObject);
            }

            timeSinceLastSawPlayer += Time.deltaTime;
        }

        public void SetChasePlayerDisposition(bool enable) // Called via Unity Events
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
                playerStateHandler.OpenSimpleDialogue(string.Format(messageCannotFight, enemy.GetCombatName()));
            }
            else
            {
                List<CombatParticipant> enemies = new List<CombatParticipant>();
                enemies.Add(enemy);
                playerStateHandler.EnterCombat(enemies, transitionType);
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
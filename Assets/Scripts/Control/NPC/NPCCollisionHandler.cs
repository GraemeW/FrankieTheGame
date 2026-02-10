using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using Frankie.Combat;
using Frankie.ZoneManagement;

namespace Frankie.Control
{
    [RequireComponent(typeof(NPCStateHandler))]
    [RequireComponent(typeof(NPCMover))]
    public class NPCCollisionHandler : MonoBehaviour
    {
        //Tunables
        [SerializeField] private LayerMask playerCollisionMask;
        [SerializeField] private bool defaultCollisionsWhenAggravated = true;
        [SerializeField] private bool disableCollisionEventsWhenDead = true;

        // State
        private bool collisionsActive = true;
        private bool collisionsOverriddenToEnterCombat = false;
        private bool touchingPlayer = false;
        private readonly List<NPCCollisionHandler> currentNPCMob = new();

        // Cached References
        private CombatParticipant combatParticipant;
        private NPCStateHandler npcStateHandler;
        private NPCMover npcMover;

        // Events
        public CollisionEvent collidedWithPlayer;

        // Data Structures
        [System.Serializable]
        public class CollisionEvent : UnityEvent<TransitionType>
        {
        }

        #region UnityMethodsInitialization
        private void Awake()
        {
            // Hard requirement
            npcStateHandler = GetComponent<NPCStateHandler>();
            collisionsOverriddenToEnterCombat = npcStateHandler.WillForceCombat();
            npcMover = GetComponent<NPCMover>();
            // Not strictly necessary -- will fail elegantly
            combatParticipant = GetComponent<CombatParticipant>();
        }

        private void OnEnable()
        {
            npcStateHandler.npcStateChanged += HandleNPCStateChange;
            if (combatParticipant != null) { combatParticipant.SubscribeToStateUpdates(HandleNPCCombatStateChange); }
        }

        private void OnDisable()
        {
            npcStateHandler.npcStateChanged -= HandleNPCStateChange;
            if (combatParticipant != null) { combatParticipant.UnsubscribeToStateUpdates(HandleNPCCombatStateChange); }
        }

        private void HandleNPCStateChange(NPCStateType npcStateType, bool isNPCAfraid)
        {
            if (disableCollisionEventsWhenDead &&
                combatParticipant != null && combatParticipant.IsDead()) { collisionsActive = false; return; } // NPC death supersedes collision behavior

            switch (npcStateType)
            {
                case NPCStateType.Aggravated:
                    collisionsActive = defaultCollisionsWhenAggravated;
                    break;
                case NPCStateType.Frenzied:
                    collisionsOverriddenToEnterCombat = true;
                    collisionsActive = true;
                    break;
                case NPCStateType.Suspicious:
                case NPCStateType.Idle:
                case NPCStateType.Occupied:
                default:
                    collisionsActive = collisionsOverriddenToEnterCombat;
                    break;
            }
        }

        private void HandleNPCCombatStateChange(StateAlteredInfo stateAlteredInfo)
        {
            if (disableCollisionEventsWhenDead && stateAlteredInfo.stateAlteredType == StateAlteredType.Dead)
            {
                collisionsActive = false;
            }
        }
        #endregion

        #region UnityMethodsCollisions
        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (!collisionsActive) { return; }
            
            Vector2 npcPosition = collision.otherCollider.bounds.center;
            Vector2 playerPosition = collision.collider.bounds.center;

            HandleAllCollisionEntries(collision.gameObject, npcPosition, playerPosition);
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (!collisionsActive) { return; }

            Vector2 npcPosition = GetComponent<Collider2D>().bounds.center;
            Vector2 playerPosition = collision.GetComponent<Collider2D>().bounds.center;

            HandleAllCollisionEntries(collision.gameObject, npcPosition, playerPosition);
        }

        private void HandleAllCollisionEntries(GameObject collisionGameObject, Vector2 npcPosition, Vector2 playerPosition)
        {
            if (playerCollisionMask == (playerCollisionMask | (1 << collisionGameObject.layer)))
            {
                var playerMover = collisionGameObject.GetComponentInParent<PlayerMover>();
                if (playerMover != null && HandlePlayerCollisions(playerMover, npcPosition, playerPosition)) { return; }
            }

            if (!collisionGameObject.TryGetComponent(out NPCCollisionHandler collisionNPC)) { return; }
            if (HandleNPCCollisions(collisionNPC)) { return; }
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

            if (collisionGameObject.TryGetComponent(out NPCCollisionHandler collisionNPC))
            {
                RemoveNPCMob(collisionNPC);
            }
        }

        private bool HandlePlayerCollisions(PlayerMover playerMover, Vector2 npcPosition, Vector2 playerPosition)
        {
            touchingPlayer = true;

            TransitionType battleEntryType;
            // Applied for aggro situations
            if (collisionsOverriddenToEnterCombat) 
            {
                battleEntryType = GetBattleEntryType(playerMover, playerPosition, npcPosition);
                npcStateHandler.InitiateCombat(battleEntryType, GetNPCMob());
                return true;
            }
            
            // Event hooked up in Unity
            if (collidedWithPlayer == null) { return false; }
            battleEntryType = GetBattleEntryType(playerMover, playerPosition, npcPosition);
            collidedWithPlayer.Invoke(battleEntryType);
            return true;
        }

        private bool HandleNPCCollisions(NPCCollisionHandler collisionNPC)
        {
            AddNPCMob(collisionNPC);
            if (!collisionsOverriddenToEnterCombat) { return false; }

            var npcCollisionHandler = collisionNPC.GetComponent<NPCCollisionHandler>();
            if (!touchingPlayer && !npcCollisionHandler.IsNPCGraphTouchingPlayer()) { return false; }
            
            // If graph touching player, player is in transition -> pass Neutral transition since irrelevant (save on maths)
            npcStateHandler.InitiateCombat(TransitionType.BattleNeutral, GetNPCMob());
            return true;
        }
        #endregion

        #region PublicMethods
        public NPCStateHandler GetNPCStateHandler() => npcStateHandler;
        public bool IsTouchingPlayer() => touchingPlayer;
        public void SetCollisionsActive(bool enable) => collisionsActive = enable;

        public List<NPCStateHandler> GetNPCMob()
        {
            var npcCollisionGraph = new List<NPCCollisionHandler>();
            GetNPCCollisionGraph(ref npcCollisionGraph);
            return npcCollisionGraph.Select(npcCollisionHandler => npcCollisionHandler.GetNPCStateHandler()).ToList();
        }

        public bool IsNPCGraphTouchingPlayer()
        {
            if (touchingPlayer) { return true; } // short circuit on simple condition

            var npcCollisionGraph = new List<NPCCollisionHandler>();
            GetNPCCollisionGraph(ref npcCollisionGraph);

            return npcCollisionGraph.Any(x => x.IsTouchingPlayer());
        }
        #endregion

        #region PrivateMethods
        private void GetNPCCollisionGraph(ref List<NPCCollisionHandler> npcCollisionGraph)
        {
            foreach (NPCCollisionHandler npcInContact in currentNPCMob)
            {
                if (npcCollisionGraph.Contains(npcInContact)) { continue; }
                
                npcCollisionGraph.Add(npcInContact);
                npcInContact.GetNPCCollisionGraph(ref npcCollisionGraph);
            }
        }

        protected void AddNPCMob(NPCCollisionHandler npcCollisionHandler, bool triggerBilateral = true)
        {
            if (npcCollisionHandler == null) { return; }
            if (npcCollisionHandler == this) { return; }

            if (!currentNPCMob.Contains(npcCollisionHandler)) { currentNPCMob.Add(npcCollisionHandler); }
            
            // Bilateral first hit as triggers sometimes not occurring both ways
            if (triggerBilateral) { npcCollisionHandler.AddNPCMob(this, false); }
        }

        protected void RemoveNPCMob(NPCCollisionHandler npcCollisionHandler, bool triggerBilateral = true)
        {
            if (npcCollisionHandler == null) { return; }
            if (npcCollisionHandler == this) { return; }

            if (currentNPCMob.Contains(npcCollisionHandler)) { currentNPCMob.Remove(npcCollisionHandler); }

            // Bilateral first hit as triggers sometimes not occurring both ways
            if (triggerBilateral) { npcCollisionHandler.RemoveNPCMob(this, false); }
        }

        private TransitionType GetBattleEntryType(PlayerMover playerMover, Vector2 playerPosition, Vector2 npcPosition)
        {
            float npcLookMagnitudeToContact = Vector2.Dot(playerPosition - npcPosition, npcMover.GetLookDirection());
            float playerLookMagnitudeToContact = Vector2.Dot(npcPosition - playerPosition, playerMover.GetLookDirection());

            if (playerLookMagnitudeToContact > 0 && npcLookMagnitudeToContact < 0)
            {
                return TransitionType.BattleGood;
            }
            if (npcLookMagnitudeToContact > 0 && playerLookMagnitudeToContact < 0)
            {
                return TransitionType.BattleBad;
            }

            return TransitionType.BattleNeutral;
        }
        #endregion
    }
}

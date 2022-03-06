using Frankie.Combat;
using Frankie.ZoneManagement;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace Frankie.Control
{
    [RequireComponent(typeof(NPCStateHandler))]
    [RequireComponent(typeof(NPCMover))]
    public class NPCCollisionHandler : MonoBehaviour
    {
        //Tunables
        [SerializeField] LayerMask playerCollisionMask = new LayerMask();
        [SerializeField] bool defaultCollisionsWhenAggravated = true;
        [SerializeField] bool disableCollisionEventsWhenDead = true;
        [SerializeField] bool collisionsOverriddenToEnterCombat = false;

        // State
        bool collisionsActive = true;
        bool touchingPlayer = false;
        List<NPCCollisionHandler> currentNPCMob = new List<NPCCollisionHandler>();

        // Cached References
        CombatParticipant combatParticipant = null;
        NPCStateHandler npcStateHandler = null;
        NPCMover npcMover = null;

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
            npcMover = GetComponent<NPCMover>();
            // Not strictly necessary -- will fail elegantly
            combatParticipant = GetComponent<CombatParticipant>();
        }

        private void OnEnable()
        {
            npcStateHandler.npcStateChanged += HandleNPCStateChange;
            if (combatParticipant != null) { combatParticipant.stateAltered += HandleNPCCombatStateChange; }
        }

        private void OnDisable()
        {
            npcStateHandler.npcStateChanged -= HandleNPCStateChange;
            if (combatParticipant != null) { combatParticipant.stateAltered -= HandleNPCCombatStateChange; }
        }

        private void HandleNPCStateChange(NPCStateType npcStateType)
        {
            if (disableCollisionEventsWhenDead && 
                combatParticipant != null && combatParticipant.IsDead()) { collisionsActive = false;  return; } // NPC death supercedes collision behavior

            switch (npcStateType)
            {
                case NPCStateType.aggravated:
                    collisionsActive = defaultCollisionsWhenAggravated;
                    break;
                case NPCStateType.frenzied:
                    collisionsOverriddenToEnterCombat = true;
                    collisionsActive = true;
                    break;
                case NPCStateType.suspicious:
                case NPCStateType.idle:
                case NPCStateType.occupied:
                default:
                    collisionsActive = false;
                    break;
            }
        }

        private void HandleNPCCombatStateChange(CombatParticipant combatParticipant, StateAlteredData stateAlteredData)
        {
            if (disableCollisionEventsWhenDead && stateAlteredData.stateAlteredType == StateAlteredType.Dead)
            {
                collisionsActive = false;
            }
        }
        #endregion

        #region UnityMethodsCollisions
        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (!collisionsActive) { return; }

            Vector2 contactPoint = collision.GetContact(0).point;
            Vector2 npcPosition = collision.otherCollider.bounds.center;
            Vector2 playerPosition = collision.collider.bounds.center;

            HandleAllCollisionEntries(collision.gameObject, contactPoint, npcPosition, playerPosition);
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (!collisionsActive) { return; }

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

            if (collisionGameObject.TryGetComponent(out NPCCollisionHandler collisionNPC))
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

            if (collisionGameObject.TryGetComponent(out NPCCollisionHandler collisionNPC))
            {
                RemoveNPCMob(collisionNPC);
            }
        }

        private bool HandlePlayerCollisions(Vector2 contactPoint, Vector2 npcPosition, Vector2 playerPosition)
        {
            touchingPlayer = true;

            if (collisionsOverriddenToEnterCombat) // Applied for aggro situations
            {
                TransitionType battleEntryType = GetBattleEntryType(contactPoint, npcPosition, playerPosition);
                npcStateHandler.InitiateCombat(battleEntryType, GetNPCMob());
                return true;
            }
            else if (collidedWithPlayer != null) // Event hooked up in Unity
            {
                TransitionType battleEntryType = GetBattleEntryType(contactPoint, npcPosition, playerPosition);
                collidedWithPlayer.Invoke(battleEntryType);
                return true;
            }
            return false;
        }

        private bool HandleNPCCollisions(NPCCollisionHandler collisionNPC, TransitionType battleEntryType)
        {
            AddNPCMob(collisionNPC);
            if (!collisionsOverriddenToEnterCombat) { return false; }

            NPCCollisionHandler npcCollisionHandler = collisionNPC.GetComponent<NPCCollisionHandler>();
            if (touchingPlayer || npcCollisionHandler.IsNPCGraphTouchingPlayer())
            {
                npcStateHandler.InitiateCombat(battleEntryType);
                return true;
            }
            return false;
        }

        private bool HandleNPCCollisions(NPCCollisionHandler collisionNPC, Vector2 contactPoint, Vector2 npcPosition, Vector2 playerPosition)
        {
            TransitionType battleEntryType = GetBattleEntryType(contactPoint, npcPosition, playerPosition);
            return HandleNPCCollisions(collisionNPC, battleEntryType);
        }
        #endregion

        #region PublicMethods
        public void SetCollisionsActive(bool enable)
        {
            collisionsActive = enable;
        }

        public List<NPCStateHandler> GetNPCMob()
        {
            List<NPCStateHandler> translatedNPCMob = new List<NPCStateHandler>();
            foreach (NPCCollisionHandler npcCollisionHandler in currentNPCMob)
            {
                translatedNPCMob.Add(npcCollisionHandler.GetNPCStateHandler());
            }
            return translatedNPCMob;
        }

        public NPCStateHandler GetNPCStateHandler()
        {
            return npcStateHandler;
        }


        public bool IsTouchingPlayer()
        {
            return touchingPlayer;
        }

        public bool IsNPCGraphTouchingPlayer()
        {
            if (touchingPlayer) { return true; } // short circuit on simple condition

            List<NPCCollisionHandler> npcCollisionGraph = new List<NPCCollisionHandler>();
            GetNPCCollisionGraph(ref npcCollisionGraph);

            return npcCollisionGraph.Any(x => x.IsTouchingPlayer());
        }

        private void GetNPCCollisionGraph(ref List<NPCCollisionHandler> npcCollisionGraph)
        {
            foreach (NPCCollisionHandler npcInContact in currentNPCMob)
            {
                if (!npcCollisionGraph.Contains(npcInContact))
                {
                    npcCollisionGraph.Add(npcInContact);
                    npcInContact.GetNPCCollisionGraph(ref npcCollisionGraph);
                }
            }
        }
        #endregion

        #region PrivateMethods
        private void AddNPCMob(NPCCollisionHandler npcStateHandler)
        {
            if (npcStateHandler == null) { return; }
            if (npcStateHandler == this) { return; }

            currentNPCMob.Add(npcStateHandler);
        }

        private void RemoveNPCMob(NPCCollisionHandler npcStateHandler)
        {
            if (currentNPCMob.Contains(npcStateHandler))
            {
                currentNPCMob.Remove(npcStateHandler);
            }
        }

        private TransitionType GetBattleEntryType(Vector2 contactPoint, Vector2 npcPosition, Vector2 playerPosition)
        {
            float npcLookMagnitudeToContact = Vector2.Dot(contactPoint - npcPosition, npcMover.GetLookDirection());
            float playerLookMagnitudeToContact = Vector2.Dot(contactPoint - playerPosition, npcStateHandler.GetPlayerLookDirection());

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
    }
}
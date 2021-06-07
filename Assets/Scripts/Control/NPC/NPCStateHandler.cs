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
        [SerializeField] float chaseDistance = 3.0f;
        [SerializeField] float aggravationTime = 3.0f;
        [SerializeField] float suspicionTime = 3.0f;
        [SerializeField] float shoutDistance = 2.0f;

        // State
        NPCState npcState = NPCState.idle;
        float timeSinceLastSawPlayer = Mathf.Infinity;
        bool currentChasePlayerDisposition = false;

        // Cached References
        BaseStats baseStats = null;
        NPCMover npcMover = null;
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

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            playerStateHandler = player.GetComponent<PlayerStateHandler>();
            playerController = player.GetComponent<PlayerController>();
            currentChasePlayerDisposition = willChasePlayer;
        }

        private void OnEnable()
        {
            playerStateHandler.playerStateChanged += HandlePlayerStateChange;
        }

        private void OnDisable()
        {
            playerStateHandler.playerStateChanged -= HandlePlayerStateChange;
        }

        private void Update()
        {
            if (!currentChasePlayerDisposition || npcState == NPCState.occupied) { return; }

            CheckForPlayerProximity();
            timeSinceLastSawPlayer += Time.deltaTime;
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (!collision.gameObject.CompareTag("Player")) { return; }

            if (collidedWithPlayer != null)
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
            if (playerState == PlayerState.inWorld)
            {
                SetNPCState(NPCState.idle);
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
            return npcState;
        }

        public void SetNPCState(NPCState npcState)
        {
            if (this.npcState == npcState) { return; }

            this.npcState = npcState;
            if (npcState == NPCState.aggravated)
            {
                npcMover.SetMoveTarget(playerController.gameObject);
            }
            else if (npcState == NPCState.suspicious)
            {
                npcMover.ClearMoveTargets();
            }
            else if (npcState == NPCState.idle)
            {
                npcMover.MoveToOriginalPosition();
            }
        }

        public void SetChasePlayerDisposition(bool enable)
        {
            currentChasePlayerDisposition = enable;
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

            timeSinceLastSawPlayer += Time.deltaTime;
        }

        public void InitiateCombat(PlayerStateHandler playerStateHandler)  // called via Unity Event
        {
            if (playerStateHandler.GetPlayerState() != PlayerState.inWorld) { return; }

            InitiateCombat(playerStateHandler, TransitionType.BattleNeutral);
        }

        public void InitiateCombat(PlayerStateHandler playerStateHandler, TransitionType transitionType)  // called via Unity Event
        {
            if (playerStateHandler.GetPlayerState() != PlayerState.inWorld) { return; }

            CombatParticipant enemy = GetComponent<CombatParticipant>();
            if (enemy.IsDead())
            {
                playerStateHandler.OpenSimpleDialogue(string.Format(messageCannotFight, enemy.GetCombatName()));
            }
            else
            {
                List<CombatParticipant> enemies = new List<CombatParticipant>();
                enemies.Add(enemy);
                // TODO:  Implement pile-on / swarm system;
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
        }
#endif
    }
}
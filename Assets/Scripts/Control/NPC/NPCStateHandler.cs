using System.Collections.Generic;
using UnityEngine;
using Frankie.ZoneManagement;
using Frankie.Stats;
using Frankie.Combat;
using UnityEngine.Events;
using System;

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

        // Cached References
        BaseStats baseStats = null;
        NPCMover npcMover = null;
        PlayerStateHandler playerStateHandler = null;
        PlayerController playerController = null;

        // Events
        public InteractionEvent collidedWithPlayer;

        // Data Structures
        [System.Serializable]
        public class InteractionEvent : UnityEvent<PlayerStateHandler>
        {
        }

        private void Awake()
        {
            baseStats = GetComponent<BaseStats>();
            npcMover = GetComponent<NPCMover>();

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            playerStateHandler = player.GetComponent<PlayerStateHandler>();
            playerController = player.GetComponent<PlayerController>();
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
            if (!willChasePlayer || npcState == NPCState.occupied) { return; }

            CheckForPlayerProximity();
            timeSinceLastSawPlayer += Time.deltaTime;
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (collidedWithPlayer == null) { return; }

            CombatParticipant combatParticipant = collision.gameObject.GetComponent<CombatParticipant>();
            if (combatParticipant == null) { return; }

            if (playerStateHandler.GetParty().HasMember(combatParticipant))
            {
                collidedWithPlayer.Invoke(playerStateHandler);
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

        private void CheckForPlayerProximity()
        {
            float distanceToPlayer = Vector2.Distance(npcMover.GetInteractionPosition(), playerController.GetInteractionPosition());
            if (distanceToPlayer < chaseDistance)
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
                // TODO:  Implement battle type / transition types
                playerStateHandler.EnterCombat(enemies, TransitionType.BattleNeutral);
            }
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
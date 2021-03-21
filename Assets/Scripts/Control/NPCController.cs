using System.Collections.Generic;
using UnityEngine;
using Frankie.ZoneManagement;
using Frankie.Stats;
using Frankie.Combat;
using Frankie.Core;

namespace Frankie.Control
{
    public class NPCController : MonoBehaviour
    {
        // Tunables
        [SerializeField] Transform interactionCenterPoint = null;
        [Tooltip("Only used if not found via base stats")] [SerializeField] string defaultName = "";
        [Tooltip("Include {0} for enemy name")] [SerializeField] string messageCannotFight = "{0} is wounded and cannot fight.";

        // Cached References
        Animator animator = null;
        BaseStats baseStats = null;

        // State
        Vector2 lookDirection = new Vector2();
        float currentSpeed = 0;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            baseStats = GetComponent<BaseStats>();
        }

        private void Start()
        {
            lookDirection = Vector2.down;
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

        public void SetLookDirectionToPlayer(PlayerController callingController) // called via Unity Event
        {
            Vector2 lookDirection = callingController.GetInteractionPosition() - (Vector2)interactionCenterPoint.position;
            SetLookDirection(lookDirection);
            UpdateAnimator();
        }

        public void SetLookDirection(Vector2 lookDirection)
        {
            this.lookDirection = lookDirection;
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
                // TODO:  Implement pile-on / swarm system
                playerStateHandler.EnterCombat(enemies, TransitionType.BattleNeutral);
            }
        }

        private void UpdateAnimator()
        {
            animator.SetFloat("Speed", currentSpeed);
            animator.SetFloat("xLook", lookDirection.x);
            animator.SetFloat("yLook", lookDirection.y);
        }
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frankie.Core;
using Frankie.Stats;
using System.Text.RegularExpressions;

namespace Frankie.Control
{
    public class NPCController : MonoBehaviour
    {
        // Tunables
        [SerializeField] Transform interactionCenterPoint = null;
        [Tooltip("Only used if not found via base stats")] [SerializeField] string defaultName = "";

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
                return baseStats.GetCharacterNamePretty();
            }
            return defaultName;
        }

        public void SetLookDirectionToPlayer(PlayerController callingController) // Unity Event
        {
            Vector2 lookDirection = callingController.GetInteractionPosition() - (Vector2)interactionCenterPoint.position;
            SetLookDirection(lookDirection);
            UpdateAnimator();

            // HACK:  Temporary to test combat
            // TODO:  Remove, implement properly
            /*
            List<CombatParticipant> enemies = new List<CombatParticipant>();
            CombatParticipant enemy = GetComponent<CombatParticipant>();
            enemy.ResurrectCharacter(enemy.GetMaxHP());
            foreach (CombatParticipant character in callingController.GetComponent<Party>().GetParty())
            {
                character.ResurrectCharacter(character.GetMaxHP());
            }
            enemies.Add(enemy);
            callingController.EnterCombat(enemies, battleEntryType); */
        }

        public void SetLookDirection(Vector2 lookDirection)
        {
            this.lookDirection = lookDirection;
        }

        private void UpdateAnimator()
        {
            animator.SetFloat("Speed", currentSpeed);
            animator.SetFloat("xLook", lookDirection.x);
            animator.SetFloat("yLook", lookDirection.y);
        }
    }
}
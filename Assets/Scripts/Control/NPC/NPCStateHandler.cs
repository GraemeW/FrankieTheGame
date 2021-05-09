using System.Collections.Generic;
using UnityEngine;
using Frankie.ZoneManagement;
using Frankie.Stats;
using Frankie.Combat;

namespace Frankie.Control
{
    public class NPCStateHandler : MonoBehaviour
    {
        // Tunables
        [Tooltip("Only used if not found via base stats")] [SerializeField] string defaultName = "";
        [Tooltip("Include {0} for enemy name")] [SerializeField] string messageCannotFight = "{0} is wounded and cannot fight.";

        // Cached References
        BaseStats baseStats = null;

        private void Awake()
        {
            baseStats = GetComponent<BaseStats>();
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
    }
}
using Frankie.Stats;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Combat
{
    [CreateAssetMenu(fileName = "New Character Filtering", menuName = "BattleAction/Filters/Character")]
    public class CharacterFilter : FilterStrategy
    {
        [SerializeField] List<CharacterProperties> characterPropertiesToFilter = new List<CharacterProperties>();
        [SerializeField] bool negate = false;

        public override IEnumerable<CombatParticipant> Filter(IEnumerable<CombatParticipant> objectsToFilter)
        {
            if (objectsToFilter == null || characterPropertiesToFilter.Count == 0) { yield break; }

            foreach (CombatParticipant combatParticipant in objectsToFilter)
            {
                CharacterProperties characterProperties = combatParticipant.GetBaseStats()?.GetCharacterProperties();
                if (characterProperties == null) { continue; }

                if (!negate && characterPropertiesToFilter.Contains(characterProperties))
                {
                    yield return combatParticipant;
                }
                else if (negate && !characterPropertiesToFilter.Contains(characterProperties))
                {
                    yield return combatParticipant;
                }
            }
        }
    }
}
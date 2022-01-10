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

        public override IEnumerable<CombatParticipant> Filter(IEnumerable<CombatParticipant> objectsToFilter)
        {
            foreach (CombatParticipant combatParticipant in objectsToFilter)
            {
                CharacterProperties characterProperties = combatParticipant.GetBaseStats()?.GetCharacterProperties();
                if (characterProperties == null) { continue; }

                if (characterPropertiesToFilter.Contains(characterProperties))
                {
                    yield return combatParticipant;
                }
            }
        }
    }
}
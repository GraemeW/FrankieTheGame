using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Frankie.Stats;

namespace Frankie.Combat
{
    [CreateAssetMenu(fileName = "New Character Filtering", menuName = "BattleAction/Filters/Character")]
    public class CharacterFilter : FilterStrategy
    {
        [SerializeField] private List<CharacterProperties> characterPropertiesToFilter = new();
        [SerializeField] private bool negate = false;

        public override IEnumerable<BattleEntity> Filter(IEnumerable<BattleEntity> objectsToFilter)
        {
            if (objectsToFilter == null || characterPropertiesToFilter.Count == 0) { yield break; }

            foreach (BattleEntity battleEntity in objectsToFilter)
            {
                CharacterProperties characterProperties = battleEntity.combatParticipant.GetCharacterProperties();
                if (characterProperties == null) { continue; }
                
                // Check via name comparison for compatibility with addressables system
                bool filteredListContainsCharacter = characterPropertiesToFilter.Any(filterCharacter => CharacterProperties.AreCharacterPropertiesMatched(characterProperties, filterCharacter));
                if ((!negate && filteredListContainsCharacter) || (negate && !filteredListContainsCharacter))
                {
                    yield return battleEntity;
                }
            }
        }
    }
}

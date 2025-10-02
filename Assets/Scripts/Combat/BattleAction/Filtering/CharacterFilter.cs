using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frankie.Stats;

namespace Frankie.Combat
{
    [CreateAssetMenu(fileName = "New Character Filtering", menuName = "BattleAction/Filters/Character")]
    public class CharacterFilter : FilterStrategy
    {
        [SerializeField] List<CharacterProperties> characterPropertiesToFilter = new List<CharacterProperties>();
        [SerializeField] bool negate = false;

        public override IEnumerable<BattleEntity> Filter(IEnumerable<BattleEntity> objectsToFilter)
        {
            if (objectsToFilter == null || characterPropertiesToFilter.Count == 0) { yield break; }

            foreach (BattleEntity battleEntity in objectsToFilter)
            {
                CharacterProperties characterProperties = battleEntity.combatParticipant.GetCharacterProperties();
                if (characterProperties == null) { continue; }

                bool filteredListContainsCharacter = false;
                // Check via name comparison for compatibility with addressables system
                foreach (CharacterProperties filterCharacter in characterPropertiesToFilter)
                {
                    if (filterCharacter.GetCharacterNameID() == characterProperties.GetCharacterNameID())
                    {
                        filteredListContainsCharacter = true;
                    }
                }

                if ((!negate && filteredListContainsCharacter) || (negate && !filteredListContainsCharacter))
                {
                    yield return battleEntity;
                }
            }
        }
    }
}

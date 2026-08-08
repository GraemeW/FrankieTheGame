using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Frankie.Stats;

namespace Frankie.Saving
{
    [CreateAssetMenu(fileName = "CharacterPropertiesRegistry", menuName = "UI/Character Properties Registry", order = 30)]
    public class CharacterPropertiesRegistry : ScriptableObject
    {
        [SerializeField] private List<CharacterProperties> characterProperties = new();

        public List<string> GetCharacterIDs()
        {
            return characterProperties
                .Where(properties => properties != null)
                .Select(properties => properties.GetCharacterID())
                .ToList();
        }
    }
}

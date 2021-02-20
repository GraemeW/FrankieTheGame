using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Frankie.Stats
{
    [CreateAssetMenu(fileName = "New Character", menuName = "Characters/New Character")]
    [System.Serializable]
    public class CharacterProperties : ScriptableObject
    {
        // Properties
        public static string GetStaticCharacterNamePretty(string characterName)
        {
            return Regex.Replace(characterName, "([a-z])_?([A-Z])", "$1 $2");
        }

        public string GetCharacterNamePretty()
        {
            return Regex.Replace(name, "([a-z])_?([A-Z])", "$1 $2");
        }

        // State
        static Dictionary<string, CharacterProperties> characterLookupCache;

        public static CharacterProperties GetCharacterFromName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) { return null; }

            if (characterLookupCache == null)
            {
                characterLookupCache = new Dictionary<string, CharacterProperties>();
                CharacterProperties[] characterList = Resources.LoadAll<CharacterProperties>("");
                foreach (CharacterProperties characterProperties in characterList)
                {
                    if (characterLookupCache.ContainsKey(characterProperties.name))
                    {
                        Debug.LogError(string.Format("Looks like there's a duplicate ID for objects: {0} and {1}", characterLookupCache[characterProperties.name], characterProperties));
                        continue;
                    }

                    characterLookupCache[characterProperties.name] = characterProperties;
                }
            }
            if (name == null || !characterLookupCache.ContainsKey(name)) return null;
            return characterLookupCache[name];
        }
    }
}

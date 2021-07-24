using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Frankie.Stats
{
    [CreateAssetMenu(fileName = "New Character", menuName = "Characters/New Character")]
    public class CharacterProperties : ScriptableObject, ISerializationCallbackReceiver
    {
        // Properties
        public GameObject characterPrefab = null;
        public GameObject characterNPCPrefab = null;

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

        public static CharacterProperties GetCharacterPropertiesFromName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) { return null; }

            GenerateCharacterPropertiesLookupCache();
            if (name == null || !characterLookupCache.ContainsKey(name)) return null;
            return characterLookupCache[name];
        }

        static Dictionary<string, CharacterProperties> GetCharacterPropertiesLookup()
        {
            GenerateCharacterPropertiesLookupCache();
            return characterLookupCache;
        }

        static void GenerateCharacterPropertiesLookupCache()
        {
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
        }

        public GameObject GetCharacterPrefab()
        {
            return characterPrefab;
        }

        public GameObject GetCharacterNPCPrefab()
        {
            return characterNPCPrefab;
        }

        #region Interfaces
        public void OnBeforeSerialize()
        {
#if UNITY_EDITOR
            GenerateCharacterPropertiesLookupCache(); // Force reload of character cache to populate look-ups in editor
#endif
        }

        public void OnAfterDeserialize()
        {
        }
        #endregion
    }
}

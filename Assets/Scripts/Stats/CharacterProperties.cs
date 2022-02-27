using Frankie.Core;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Frankie.Stats
{
    [CreateAssetMenu(fileName = "New Character", menuName = "Characters/New Character")]
    public class CharacterProperties : ScriptableObject, IAddressablesCache
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

        public string GetCharacterNameID()
        {
            return name;
        }

        // State
        static AsyncOperationHandle<IList<CharacterProperties>> addressablesLoadHandle;
        static Dictionary<string, CharacterProperties> characterLookupCache;

        #region AddressablesCaching
        public static CharacterProperties GetCharacterPropertiesFromName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) { return null; }

            BuildCacheIfEmpty();
            if (!characterLookupCache.ContainsKey(name)) return null;
            return characterLookupCache[name];
        }

        public static Dictionary<string, CharacterProperties> GetCharacterPropertiesLookup()
        {
            BuildCacheIfEmpty();
            return characterLookupCache;
        }

        public static void BuildCacheIfEmpty()
        {
            if (characterLookupCache == null)
            {
                BuildCharacterPropertiesCache();
            }
        }

        private static void BuildCharacterPropertiesCache()
        {
            characterLookupCache = new Dictionary<string, CharacterProperties>();
            addressablesLoadHandle = Addressables.LoadAssetsAsync(typeof(CharacterProperties).Name, (CharacterProperties characterProperties) =>
            {
                if (characterLookupCache.ContainsKey(characterProperties.name))
                {
                    Debug.LogError(string.Format("Looks like there's a duplicate ID for objects: {0} and {1}", characterLookupCache[characterProperties.name], characterProperties));
                }

                characterLookupCache[characterProperties.name] = characterProperties;
            }
            );
            addressablesLoadHandle.WaitForCompletion();
        }

        public static void ReleaseCache()
        {
            Addressables.Release(addressablesLoadHandle);
        }
        #endregion

        #region PublicMethods
        public GameObject GetCharacterPrefab()
        {
            return characterPrefab;
        }

        public GameObject GetCharacterNPCPrefab()
        {
            return characterNPCPrefab;
        }
        #endregion
    }
}

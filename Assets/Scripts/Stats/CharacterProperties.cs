using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Frankie.Core;

namespace Frankie.Stats
{
    [CreateAssetMenu(fileName = "New Character", menuName = "Characters/New Character")]
    public class CharacterProperties : ScriptableObject, IAddressablesCache
    {
        // Properties
        public GameObject characterPrefab;
        public GameObject characterNPCPrefab;
        public bool hasProgressionStats = true;

        // State
        private static AsyncOperationHandle<IList<CharacterProperties>> _addressablesLoadHandle;
        private static Dictionary<string, CharacterProperties> _characterLookupCache;

        #region SimpleGetters
        public static string GetStaticCharacterNamePretty(string characterName) => Regex.Replace(characterName, "([a-z])_?([A-Z])", "$1 $2");
        public string GetCharacterNamePretty() => Regex.Replace(name, "([a-z])_?([A-Z])", "$1 $2");
        public string GetCharacterNameID() => name;
        public GameObject GetCharacterPrefab() => characterPrefab;
        public GameObject GetCharacterNPCPrefab() => characterNPCPrefab;
        #endregion
        
        #region AddressablesCaching
        public static CharacterProperties GetCharacterPropertiesFromName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) { return null; }

            BuildCacheIfEmpty();
            return _characterLookupCache.GetValueOrDefault(name);
        }

        public static Dictionary<string, CharacterProperties> GetCharacterPropertiesLookup()
        {
            BuildCacheIfEmpty();
            return _characterLookupCache;
        }

        public static void BuildCacheIfEmpty(bool force = false)
        {
            if (_characterLookupCache == null || force)
            {
                BuildCharacterPropertiesCache();
            }
        }

        private static void BuildCharacterPropertiesCache()
        {
            _characterLookupCache = new Dictionary<string, CharacterProperties>();
            _addressablesLoadHandle = Addressables.LoadAssetsAsync(nameof(CharacterProperties), (CharacterProperties characterProperties) =>
            {
                if (_characterLookupCache.TryGetValue(characterProperties.name, out CharacterProperties matchedProperties))
                {
                    Debug.LogError(string.Format($"Looks like there's a duplicate ID for objects: {matchedProperties} and {characterProperties}"));
                }

                _characterLookupCache[characterProperties.name] = characterProperties;
            }
            );
            _addressablesLoadHandle.WaitForCompletion();
        }

        public static void ReleaseCache()
        {
            Addressables.Release(_addressablesLoadHandle);
        }
        #endregion
    }
}

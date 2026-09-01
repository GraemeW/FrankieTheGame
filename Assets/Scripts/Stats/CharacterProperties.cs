using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Unity.Scripting.LifecycleManagement;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using LowDefMustard.Utils;
using LowDefMustard.Localization;
using Frankie.Saving;

namespace Frankie.Stats
{
    [CreateAssetMenu(fileName = "New Character", menuName = "Characters/New Character", order = 1)]
    public partial class CharacterProperties : ScriptableObject, IAddressablesCache, ILocalizable
    {
        // Properties
        [SerializeField][SimpleLocalizedString(LocalizationTableType.Core, true)] private LocalizedString localizedDisplayName;
        [SerializeField] private GameObject characterPrefab;
        [SerializeField] private GameObject characterNPCPrefab;
        [SerializeField] private bool hasProgressionStats = true;
        [SerializeField] private bool incrementsStatsOnLevelUp = false;
        
        // Const
        private const string _errName = "???";
        
        // Local State
        [HideInInspector][SerializeField] private string cachedName;
        public string iCachedName { get => cachedName; set => cachedName = value; }
        
        // Static State
        [AutoStaticsCleanup] private static readonly Dictionary<string, string> _personalizedNameLookup = new();
        [AutoStaticsCleanup] private static AsyncOperationHandle<IList<CharacterProperties>> _addressablesLoadHandle;
        [AutoStaticsCleanup] private static Dictionary<string, CharacterProperties> _characterLookupCache;

        #region Getters
        public string GetCharacterID() => name;
        // Note:  Using name as ID for simplicity
        // Previously scoped separate GUID for this, found it overkill ++ hindered look-up functionality
        
        private string GetStandardCharacterDisplayName() => localizedDisplayName.GetSafeLocalizedString();
        public GameObject GetCharacterPrefab() => characterPrefab;
        public GameObject GetCharacterNPCPrefab() => characterNPCPrefab;
        public bool HasProgressionStats() => hasProgressionStats;
        public bool ShouldIncrementsStatsOnLevelUp() => incrementsStatsOnLevelUp;
        
        public LocalizationTableType localizationTableType { get; } = LocalizationTableType.Core;
        public List<TableEntryReference> GetLocalizationEntries()
        {
            return new List<TableEntryReference>
            {
                localizedDisplayName.TableEntryReference
            };
        }
        
        public List<(string propertyName, LocalizedString localizedString, bool setToName)> GetPropertyLinkedLocalizationEntries()
        {
            return new List<(string propertyName, LocalizedString localizedString, bool setToName)>
            {
                (nameof(localizedDisplayName), localizedDisplayName, true)
            };
        }
        #endregion
        
        #region StaticMethods
        public static bool AreCharacterPropertiesMatched(CharacterProperties entryA, CharacterProperties entryB)
        {
            if (entryA == null || entryB == null) { return false; }
            return entryA.GetCharacterID() == entryB.GetCharacterID();
        }

        public static string GetCharacterDisplayName(CharacterProperties characterProperties)
        {
            if (characterProperties == null) { return _errName; }
            
            string characterID = characterProperties.GetCharacterID();
            SyncToPersonalizedNameLookup(characterID);
            
            if (_personalizedNameLookup.ContainsKey(characterID) && _personalizedNameLookup[characterID] != null) { return _personalizedNameLookup[characterID]; }
            return characterProperties.GetStandardCharacterDisplayName();
        }

        public static string GetCharacterDisplayName(BaseStats baseStats) => baseStats != null ? GetCharacterDisplayName(baseStats.GetCharacterProperties()) : _errName;

        private static void SyncToPersonalizedNameLookup(string characterID)
        {
            if (PlayerPrefsController.wereCharacterNamesDirtied) { _personalizedNameLookup.Clear(); PlayerPrefsController.wereCharacterNamesDirtied = false; }
            if (_personalizedNameLookup.ContainsKey(characterID)) { return; }
            
            if (PlayerPrefsController.CharacterNameKeyExists(characterID)) { _personalizedNameLookup[characterID] = PlayerPrefsController.GetCharacterName(characterID); }
            else { _personalizedNameLookup[characterID] = null; }
        }
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
                if (_characterLookupCache.TryGetValue(characterProperties.GetCharacterID(), out CharacterProperties matchedProperties))
                {
                    Debug.LogError(string.Format($"Looks like there's a duplicate ID for objects: {matchedProperties} and {characterProperties}"));
                }

                _characterLookupCache[characterProperties.GetCharacterID()] = characterProperties;
            }
            );
            _addressablesLoadHandle.WaitForCompletion();
        }

        public static void ReleaseCache()
        {
            if (_addressablesLoadHandle.IsValid()) { Addressables.Release(_addressablesLoadHandle); }
        }
        #endregion
    }
}

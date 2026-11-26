using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Unity.Scripting.LifecycleManagement;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using LowDefMustard.Core;
using LowDefMustard.Utils;
using LowDefMustard.Localization;
using Frankie.Saving;
using Frankie.Speech;
using Frankie.Utils.Localization;

namespace Frankie.Stats
{
    [CreateAssetMenu(fileName = "New Character", menuName = "Characters/New Character", order = 2)]
    public partial class CharacterProperties : ScriptableObject, IAddressablesCache, ILocalizable, IDialogueVariable
    {
        // Properties
        [SerializeField][SimpleLocalizedString(LocalizationTableType.Core, true)] private LocalizedString localizedDisplayName;
        [SerializeField] private AssetReferenceGameObject characterPrefab; // Loaded on demand (via CharacterPrefabCache)
        [SerializeField] private AssetReferenceGameObject characterNPCPrefab;
        [SerializeField] private bool hasProgressionStats = true;
        [SerializeField] private bool incrementsStatsOnLevelUp = false;
        
        // Const
        private const string _errName = "???";
        
        // Field References
        internal const string characterPrefabField = nameof(characterPrefab);
        internal const string characterNPCPrefabField = nameof(characterNPCPrefab);
        
        // Local State
        [HideInInspector][SerializeField] private string cachedName;
        public string iCachedName { get => cachedName; set => cachedName = value; }
        
        // Static State
        [AutoStaticsCleanup] private static readonly Dictionary<string, string> _personalizedNameLookup = new();
        [AutoStaticsCleanup] private static AddressablesLookupCache<CharacterProperties> _cache = new(LDMLogCategory.Stats, nameof(CharacterProperties), characterProperties => characterProperties.GetCharacterID());

        #region StaticMethods
        public static bool AreCharacterPropertiesMatched(CharacterProperties entryA, CharacterProperties entryB)
        {
            if (entryA == null || entryB == null) { return false; }
            return entryA.GetCharacterID() == entryB.GetCharacterID();
        }
        
        public static string GetCharacterDisplayName(BaseStats baseStats) => baseStats != null ? GetCharacterDisplayName(baseStats.GetCharacterProperties()) : _errName;

        public static string GetCharacterDisplayName(CharacterProperties characterProperties)
        {
            if (characterProperties == null) { return _errName; }
            
            string characterID = characterProperties.GetCharacterID();
            SyncToPersonalizedNameLookup(characterID);
            
            if (_personalizedNameLookup.ContainsKey(characterID) && _personalizedNameLookup[characterID] != null) { return _personalizedNameLookup[characterID]; }
            return characterProperties.GetStandardCharacterDisplayName();
        }

        private static void SyncToPersonalizedNameLookup(string characterID)
        {
            if (PlayerPrefsController.wereCharacterNamesDirtied) { _personalizedNameLookup.Clear(); PlayerPrefsController.wereCharacterNamesDirtied = false; }
            if (_personalizedNameLookup.ContainsKey(characterID)) { return; }
            
            if (PlayerPrefsController.CharacterNameKeyExists(characterID)) { _personalizedNameLookup[characterID] = PlayerPrefsController.GetCharacterName(characterID); }
            else { _personalizedNameLookup[characterID] = null; }
        }
        #endregion
        
        #region Getters
        public string GetCharacterID() => name;
        // Note:  Using name as ID for simplicity
        // Previously scoped separate GUID for this, found it overkill ++ hindered look-up functionality
        
        private string GetStandardCharacterDisplayName() => localizedDisplayName.GetSafeLocalizedString();
        public GameObject GetCharacterPrefab() => GetPrefab(CharacterPrefabType.Character);
        public GameObject GetCharacterNPCPrefab() => GetPrefab(CharacterPrefabType.NPC);
        public AssetReferenceGameObject GetCharacterPrefabReference(CharacterPrefabType prefabType) => prefabType == CharacterPrefabType.NPC ? characterNPCPrefab : characterPrefab;
        public bool HasProgressionStats() => hasProgressionStats;
        public bool ShouldIncrementsStatsOnLevelUp() => incrementsStatsOnLevelUp;

        private GameObject GetPrefab(CharacterPrefabType prefabType)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) { return GetCharacterPrefabReference(prefabType)?.editorAsset; } // Editor tooling reads the project prefab directly
#endif
            return CharacterPrefabCache.TryGetPrefab(this, prefabType, out GameObject prefab) ? prefab : null;
        }
        #endregion
        
        #region DialogueVariable
        public string GetDialogueVariable(DialogueVariableContext context) => GetCharacterDisplayName(this);
        #endregion
        
        #region Localization
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

        #region AddressablesCaching
        public static IAddressablesCacheLifecycle addressablesCache => _cache;
        public static IReadOnlyDictionary<string, CharacterProperties> GetCharacterPropertiesLookup() => _cache.GetAll();
        internal static CharacterProperties GetCharacterPropertiesFromName(string name) => _cache.Get(name);
        
        public static bool TryGetCharacterPropertiesFromName(string name, out CharacterProperties characterProperties)
        {
            characterProperties = GetCharacterPropertiesFromName(name);
            if (characterProperties != null) { return true; }
            
            LDMLog.Warning(LDMLogCategory.Stats, $"Character ID '{name}' isn't in the CharacterProperties cache. Skipping it.");
            return false;
        }

        public static List<CharacterProperties> GetCharacterPropertiesFromNames(IEnumerable<string> names)
        {
            var characters = new List<CharacterProperties>();
            if (names == null) { return characters; }
            
            foreach (string name in names)
            {
                if (TryGetCharacterPropertiesFromName(name, out CharacterProperties characterProperties)) { characters.Add(characterProperties); }
            }
            return characters;
        }

        public static List<string> GetCharacterIDs(IEnumerable<CharacterProperties> characters) => characters?.Where(characterProperties => characterProperties != null).Select(characterProperties => characterProperties.GetCharacterID()).ToList() ?? new List<string>();
        #endregion
    }
}

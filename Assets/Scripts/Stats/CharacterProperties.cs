using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using Frankie.Utils.Addressables;
using Frankie.Utils.Localization;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Frankie.Stats
{
    [CreateAssetMenu(fileName = "New Character", menuName = "Characters/New Character")]
    public class CharacterProperties : ScriptableObject, IAddressablesCache, ILocalizable
    {
        // Properties
        [SerializeField][SimpleLocalizedString(LocalizationTableType.Core, true)] public LocalizedString localizedDisplayName;
        [SerializeField] public GameObject characterPrefab;
        [SerializeField] public GameObject characterNPCPrefab;
        [SerializeField] public bool hasProgressionStats = true;
        [SerializeField] public bool incrementsStatsOnLevelUp = false;
        
        // State
        [HideInInspector][SerializeField] private string cachedName;
        private static AsyncOperationHandle<IList<CharacterProperties>> _addressablesLoadHandle;
        private static Dictionary<string, CharacterProperties> _characterLookupCache;

        #region Getters
        public string GetCharacterDisplayName() => localizedDisplayName.GetSafeLocalizedString();
        public string GetCharacterID() => name;
        // Note:  Using name as ID for simplicity
        // Previously scoped separate GUID for this, found it overkill ++ hindered look-up functionality
        
        public LocalizationTableType localizationTableType { get; } = LocalizationTableType.Core;
        public List<TableEntryReference> GetLocalizationEntries()
        {
            return new List<TableEntryReference>
            {
                localizedDisplayName.TableEntryReference
            };
        }
        #endregion
        
        #region StaticMethods
        public static bool AreCharacterPropertiesMatched(CharacterProperties entryA, CharacterProperties entryB)
        {
            if (entryA == null || entryB == null) { return false; }
            return entryA.GetCharacterID() == entryB.GetCharacterID();
        }
        #endregion
        
#if UNITY_EDITOR
        #region LocalizationUtility
        private string GetNameLocalizationKey() => GetNameLocalizationKey(name);
        private static string GetNameLocalizationKey(string id) => $"{nameof(CharacterProperties)}.{id}";
        
        private void ReconcileCachedName()
        {
            if (name == cachedName) { return; }

            TableEntryReference oldKey = GetNameLocalizationKey(cachedName);
            cachedName = name;
            string newKey = GetNameLocalizationKey();
            LocalizationTool.MakeOrRenameKey(localizationTableType, oldKey, newKey);
            
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssetIfDirty(this);
        }

        public void TryLocalizeDefaults()
        {
            ReconcileCachedName();
            string key = GetNameLocalizationKey();
            if (!LocalizationTool.TryLocalizeEntry(localizationTableType, localizedDisplayName, key, name)) { return; }
            
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssetIfDirty(this);
        }
        #endregion
#endif
        
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

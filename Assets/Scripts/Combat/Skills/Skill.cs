using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using Frankie.Stats;
using Frankie.Utils.Addressables;
using Frankie.Utils.Localization;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Frankie.Combat
{
    [CreateAssetMenu(fileName = "New Skill", menuName = "Skills/New Skill")]
    public class Skill : ScriptableObject, IBattleActionSuper, IAddressablesCache, ILocalizable
    {
        // Tunables
        [SerializeField][SimpleLocalizedString(LocalizationTableType.Skills, true)] private LocalizedString localizedDisplayName; 
        [SerializeField][SkillStat] private Stat skillStat;
        [SerializeField] private BattleAction battleAction;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.Skills, true)] private LocalizedString localizedDetail;
        [SerializeField] private string detail = "";

        // State
        [HideInInspector][SerializeField] private string cachedName;
        private static AsyncOperationHandle<IList<Skill>> _addressablesLoadHandle;
        private static Dictionary<string, Skill> _skillLookupCache;

        #region Getters
        public static string GetSkillNamePretty(string name) => Regex.Replace(name, "([a-z])_?([A-Z])", "$1 $2"); 
        public Stat GetStat() => skillStat;
        public bool IsItem() => false;
        public string GetName() => GetSkillNamePretty(name);
        public string GetDetail() => detail;
        public float GetAPCost() => battleAction == null ? 0f : battleAction.GetAPCost();
        
        public LocalizationTableType localizationTableType { get; } = LocalizationTableType.Skills;
        public List<TableEntryReference> GetLocalizationEntries()
        {
            throw new NotImplementedException();
        }
        #endregion

        #region PublicMethods
        public bool Use(BattleActionData battleActionData, Action finished)
        {
            return battleAction.Use(battleActionData, true, finished);
        }

        public void SetTargets(TargetingNavigationType targetingNavigationType, BattleActionData battleActionData, IEnumerable<BattleEntity> activeCharacters, IEnumerable<BattleEntity> activeEnemies)
        {
            battleAction?.SetTargets(targetingNavigationType, battleActionData, activeCharacters, activeEnemies);
        }
        #endregion
        
#if UNITY_EDITOR
        #region LocalizationUtility
        private string GetNameLocalizationKey() => GetNameLocalizationKey(name);
        private static string GetNameLocalizationKey(string id) => $"{nameof(Skill)}.{id}";
        
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

        public void TryLocalizedName()
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
        public static Skill GetSkillFromName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) { return null; }
            BuildCacheIfEmpty();
            return _skillLookupCache.GetValueOrDefault(name);
        }

        public static void BuildCacheIfEmpty()
        {
            // In Unity Editor, force-build cache for quicker / simpler debug
#if (!UNITY_EDITOR)
            if (_skillLookupCache != null) { return; }
#endif

            _skillLookupCache = new Dictionary<string, Skill>();
            _addressablesLoadHandle = Addressables.LoadAssetsAsync(nameof(Skill), (Skill skill) =>
                {
                    if (_skillLookupCache.TryGetValue(skill.name, out Skill value)) { Debug.LogError($"Looks like there's a duplicate ID for objects: {value} and {skill}"); }
                    _skillLookupCache[skill.name] = skill;
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

using System;
using System.Collections.Generic;
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
    [CreateAssetMenu(fileName = "New Skill", menuName = "Skills/New Skill", order = 10)]
    public class Skill : ScriptableObject, IBattleActionSuper, IAddressablesCache, ILocalizable
    {
        // Tunables
        [SerializeField][SimpleLocalizedString(LocalizationTableType.Skills, true)] private LocalizedString localizedDisplayName; 
        [SerializeField][SkillStat] private Stat skillStat;
        [SerializeField] private BattleAction battleAction;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.Skills, true)] private LocalizedString localizedDetail;

        // State
        [HideInInspector][SerializeField] private string cachedName;
        public string iCachedName { get => cachedName; set => cachedName = value; }
        private static AsyncOperationHandle<IList<Skill>> _addressablesLoadHandle;
        private static Dictionary<string, Skill> _skillLookupCache;

        #region Getters
        public string GetName() => localizedDisplayName.GetSafeLocalizedString();
        public Stat GetStat() => skillStat;
        public bool IsItem() => false;
        public string GetDetail() => localizedDetail.GetSafeLocalizedString();
        public float GetAPCost() => battleAction == null ? 0f : battleAction.GetAPCost();
        
        public LocalizationTableType localizationTableType { get; } = LocalizationTableType.Skills;
        public List<TableEntryReference> GetLocalizationEntries()
        {
            return new List<TableEntryReference>
            {
                localizedDisplayName.TableEntryReference,
                localizedDetail.TableEntryReference
            };
        }

        public List<(string propertyName, LocalizedString localizedString, bool setToName)> GetPropertyLinkedLocalizationEntries()
        {
            return new List<(string propertyName, LocalizedString localizedString, bool setToName)>
            {
                (nameof(localizedDisplayName), localizedDisplayName, true),
                (nameof(localizedDetail), localizedDetail, false)
            };
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

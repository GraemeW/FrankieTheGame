using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Frankie.Core;

namespace Frankie.Combat
{
    [CreateAssetMenu(fileName = "New Skill", menuName = "Skills/New Skill")]
    public class Skill : ScriptableObject, IBattleActionSuper, IAddressablesCache
    {
        // Tunables
        [SerializeField] private SkillStat stat;
        [SerializeField] private BattleAction battleAction;
        [SerializeField] private string detail = "";

        // State
        private static AsyncOperationHandle<IList<Skill>> _addressablesLoadHandle;
        private static Dictionary<string, Skill> _skillLookupCache;

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
            if (skillLookupCache != null) { return; }
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

        #region Getters
        public static string GetSkillNamePretty(string name) => Regex.Replace(name, "([a-z])_?([A-Z])", "$1 $2"); 
        public SkillStat GetStat() => stat;
        public bool IsItem() => false;
        public string GetName() => GetSkillNamePretty(name);
        public string GetDetail() => detail;
        public float GetAPCost() => battleAction == null ? 0f : battleAction.GetAPCost();
        #endregion

        #region PublicMethods
        public bool Use(BattleActionData battleActionData, Action finished)
        {
            return battleAction.Use(battleActionData, finished);
        }

        public void SetTargets(TargetingNavigationType targetingNavigationType, BattleActionData battleActionData,
            IEnumerable<BattleEntity> activeCharacters, IEnumerable<BattleEntity> activeEnemies)
        {
            battleAction?.SetTargets(targetingNavigationType, battleActionData, activeCharacters, activeEnemies);
        }
        #endregion
    }
}

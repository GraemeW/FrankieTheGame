using Frankie.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Frankie.Combat
{
    [CreateAssetMenu(fileName = "New Skill", menuName = "Skills/New Skill")]
    public class Skill : ScriptableObject, IBattleActionSuper, IAddressablesCache
    {
        // Tunables
        [SerializeField] SkillStat stat = default;
        [SerializeField] BattleAction battleAction = null;
        [SerializeField] string detail = "";

        // State
        static AsyncOperationHandle<IList<Skill>> addressablesLoadHandle;
        static Dictionary<string, Skill> skillLookupCache;

        #region AddressablesCaching
        public static Skill GetSkillFromName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) { return null; }

            BuildCacheIfEmpty();
            if (name == null || !skillLookupCache.ContainsKey(name)) return null;
            return skillLookupCache[name];
        }

        public static void BuildCacheIfEmpty()
        {
#if (!UNITY_EDITOR)
            if (skillLookupCache != null) { return; }
#endif

            skillLookupCache = new Dictionary<string, Skill>();
            addressablesLoadHandle = Addressables.LoadAssetsAsync(typeof(Skill).Name, (Skill skill) =>
            {
                if (skillLookupCache.ContainsKey(skill.name))
                {
                    Debug.LogError(string.Format("Looks like there's a duplicate ID for objects: {0} and {1}", skillLookupCache[skill.name], skill));
                }

                skillLookupCache[skill.name] = skill;
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
        public static string GetSkillNamePretty(string name)
        {
            return Regex.Replace(name, "([a-z])_?([A-Z])", "$1 $2");
        }

        public SkillStat GetStat()
        {
            return stat;
        }
#endregion

#region BattleActionUserInterface
        public bool Use(BattleActionData battleActionData, Action finished)
        {
            return battleAction.Use(battleActionData, finished);
        }

        public void GetTargets(bool? traverseForward, BattleActionData battleActionData,
            IEnumerable<CombatParticipant> activeCharacters, IEnumerable<CombatParticipant> activeEnemies)
        {
            battleAction?.GetTargets(traverseForward, battleActionData, activeCharacters, activeEnemies);
        }

        public bool IsItem()
        {
            return false;
        }

        public string GetName()
        {
            return GetSkillNamePretty(name);
        }

        public string GetDetail()
        {
            return detail;
        }

        public float GetAPCost()
        {
            if (battleAction == null) { return 0f; }
            return battleAction.GetAPCost();
        }
#endregion
    }
}

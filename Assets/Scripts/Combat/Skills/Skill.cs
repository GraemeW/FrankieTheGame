using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Frankie.Combat
{
    [CreateAssetMenu(fileName = "New Skill", menuName = "Skills/New Skill")]
    public class Skill : ScriptableObject, IBattleActionUser, ISerializationCallbackReceiver
    {
        // Tunables
        [SerializeField] SkillStat stat = default;
        [SerializeField] BattleAction battleAction = null;
        [SerializeField] string detail = "";

        // State
        static Dictionary<string, Skill> skillLookupCache;

        #region SkillToNameCaching
        public static Skill GetSkillFromName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) { return null; }

            if (skillLookupCache == null)
            {
                BuildSkillCache();
            }
            if (name == null || !skillLookupCache.ContainsKey(name)) return null;
            return skillLookupCache[name];
        }

        public static string GetSkillNamePretty(string name)
        {
            return Regex.Replace(name, "([a-z])_?([A-Z])", "$1 $2");
        }

        private static void BuildSkillCache()
        {
            skillLookupCache = new Dictionary<string, Skill>();
            Skill[] skillList = Resources.LoadAll<Skill>("");
            foreach (Skill skill in skillList)
            {
                if (skillLookupCache.ContainsKey(skill.name))
                {
                    Debug.LogError(string.Format("Looks like there's a duplicate ID for objects: {0} and {1}", skillLookupCache[skill.name], skill));
                    continue;
                }

                skillLookupCache[skill.name] = skill;
            }
        }
        #endregion

        #region PublicMethods
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

        #region SerializationInterface
        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
#if UNITY_EDITOR
            BuildSkillCache(); // Force reload of skill cache to populate skill look-up in editor
            AttemptToSetBattleAction();
#endif
        }

        private void AttemptToSetBattleAction()
        {
#if UNITY_EDITOR
            BattleAction battleActionFromName = BattleAction.GetBattleActionFromName(name);
            if (battleActionFromName != null && battleActionFromName != battleAction)
            {
                UnityEngine.Debug.Log($"Set battle action from name: {battleActionFromName}");
                battleAction = battleActionFromName;
                EditorUtility.SetDirty(this);
            }
#endif
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            // Unused, required for interface
        }
        #endregion
    }
}

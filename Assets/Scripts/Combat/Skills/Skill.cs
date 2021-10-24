using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Frankie.Combat
{
    [CreateAssetMenu(fileName = "New Skill", menuName = "Skills/New Skill")]
    public class Skill : ScriptableObject, IBattleActionUser, ISerializationCallbackReceiver
    {
        // Tunables
        public SkillStat stat = default;
        [SerializeField] BattleAction battleAction = null;

        // State
        static Dictionary<string, Skill> skillLookupCache;

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

        private void AttemptToSetBattleAction()
        {
            BattleAction battleActionFromName = BattleAction.GetBattleActionFromName(name);
            if (battleActionFromName != null && battleActionFromName != battleAction)
            {
                battleAction = battleActionFromName;
                Debug.Log($"Battle action updated to: {battleActionFromName}");
            }
        }

        public void Use(CombatParticipant sender, IEnumerable<CombatParticipant> recipients, Action finished)
        {
            battleAction.Use(sender, recipients);

            if (finished != null)
            {
                finished.Invoke();
            }
        }

        public IEnumerable<CombatParticipant> GetTargets(bool? traverseForward, IEnumerable<CombatParticipant> currentTarget, IEnumerable<CombatParticipant> activeCharacters, IEnumerable<CombatParticipant> activeEnemies)
        {
            return battleAction.GetTargets(traverseForward, currentTarget, activeCharacters, activeEnemies);
        }

        public bool IsItem()
        {
            return false;
        }

        public string GetName()
        {
            return GetSkillNamePretty(name);
        }

        #region Interfaces
        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
#if UNITY_EDITOR
            BuildSkillCache(); // Force reload of skill cache to populate skill look-up in editor
            AttemptToSetBattleAction();
#endif
        }

         void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            // Unused, required for interface
        }
        #endregion
    }
}

using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Frankie.Combat
{
    [CreateAssetMenu(fileName = "New Skill", menuName = "Skills/New Skill")]
    public class Skill : ScriptableObject, ISerializationCallbackReceiver
    {
        // Tunables
        [Header("Behaviour")]
        public float cooldown = 1.0f;
        [Tooltip("This only changes the sorting order")] [SerializeField] bool friendly = false;
        [Header("Modifiers")]
        public SkillStat stat = default;
        public float apValue = 0f;
        public float hpValue = 0f;
        public int numberOfHits = 1;
        public DamageType damageType = default;
        public StatusEffectProbabilityPair[] statusEffects = null;

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

        public static string GetSkillNamePretty(string skillName)
        {
            return Regex.Replace(skillName, "([a-z])_?([A-Z])", "$1 $2");
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

        public bool IsFriendly()
        {
            return friendly;
        }

        public void OnBeforeSerialize()
        {
#if UNITY_EDITOR
            BuildSkillCache(); // Force reload of skill cache to populate skill look-up in editor
#endif
        }

        public void OnAfterDeserialize()  // Unused, required for interface
        {
        }
    }
}

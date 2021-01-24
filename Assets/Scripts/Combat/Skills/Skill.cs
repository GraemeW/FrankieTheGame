using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Combat
{
    [CreateAssetMenu(fileName = "New Skill", menuName = "Skills/New Skill")]
    public class Skill : ScriptableObject
    {
        // Tunables
        [Header("Behaviour")]
        public bool isFriendly = false;
        public float cooldown = 1.0f;
        [Header("Modifiers")]
        public float pointAdder = 0f;
        public int numberOfHits = 1;
        public DamageType damageType = default;
        public StatusProbabilityPair[] statusEffects = null;

        // Data Structures
        [System.Serializable]
        public struct StatusProbabilityPair
        {
            public StatusEffect statusEffect;
            [Range(0,1)] public float fractionalProbability;
        }

        // State
        static Dictionary<string, Skill> skillLookupCache;

        public static Skill GetSkillFromName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) { return null; }

            if (skillLookupCache == null)
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
            if (name == null || !skillLookupCache.ContainsKey(name)) return null;
            return skillLookupCache[name];
        }
    }
}

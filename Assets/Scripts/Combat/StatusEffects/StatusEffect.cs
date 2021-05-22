using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Frankie.Combat
{
    [CreateAssetMenu(fileName = "New Status Effect", menuName = "Skills/New Status Effect")]
    public class StatusEffect : ScriptableObject
    {
        // Config Data
        [Tooltip("Behavior Definition -- See ActiveStatusEffect for implementation")]
        public StatusEffectType statusEffectType;
        [Tooltip("Major impact parameter -- See ActiveStatusEffect for implementation")]
        public float primaryValue;
        [Tooltip("Minor impact parameter -- See ActiveStatusEffect for implementation")]
        public float secondaryValue;
        [Tooltip("Total effect duration before expiration")]
        public float duration;
        [Tooltip("For effects that trigger multiple times, defines number of triggers")]
        public int numberOfTicks;

        static Dictionary<string, StatusEffect> statusEffectLookupCache;

        public static StatusEffect GetFromName(string statusEffectName)
        {
            if (statusEffectLookupCache == null)
            {
                BuildCaches();
            }

            if (statusEffectName == null || !statusEffectLookupCache.ContainsKey(statusEffectName)) return null;
            return statusEffectLookupCache[statusEffectName];
        }

        private static void BuildCaches()
        {
            statusEffectLookupCache = new Dictionary<string, StatusEffect>();
            StatusEffect[] statusEffectList = Resources.LoadAll<StatusEffect>("");
            foreach (StatusEffect statusEffect in statusEffectList)
            {
                if (statusEffectLookupCache.ContainsKey(statusEffect.name))
                {
                    Debug.LogError(string.Format("Looks like there's a duplicate ID for objects: {0} and {1}", statusEffectLookupCache[statusEffect.name], statusEffect));
                    continue;
                }

                statusEffectLookupCache[statusEffect.name] = statusEffect;
            }
        }
    }
}

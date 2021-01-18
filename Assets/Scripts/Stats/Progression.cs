using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Stats
{
    [CreateAssetMenu(fileName = "Progression", menuName = "Stats/New Progression", order = 0)]
    public class Progression : ScriptableObject
    {
        // Tunables
        [SerializeField] ProgressionCharacterClass[] characterClasses = default;

        // State
        Dictionary<CharacterName, Dictionary<Stat, float[]>> lookupTable = null;

        public float GetStat(Stat stat, CharacterName characterName, int level)
        {
            BuildLookup();
            float[] levels = lookupTable[characterName][stat];
            int safeLevel = Mathf.Clamp(level - 1, 0, levels.Length - 1);
            return levels[safeLevel];
        }

        public int GetLevels(Stat stat, CharacterName characterName)
        {
            BuildLookup();
            return lookupTable[characterName][stat].Length;
        }

        private void BuildLookup()
        {
            if (lookupTable != null) { return; }
            lookupTable = new Dictionary<CharacterName, Dictionary<Stat, float[]>>();

            foreach (ProgressionCharacterClass progressionCharacterClass in characterClasses)
            {
                Dictionary<Stat, float[]> statDictionary = new Dictionary<Stat, float[]>();
                foreach (ProgressionStat progressionStat in progressionCharacterClass.stats)
                {
                    statDictionary[progressionStat.stat] = progressionStat.levels;
                }
                lookupTable[progressionCharacterClass.characterName] = statDictionary;
            }
        }

        // Data structures
        [System.Serializable]
        class ProgressionCharacterClass
        {
            public CharacterName characterName = CharacterName.None;
            public ProgressionStat[] stats = default;
        }

        [System.Serializable]
        class ProgressionStat
        {
            public Stat stat = default;
            public float[] levels = default;
        }
    }
}

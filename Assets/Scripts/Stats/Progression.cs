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
        Dictionary<CharacterProperties, Dictionary<Stat, float[]>> lookupTable = null;

        public float GetStat(Stat stat, CharacterProperties characterProperties, int level)
        {
            BuildLookup();
            float[] levels = lookupTable[characterProperties][stat];
            int safeLevel = Mathf.Clamp(level - 1, 0, levels.Length - 1);
            return levels[safeLevel];
        }

        public Dictionary<Stat, float> GetStatSheet(CharacterProperties characterProperties, int level)
        {
            BuildLookup();
            Dictionary<Stat, float> statSheet = new Dictionary<Stat, float>();
            Dictionary<Stat, float[]> statBook = lookupTable[characterProperties];
            foreach (Stat stat in statBook.Keys)
            {
                statSheet[stat] = GetStat(stat, characterProperties, level);
            }
            return statSheet;
        }

        public int GetLevels(Stat stat, CharacterProperties characterProperties)
        {
            BuildLookup();
            return lookupTable[characterProperties][stat].Length;
        }

        private void BuildLookup()
        {
            if (lookupTable != null) { return; }
            lookupTable = new Dictionary<CharacterProperties, Dictionary<Stat, float[]>>();

            foreach (ProgressionCharacterClass progressionCharacterClass in characterClasses)
            {
                Dictionary<Stat, float[]> statDictionary = new Dictionary<Stat, float[]>();
                foreach (ProgressionStat progressionStat in progressionCharacterClass.stats)
                {
                    statDictionary[progressionStat.stat] = progressionStat.levels;
                }
                lookupTable[progressionCharacterClass.characterProperties] = statDictionary;
            }
        }

        // Data structures
        [System.Serializable]
        class ProgressionCharacterClass
        {
            public CharacterProperties characterProperties = null;
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

using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Stats
{
    [CreateAssetMenu(fileName = "Progression", menuName = "Stats/New Progression", order = 0)]
    public class Progression : ScriptableObject
    {
        // Tunables
        [SerializeField] float defaultStatValueIfMissing = 1f;
        [SerializeField] ProgressionCharacterClass[] characterClasses = default;

        // State
        Dictionary<CharacterProperties, Dictionary<Stat, float>> lookupTable = null;

        public float GetStat(Stat stat, CharacterProperties characterProperties)
        {
            BuildLookup();
            return lookupTable[characterProperties][stat];
        }

        public Dictionary<Stat, float> GetStatSheet(CharacterProperties characterProperties)
        {
            BuildLookup();
            Dictionary<Stat, float> statSheet = new Dictionary<Stat, float>();
            Dictionary<Stat, float> statBook = lookupTable[characterProperties];
            foreach (Stat stat in statBook.Keys)
            {
                statSheet[stat] = GetStat(stat, characterProperties);
            }
            return statSheet;
        }

        private void BuildLookup()
        {
            if (lookupTable != null) { return; }
            lookupTable = new Dictionary<CharacterProperties, Dictionary<Stat, float>>();

            foreach (ProgressionCharacterClass progressionCharacterClass in characterClasses)
            {
                Dictionary<Stat, float> statDictionary = new Dictionary<Stat, float>();

                foreach (Stat stat in Enum.GetValues(typeof(Stat)))
                {
                    bool foundStat = false;
                    foreach (ProgressionStat progressionStat in progressionCharacterClass.stats)
                    {
                        if (progressionStat.stat == stat)
                        {
                            statDictionary[stat] = progressionStat.value;
                            foundStat = true;
                            break;
                        }
                    }
                    if (!foundStat) { statDictionary[stat] = defaultStatValueIfMissing; }
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
            public float value = default;
        }
    }
}

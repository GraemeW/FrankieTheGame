using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Frankie.Stats
{
    [CreateAssetMenu(fileName = "Progression", menuName = "Stats/New Progression", order = 0)]
    public class Progression : ScriptableObject
    {
        // Tunables
        [SerializeField] private float defaultStatValueIfMissing = 1f;
        [SerializeField] private ProgressionCharacter[] characters;

        // State
        private Dictionary<CharacterProperties, Dictionary<Stat, float>> lookupTable;

        // Static
        private static float GetDefaultStatValue(Stat stat)
        {
            return stat switch
            {
                Stat.HP => 20f,
                Stat.AP => 10f,
                Stat.ExperienceReward => 500f,
                Stat.ExperienceToLevelUp => 999f,
                Stat.Brawn => 3f,
                Stat.Beauty => 3f,
                Stat.Smarts => 3f,
                Stat.Nimble => 3f,
                Stat.Luck => 3f,
                Stat.Pluck => 3f,
                Stat.Stoic => 2f,
                Stat.InitialLevel => 1f,
                _ => 0f
            };
        }
        
        #if UNITY_EDITOR
        public void ForceBuildLookup()
        {
            BuildLookup(true);
        }
        
        public void UpdateProgressionAsset(CharacterProperties characterProperties, Stat updatedStat, float newValue)
        {
            Undo.RegisterCompleteObjectUndo(this, "Update Progression");
            bool characterFound = false;
            foreach (ProgressionCharacter character in characters)
            {
                if (character.characterProperties != characterProperties) { continue; }

                characterFound = true;
                bool statFound = false;
                foreach (ProgressionStat progressionStat in character.stats)
                {
                    if (progressionStat.stat != updatedStat) { continue; }
                    
                    statFound = true;
                    progressionStat.value = newValue;
                    break;
                }

                if (!statFound)
                {
                    var newProgressionStat = new ProgressionStat { stat = updatedStat, value = newValue };
                    Array.Resize(ref character.stats, character.stats.Length + 1);
                    character.stats[^1] =  newProgressionStat;
                }
                break;
            }
            EditorUtility.SetDirty(this);

            if (characterFound)
            {
                // Update dictionary to avoid need to rebuild entire dict on every update
                lookupTable[characterProperties][updatedStat] = newValue;
            }
        }

        public void AddToProgressionAsset(CharacterProperties characterProperties)
        {
            var character = new ProgressionCharacter
            {
                characterProperties = characterProperties,
                stats = (from Stat stat in Enum.GetValues(typeof(Stat)) select new ProgressionStat { stat = stat, value = GetDefaultStatValue(stat) }).ToArray()
            };
            
            Undo.RegisterCompleteObjectUndo(this, "Add to Progression");
            Array.Resize(ref characters, characters.Length + 1);
            characters[^1] = character;
            EditorUtility.SetDirty(this);
            
            ForceBuildLookup();
        }

        public void RemoveFromProgressionAsset(IEnumerable<CharacterProperties> charactersProperties)
        {
            Undo.RegisterCompleteObjectUndo(this, "Remove from Progression");
            foreach (CharacterProperties characterProperties in charactersProperties)
            {
                var charactersVolatile = new List<ProgressionCharacter>(characters);
                charactersVolatile.RemoveAll(x => x.characterProperties == characterProperties);
                characters =  charactersVolatile.ToArray();
            }
            EditorUtility.SetDirty(this);
            
            ForceBuildLookup();
        }
        #endif
        
        #region PublicMethods
        public ProgressionCharacter[] GetCharacters() => characters;

        public bool HasProgression(CharacterProperties characterProperties)
        {
            BuildLookup();
            return lookupTable.ContainsKey(characterProperties);
        }
        
        public float GetStat(Stat stat, CharacterProperties characterProperties)
        {
            BuildLookup();
            return lookupTable[characterProperties][stat];
        }

        public Dictionary<Stat, float> GetStatSheet(CharacterProperties characterProperties)
        {
            BuildLookup();
            var statSheet = new Dictionary<Stat, float>();
            Dictionary<Stat, float> statBook = lookupTable[characterProperties];
            foreach (Stat stat in statBook.Keys)
            {
                statSheet[stat] = GetStat(stat, characterProperties);
            }
            return statSheet;
        }
        #endregion
        
        #region PrivateMethods
        private void BuildLookup(bool forceBuild = false)
        {
            if (lookupTable != null && !forceBuild) { return; }
            lookupTable = new Dictionary<CharacterProperties, Dictionary<Stat, float>>();

            foreach (ProgressionCharacter character in characters)
            {
                var statDictionary = new Dictionary<Stat, float>();

                foreach (Stat stat in Enum.GetValues(typeof(Stat)))
                {
                    bool foundStat = false;
                    foreach (ProgressionStat progressionStat in character.stats)
                    {
                        if (progressionStat.stat != stat) continue;
                        statDictionary[stat] = progressionStat.value;
                        foundStat = true;
                        break;
                    }
                    if (!foundStat) { statDictionary[stat] = defaultStatValueIfMissing; }
                }
                lookupTable[character.characterProperties] = statDictionary;
            }
        }
        #endregion

        #region DataStructures
        [Serializable]
        public class ProgressionCharacter
        {
            public CharacterProperties characterProperties;
            public ProgressionStat[] stats;
        }

        [Serializable]
        public class ProgressionStat
        {
            public Stat stat;
            public float value;
        }
        
        #endregion
    }
}

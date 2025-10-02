using UnityEngine;
using Frankie.Utils;

namespace Frankie.Combat.Spawner
{
    [System.Serializable]
    public class SpawnConfigurationProbabilityPair<T> : IObjectProbabilityPair<T> where T : SpawnConfiguration
    {
        [SerializeField] public SpawnConfiguration spawnConfiguration;
        [SerializeField][Min(0)] public int probability;

        public SpawnConfigurationProbabilityPair(SpawnConfiguration spawnConfiguration, int probability)
        {
            this.spawnConfiguration = spawnConfiguration;
            this.probability = probability;
        }

        public T GetObject()
        {
            return spawnConfiguration as T;
        }

        public int GetProbability()
        {
            return probability;
        }
    }
}

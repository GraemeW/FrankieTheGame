using UnityEngine;
using LowDefMustard.Utils;

namespace Frankie.Combat.Spawner
{
    [System.Serializable]
    public class SpawnConfigurationProbabilityPair : IObjectProbabilityPair<SpawnConfiguration>
    {
        [SerializeField] public SpawnConfiguration spawnConfiguration;
        [SerializeField][Min(0)] public int probabilityWeight;

        public SpawnConfigurationProbabilityPair(SpawnConfiguration spawnConfiguration, int probabilityWeight)
        {
            this.spawnConfiguration = spawnConfiguration;
            this.probabilityWeight = probabilityWeight;
        }

        public SpawnConfiguration value => spawnConfiguration;
        public int weight => probabilityWeight;
    }
}

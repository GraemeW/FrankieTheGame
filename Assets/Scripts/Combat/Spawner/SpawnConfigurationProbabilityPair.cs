using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Combat.Spawner
{
    [System.Serializable]
    public struct SpawnConfigurationProbabilityPair
    {
        [SerializeField] public SpawnConfiguration spawnConfiguration;
        [SerializeField] [Tooltip("Fractional probability is probability divided by sum(probability) for all spawn configurations")] public int probability;
    }
}
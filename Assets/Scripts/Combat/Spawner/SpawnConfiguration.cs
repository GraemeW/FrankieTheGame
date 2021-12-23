using Frankie.Stats;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Combat.Spawner
{
    [System.Serializable]
    public class SpawnConfiguration
    {
        [SerializeField] [Tooltip("Will force to not exceed this amount, clips as iterating")] public int maxQuantity = 0;
        [SerializeField] public EnemyConfiguration[] enemyConfigurations = null;

        public static IEnumerable<CharacterProperties> GetEnemies(EnemyConfiguration[] enemyConfigurations, int maxQuantity)
        {
            if (enemyConfigurations == null) { yield break; }

            int quantityPassed = 0;
            foreach (EnemyConfiguration enemyConfiguration in enemyConfigurations)
            {
                // Erroneous input
                if (enemyConfiguration.characterProperties == null) { continue; }
                if (enemyConfiguration.maximum <= 0) { continue; }

                // Edge case
                int minimum = Mathf.Max(enemyConfiguration.minimum, 0);
                int maximum = Mathf.Max(minimum, enemyConfiguration.maximum); // +1 offset since random exclusive w/ ints

                int quantityToSpawn = Random.Range(minimum, maximum + 1);
                for (int i = 0; i < quantityToSpawn; i++)
                {
                    yield return enemyConfiguration.characterProperties;
                    quantityPassed++;

                    if (quantityPassed >= maxQuantity) { yield break; }
                }
            }
        }
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Frankie.Stats;

namespace Frankie.Combat.Spawner
{
    public class EnemySpawner : MonoBehaviour
    {
        // Tunables
        [SerializeField] float xJitterDistance = 1.0f;
        [SerializeField] float yJitterDistance = 1.0f;
        [SerializeField] SpawnConfigurationProbabilityPair[] spawnConfigurations = null;

        private void OnEnable()
        {
            SpawnEnemies();
        }

        private void OnDisable()
        {
            DespawnEnemies();
        }

        private void SpawnEnemies()
        {
            SpawnConfiguration spawnConfiguration = GetSpawnConfiguration();
            if (spawnConfiguration == null) { return; }

            int maxQuantity = spawnConfiguration.maxQuantity;
            EnemyConfiguration[] enemyConfigurations = spawnConfiguration.enemyConfigurations;
            if (spawnConfiguration.maxQuantity == 0 || enemyConfigurations == null) { return; }

            foreach (CharacterProperties characterProperties in SpawnConfiguration.GetEnemies(enemyConfigurations, maxQuantity))
            {
                GameObject enemyPrefab = characterProperties.characterNPCPrefab;
                if (enemyPrefab == null) { continue; }

                GameObject spawnedEnemy = Instantiate(enemyPrefab, transform);
                float xJitter = Random.Range(-xJitterDistance, xJitterDistance);
                float yJitter = Random.Range(-yJitterDistance, yJitterDistance);
                Vector3 jitterVector = new Vector3(xJitter, yJitter, 0f);
                spawnedEnemy.transform.position = spawnedEnemy.transform.position + jitterVector;
            }
        }

        private SpawnConfiguration GetSpawnConfiguration()
        {
            int probabilityDenominator = spawnConfigurations.Sum(x => x.probability);
            int randomRoll = Random.Range(0, probabilityDenominator);

            int accumulatingProbability = 0;
            foreach (SpawnConfigurationProbabilityPair spawnConfigurationProbabilityPair in spawnConfigurations)
            {
                accumulatingProbability += spawnConfigurationProbabilityPair.probability;
                if (randomRoll < accumulatingProbability)
                {
                    return spawnConfigurationProbabilityPair.spawnConfiguration;
                }
            }
            return null;
        }

        private void DespawnEnemies()
        {
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.magenta;
            Vector3 cubeCoordinates = new Vector3(xJitterDistance * 2, yJitterDistance * 2, 0f);
            Gizmos.DrawWireCube(transform.position, cubeCoordinates);
        }
#endif
    }
}

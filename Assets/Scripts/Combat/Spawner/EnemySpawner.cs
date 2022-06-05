using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Frankie.Stats;
using Frankie.Utils;

namespace Frankie.Combat.Spawner
{
    public class EnemySpawner : MonoBehaviour
    {
        // Tunables
        [SerializeField] bool spawnOnEnable = true;
        [SerializeField] float xJitterDistance = 1.0f;
        [SerializeField] float yJitterDistance = 1.0f;
        [NonReorderable][SerializeField] SpawnConfigurationProbabilityPair<SpawnConfiguration>[] spawnConfigurations = null;

        private void OnEnable()
        {
            if (spawnOnEnable) { SpawnEnemies(); }
        }

        private void OnDisable()
        {
            DespawnEnemies();
        }

        public void SpawnEnemies() // Callable by Unity Events
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
            SpawnConfiguration spawnConfiguration = ProbabilityPairOperation<SpawnConfiguration>.GetRandomObject(spawnConfigurations);
            return spawnConfiguration;
        }

        public void DespawnEnemies() // Callable by Unity Events
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

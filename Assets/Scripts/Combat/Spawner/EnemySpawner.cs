using UnityEngine;
using Frankie.Stats;
using Frankie.Utils;

namespace Frankie.Combat.Spawner
{
    public class EnemySpawner : MonoBehaviour
    {
        // Tunables
        [SerializeField] private bool spawnOnEnable = true;
        [SerializeField] private bool spawnOnVisible = false;
        [SerializeField][Min(0f)][Tooltip("in seconds")] private float timeBetweenSpawns;
        [SerializeField] private float xJitterDistance = 1.0f;
        [SerializeField] private float yJitterDistance = 1.0f;
        [NonReorderable][SerializeField] private SpawnConfigurationProbabilityPair<SpawnConfiguration>[] spawnConfigurations;

        // State
        private float timeUntilNextSpawn = -1f;

        #region UnityMethods
        private void OnEnable()
        {
            if (spawnOnEnable) { SpawnEnemies(); }
        }

        private void OnDisable()
        {
            DespawnEnemies();
        }

        private void OnBecameVisible()
        {
            if (spawnOnVisible) { SpawnEnemies(); }
        }

        private void Update()
        {
            if (timeUntilNextSpawn > 0f) { timeUntilNextSpawn -= Time.deltaTime; }
        }
        #endregion

        #region PublicMethods
        public void SpawnEnemies() // Callable by Unity Events
        {
            if (!spawnOnEnable && (timeBetweenSpawns > 0f && timeUntilNextSpawn > 0f)) { return; }

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
                float xJitter = UnityEngine.Random.Range(-xJitterDistance, xJitterDistance);
                float yJitter = UnityEngine.Random.Range(-yJitterDistance, yJitterDistance);
                var jitterVector = new Vector3(xJitter, yJitter, 0f);
                spawnedEnemy.transform.position += jitterVector;
                Debug.Log($"{spawnedEnemy.gameObject.name} has spawned at {spawnedEnemy.transform.position}.");
            }
            timeUntilNextSpawn = timeBetweenSpawns;
        }

        public void DespawnEnemies() // Callable by Unity Events
        {
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }
        }
        #endregion

        #region PrivatMethods
        private SpawnConfiguration GetSpawnConfiguration()
        {
            SpawnConfiguration spawnConfiguration = ProbabilityPairOperation<SpawnConfiguration>.GetRandomObject(spawnConfigurations);
            return spawnConfiguration;
        }
        #endregion


#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.magenta;
            var cubeCoordinates = new Vector3(xJitterDistance * 2, yJitterDistance * 2, 0f);
            Gizmos.DrawWireCube(transform.position, cubeCoordinates);
            Gizmos.DrawSphere(transform.position, 0.15f);
        }
#endif
    }
}

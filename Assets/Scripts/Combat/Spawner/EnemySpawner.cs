using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using LowDefMustard.Core;
using LowDefMustard.Saving;
using LowDefMustard.Zones;
using LowDefMustard.Utils;
using Frankie.Stats;

namespace Frankie.Combat.Spawner
{
    public class EnemySpawner : MonoBehaviour, ICharacterPrefabUser
    {
        // Note:  Enemy prefabs load through CharacterPrefabCache ~ preloaded with the scene (incl. while inactive, e.g. a room toggled on later);  a spawn only waits when they're still loading

        // Tunables
        [SerializeField] private bool spawnOnEnable = true;
        [SerializeField] private bool spawnOnVisible = false;
        [SerializeField][Min(0f)][Tooltip("in seconds")] private float timeBetweenSpawns;
        [SerializeField] private float xJitterDistance = 1.0f;
        [SerializeField] private float yJitterDistance = 1.0f;
        [NonReorderable][SerializeField] private SpawnConfigurationProbabilityPair[] spawnConfigurations;

        // State
        private bool hasStarted = false;
        private float timeUntilNextSpawn = -1f;
        private readonly List<GameObject> spawnedEnemies = new();
        private Coroutine spawnWhenLoadedRoutine;

        #region UnityMethods
        private void Start()
        {
            // First spawn deferred to Start - Awake/OnEnable order across the scene is undefined on first activation
            hasStarted = true;
            CharacterPrefabCache.StartPreload(GetCharactersToPreload()); // In case the scene skipped the transition preload (e.g. a no-fade load)
            if (spawnOnEnable) { SpawnEnemies(); }
        }

        private void OnEnable()
        {
            if (hasStarted && spawnOnEnable) { SpawnEnemies(); }
        }

        private void OnDisable()
        {
            StopSpawnWhenLoaded();
            SceneLoaderBase.QueueDelayedDestroy(spawnedEnemies.ToList());
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
            if (spawnWhenLoadedRoutine != null) { return; } // A spawn is already waiting on its load ~ ignore, so repeated calls can't keep restarting it
            if (!spawnOnEnable && (timeBetweenSpawns > 0f && timeUntilNextSpawn > 0f)) { return; }
            if (!spawnConfigurations.TryGetRandom(out SpawnConfiguration spawnConfiguration)) { return; }

            int maxQuantity = spawnConfiguration.maxQuantity;
            EnemyConfiguration[] enemyConfigurations = spawnConfiguration.enemyConfigurations;
            if (spawnConfiguration.maxQuantity == 0 || enemyConfigurations == null) { return; }
            
            List<CharacterProperties> enemies = SpawnConfiguration.GetEnemies(enemyConfigurations, maxQuantity).ToList();
            timeUntilNextSpawn = timeBetweenSpawns;
            if (CharacterPrefabCache.IsReady(enemies)) { Spawn(enemies); }
            else { spawnWhenLoadedRoutine = StartCoroutine(SpawnWhenLoaded(enemies)); }
        }

        public void DespawnEnemies() // Callable by Unity Events
        {
            foreach (GameObject spawnedEnemy in spawnedEnemies.Where(spawnedEnemy => spawnedEnemy != null))
            {
                Destroy(spawnedEnemy);
            }
            spawnedEnemies.Clear();
        }

        public IEnumerable<CharacterProperties> GetCharactersToPreload()
        {
            if (spawnConfigurations == null) { return Enumerable.Empty<CharacterProperties>(); }
            return spawnConfigurations.Where(pair => pair?.value != null).SelectMany(pair => SpawnConfiguration.GetPossibleEnemies(pair.value.enemyConfigurations)).Distinct();
        }
        #endregion

        #region PrivateMethods
        private IEnumerator SpawnWhenLoaded(List<CharacterProperties> enemies)
        {
            yield return CharacterPrefabCache.Preload(enemies);
            spawnWhenLoadedRoutine = null;
            Spawn(enemies);
        }

        private void StopSpawnWhenLoaded()
        {
            if (spawnWhenLoadedRoutine != null) { StopCoroutine(spawnWhenLoadedRoutine); }
            spawnWhenLoadedRoutine = null;
        }

        private void Spawn(IEnumerable<CharacterProperties> enemies)
        {
            foreach (CharacterProperties characterProperties in enemies)
            {
                GameObject enemyPrefab = characterProperties.GetCharacterNPCPrefab();
                if (enemyPrefab == null) { continue; }

                GameObject spawnedEnemy = Instantiate(enemyPrefab, transform);
                PreventSpawnedEnemySave(spawnedEnemy);
                
                float xJitter = UnityEngine.Random.Range(-xJitterDistance, xJitterDistance);
                float yJitter = UnityEngine.Random.Range(-yJitterDistance, yJitterDistance);
                var jitterVector = new Vector3(xJitter, yJitter, 0f);
                spawnedEnemy.transform.position += jitterVector;
                
                spawnedEnemies.Add(spawnedEnemy);
                LDMLog.Info(LDMLogCategory.Combat, $"Spawned enemy '{spawnedEnemy.gameObject.name}' (position: {spawnedEnemy.transform.position}).", spawnedEnemy.gameObject);
            }
        }

        private static void PreventSpawnedEnemySave(GameObject spawnedEnemy)
        {
            if (spawnedEnemy == null) { return; }
            foreach (SaveableEntity saveableEntity in spawnedEnemy.GetComponentsInChildren<SaveableEntity>(true))
            {
                saveableEntity.ForcePreventSave();
            }
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

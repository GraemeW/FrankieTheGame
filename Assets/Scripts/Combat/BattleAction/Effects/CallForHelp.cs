using Frankie.Combat.Spawner;
using Frankie.Stats;
using Frankie.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Combat
{
    [CreateAssetMenu(fileName = "New Call For Help Effect", menuName = "BattleAction/Effects/Call For Help Effect")]
    public class CallForHelp : EffectStrategy
    {
        // Tunables
        [NonReorderable][SerializeField] SpawnConfigurationProbabilityPair<SpawnConfiguration>[] spawnConfigurations = null;

        // Implemented Methods
        // Note:  Much of this is derivative of EnemySpawner, just done in combat -- if no battle controller is present, this function does nothing
        public override void StartEffect(CombatParticipant sender, IEnumerable<BattleEntity> recipients, DamageType damageType, Action<EffectStrategy> finished)
        {
            BattleController battleController = GameObject.FindGameObjectWithTag("BattleController")?.GetComponent<BattleController>();
            SpawnConfiguration spawnConfiguration = GetSpawnConfiguration();
            if (battleController == null || spawnConfiguration == null) { finished?.Invoke(this); return; }

            int maxQuantity = spawnConfiguration.maxQuantity;
            EnemyConfiguration[] enemyConfigurations = spawnConfiguration.enemyConfigurations;
            if (spawnConfiguration.maxQuantity == 0 || enemyConfigurations == null) { finished?.Invoke(this); return; }

            foreach (CharacterProperties characterProperties in SpawnConfiguration.GetEnemies(enemyConfigurations, maxQuantity))
            {
                GameObject enemyPrefab = characterProperties.characterNPCPrefab;
                if (enemyPrefab == null) { continue; }

                GameObject spawnedEnemy = Instantiate(enemyPrefab);
                spawnedEnemy.transform.position = sender.transform.position;

                if (spawnedEnemy.TryGetComponent(out CombatParticipant enemy))
                {
                    battleController.AddEnemyMidCombat(enemy);
                }
            }
            finished?.Invoke(this);
        }

        // Private Methods
        private SpawnConfiguration GetSpawnConfiguration()
        {
            SpawnConfiguration spawnConfiguration = ProbabilityPairOperation<SpawnConfiguration>.GetRandomObject(spawnConfigurations);
            return spawnConfiguration;
        }
    }
}

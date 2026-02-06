using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frankie.Combat.Spawner;
using Frankie.Control;
using Frankie.Stats;
using Frankie.Utils;

namespace Frankie.Combat
{
    [CreateAssetMenu(fileName = "New Call For Help Effect", menuName = "BattleAction/Effects/Call For Help Effect")]
    public class CallForHelp : EffectStrategy
    {
        // Tunables
        [SerializeField] private int maxEnemiesAllowedToCallInCombat = 3;
        [NonReorderable][SerializeField] private SpawnConfigurationProbabilityPair<SpawnConfiguration>[] spawnConfigurations;
        
        // Implemented Methods
        // Note:  Much of this is derivative of EnemySpawner, just done in combat -- if no battle controller is present, this function does nothing
        #region PublicMethods
        public override IEnumerator StartEffect(CombatParticipant sender, IList<BattleEntity> recipients, DamageType damageType)
        {
            BattleController battleController = BattleController.FindBattleController();
            SpawnConfiguration spawnConfiguration = GetSpawnConfiguration();
            if (battleController == null || spawnConfiguration == null || !HasViableSpawnConfiguration(spawnConfiguration)) { yield break; }

            if (!battleController.IsEnemyPositionAvailable() || battleController.GetCountEnemiesAddedMidCombat() >= maxEnemiesAllowedToCallInCombat)
            {
                sender.AnnounceStateUpdate(StateAlteredType.FriendIgnored);
                yield break;
            }

            bool friendFound = false;
            foreach (CharacterProperties characterProperties in SpawnConfiguration.GetEnemies(spawnConfiguration.enemyConfigurations, spawnConfiguration.maxQuantity))
            {
                GameObject enemyPrefab = characterProperties.characterNPCPrefab;
                if (enemyPrefab == null) { continue; }

                GameObject spawnedEnemy = Instantiate(enemyPrefab);
                DisableEnemyColliders(spawnedEnemy);
                SetEnemyDisposition(spawnedEnemy);
                spawnedEnemy.transform.position = sender.transform.position;

                if (spawnedEnemy.TryGetComponent(out CombatParticipant enemy))
                {
                    battleController.AddEnemyMidCombat(enemy);
                    friendFound = true;
                }
                else
                { Destroy(spawnedEnemy); } // Safety on shenanigans (spawned enemy lacking a combat participant component
            }

            sender.AnnounceStateUpdate(friendFound ? StateAlteredType.FriendFound : StateAlteredType.FriendIgnored);
        }
        #endregion

        #region PrivateMethods
        private static bool HasViableSpawnConfiguration(SpawnConfiguration spawnConfiguration) => spawnConfiguration.maxQuantity != 0 && spawnConfiguration.enemyConfigurations != null;
        private SpawnConfiguration GetSpawnConfiguration() => ProbabilityPairOperation<SpawnConfiguration>.GetRandomObject(spawnConfigurations);

        private static void DisableEnemyColliders(GameObject spawnedEnemy)
        {
            // Required to allow enemies to stack in world space (otherwise can get enemies teleporting)
            foreach (Collider2D collider in spawnedEnemy.GetComponents<Collider2D>())
            {
                collider.enabled = false;
            }
        }

        private static void SetEnemyDisposition(GameObject spawnedEnemy)
        {
            // Required to avoid weird chase/movement shenanigans
            if (spawnedEnemy.TryGetComponent(out NPCChaser npcChaser)) { npcChaser.SetChaseDisposition(false); }
            if (spawnedEnemy.TryGetComponent(out NPCStateHandler npcStateHandler)) { npcStateHandler.ForceNPCOccupied(); }
        }
        #endregion
    }
}

using Frankie.Combat.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Frankie.Combat
{
    [CreateAssetMenu(fileName = "New Spawn Target Prefab Effect", menuName = "BattleAction/Effects/Spawn Target Prefab Effect")]
    public class SpawnTargetPrefabEffect : EffectStrategy
    {
        [SerializeField] Image graphicToSpawn = null;
        [SerializeField] bool isGlobalEffect = false;
        [SerializeField] [Tooltip("Set to min to never destroy")] [Min(0f)] float destroyAfterSeconds = 2.0f;

        public override void StartEffect(CombatParticipant sender, IEnumerable<CombatParticipant> recipients, Action<EffectStrategy> finished)
        {
            BattleCanvas battleCanvas = GameObject.FindGameObjectWithTag("BattleCanvas")?.GetComponent<BattleCanvas>();
            if (battleCanvas == null) { return; }

            foreach (Vector3 position in GetPositions(recipients, battleCanvas))
            {
                Image spawnedGraphic = Instantiate(graphicToSpawn, battleCanvas.transform);
                spawnedGraphic.transform.position = position;

                if (!Mathf.Approximately(destroyAfterSeconds, 0f))
                {
                    Destroy(spawnedGraphic.gameObject, destroyAfterSeconds);
                }
            }

            finished?.Invoke(this);
        }

        private IEnumerable<Vector3> GetPositions(IEnumerable<CombatParticipant> recipients, BattleCanvas battleCanvas)
        {
            if (!isGlobalEffect)
            {
                foreach (CombatParticipant recipient in recipients)
                {
                    EnemySlide enemySlide = battleCanvas.GetEnemySlide(recipient);
                    if (enemySlide != null)
                    {
                        yield return enemySlide.transform.position;
                    }
                }
            }
            else
            {
                yield return Vector3.zero;
            }
        }
    }
}
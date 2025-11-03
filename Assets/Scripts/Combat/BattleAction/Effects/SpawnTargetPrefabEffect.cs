using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Frankie.Combat.UI;

namespace Frankie.Combat
{
    [CreateAssetMenu(fileName = "New Spawn Target Prefab Effect", menuName = "BattleAction/Effects/Spawn Target Prefab Effect")]
    public class SpawnTargetPrefabEffect : EffectStrategy
    {
        [SerializeField] private Image graphicToSpawn;
        [SerializeField] private bool isGlobalEffect = false;
        [SerializeField][Min(0f)] private float delayAfterSeconds = 0.5f;
        [SerializeField][Tooltip("Set to min to never destroy")][Min(0f)] private float destroyAfterSeconds = 2.0f;
        
        public override IEnumerator StartEffect(CombatParticipant sender, IList<BattleEntity> recipients, DamageType damageType)
        {
            BattleCanvas battleCanvas = BattleCanvas.FindBattleCanvas();
            if (battleCanvas == null) { yield break; }

            foreach (Vector3 position in GetPositions(recipients, battleCanvas))
            {
                Image spawnedGraphic = Instantiate(graphicToSpawn, battleCanvas.transform);
                spawnedGraphic.transform.position = position;

                if (!Mathf.Approximately(destroyAfterSeconds, 0f))
                {
                    Destroy(spawnedGraphic.gameObject, destroyAfterSeconds);
                }
            }
            yield return new WaitForSeconds(Mathf.Max(delayAfterSeconds, 0f));
        }

        private IEnumerable<Vector3> GetPositions(IEnumerable<BattleEntity> recipients, BattleCanvas battleCanvas)
        {
            if (!isGlobalEffect)
            {
                foreach (BattleEntity recipient in recipients)
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

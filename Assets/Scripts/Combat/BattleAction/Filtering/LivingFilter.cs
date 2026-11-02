using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Combat
{
    [CreateAssetMenu(fileName = "New Living Filtering", menuName = "BattleAction/Filters/Living", order = 15)]
    public class LivingFilter : FilterStrategy
    {
        // Tunables
        [SerializeField] private bool isAlive = true;

        // Public Methods
        public override IEnumerable<BattleEntity> Filter(IEnumerable<BattleEntity> objectsToFilter)
        {
            if (objectsToFilter == null) { yield break; }
            foreach (BattleEntity battleEntity in objectsToFilter)
            {
                if (isAlive != battleEntity.combatParticipant.IsDead())
                {
                    yield return battleEntity;
                }
            }
        }
    }
}

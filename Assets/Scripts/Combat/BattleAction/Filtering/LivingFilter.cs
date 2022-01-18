using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Frankie.Combat
{
    [CreateAssetMenu(fileName = "New Living Filtering", menuName = "BattleAction/Filters/Living")]
    public class LivingFilter : FilterStrategy
    {
        [SerializeField] bool isAlive = true;

        public override IEnumerable<CombatParticipant> Filter(IEnumerable<CombatParticipant> objectsToFilter)
        {
            if (objectsToFilter == null) { yield break; }
            foreach (CombatParticipant combatParticipant in objectsToFilter)
            {
                if (isAlive != combatParticipant.IsDead())
                {
                    yield return combatParticipant;
                }
            }
        }
    }

}

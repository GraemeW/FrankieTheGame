using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Combat
{
    [CreateAssetMenu(fileName = "New DoNothing Effect", menuName = "Combat/BattleActions/Effects/DoNothing Effect", order = 10)]
    public class DoNothingEffect : EffectStrategy
    {
        // Public Methods
        public override IEnumerator StartEffect(CombatParticipant sender, IList<BattleEntity> recipients, DamageType damageType)
        {
            yield break;
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Combat
{
    [CreateAssetMenu(fileName = "New DoNothing Effect", menuName = "BattleAction/Effects/DoNothing Effect")]
    public class DoNothingEffect : EffectStrategy
    {
        public override IEnumerator StartEffect(CombatParticipant sender, IList<BattleEntity> recipients, DamageType damageType)
        {
            yield break;
        }
    }
}

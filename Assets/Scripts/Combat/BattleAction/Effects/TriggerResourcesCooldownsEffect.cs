using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Combat
{
    [CreateAssetMenu(fileName = "New Trigger Resources Cooldowns Effect", menuName = "BattleAction/Effects/Trigger Resources Cooldowns Effect")]
    public class TriggerResourcesCooldownsEffect : EffectStrategy
    {
        public override void StartEffect(CombatParticipant sender, IEnumerable<CombatParticipant> recipients, Action<EffectStrategy> finished)
        {
            finished?.Invoke(this);
        }
    }
}
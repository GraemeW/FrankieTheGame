using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Combat
{
    public abstract class EffectStrategy : ScriptableObject
    {
        public abstract void StartEffect(CombatParticipant sender, IEnumerable<CombatParticipant> recipients);
    }
}

// To move into an effect strategy
/*
public void RemoveStatusEffects(StatusEffectProbabilityPair statusEffectProbabilityPair)
{
    float chanceRoll = UnityEngine.Random.Range(0f, 1f);
    if (statusEffectProbabilityPair.fractionalProbability < chanceRoll) { return; }

    ActiveStatusEffect[] activeStatusEffects = GetComponents<ActiveStatusEffect>();
    if (activeStatusEffects == null) { return; }

    foreach (ActiveStatusEffect activeStatusEffect in activeStatusEffects)
    {
        if (statusEffectProbabilityPair.statusEffect == null)
        {
            Destroy(activeStatusEffect);
        }
        else if (object.ReferenceEquals(activeStatusEffect.GetStatusEffect(), statusEffectProbabilityPair.statusEffect))
        {
            Destroy(activeStatusEffect);
        }
    }
}*/
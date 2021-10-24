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
/*public void ApplyStatusEffect(StatusEffectProbabilityPair statusEffectProbabilityPair, bool persistAfterBattle = false)
{
    float chanceRoll = UnityEngine.Random.Range(0f, 1f);
    if (statusEffectProbabilityPair.fractionalProbability < chanceRoll) { return; }

    ActiveStatusEffect activeStatusEffect = gameObject.AddComponent(typeof(ActiveStatusEffect)) as ActiveStatusEffect;
    activeStatusEffect.Setup(statusEffectProbabilityPair.statusEffect, this, persistAfterBattle);

    if (stateAltered != null)
    {
        stateAltered.Invoke(this, new StateAlteredData(StateAlteredType.StatusEffectApplied, statusEffectProbabilityPair.statusEffect.statusEffectType));
    }
}

public void ApplyBaseStatEffect(BaseStatModifier baseStatModifier)
{
    float baseStatModifierValue = UnityEngine.Random.Range(baseStatModifier.minValue, baseStatModifier.maxValue);

    if (baseStatModifier.permanent)
    {
        GetBaseStats().AdjustStat(baseStatModifier.stat, baseStatModifierValue);
    }
    else
    {
        ActiveBaseStatEffect activeBaseStatEffect = gameObject.AddComponent(typeof(ActiveBaseStatEffect)) as ActiveBaseStatEffect;
        activeBaseStatEffect.Setup(baseStatModifier.stat, baseStatModifierValue, baseStatModifier.duration);
    }

    if (stateAltered != null)
    {
        stateAltered.Invoke(this, new StateAlteredData(StateAlteredType.BaseStateEffectApplied, baseStatModifier.stat));
    }
}

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
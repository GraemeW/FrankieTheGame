using Frankie.Combat;
using Frankie.Stats;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Inventory
{
    [CreateAssetMenu(menuName = ("Inventory/Action Item"))]
    public class ActionItem : InventoryItem
    {
        // Config Data
        [SerializeField] bool consumable = true;
        [SerializeField] bool removeStatus = false;
        [SerializeField] BaseStatModifier[] baseStatModifiers = null;
        [SerializeField] StatusEffectProbabilityPair[] statusEffectProbabilityPairs = null;

        public void Use(CombatParticipant combatParticipant)
        {
            foreach (BaseStatModifier baseStatModifier in baseStatModifiers)
            {
                combatParticipant.ApplyBaseStatEffect(baseStatModifier);
            }
            foreach (StatusEffectProbabilityPair statusEffectProbabilityPair in statusEffectProbabilityPairs)
            {
                if (removeStatus)
                {
                    combatParticipant.RemoveStatusEffects(statusEffectProbabilityPair);
                }
                else
                {
                    combatParticipant.ApplyStatusEffect(statusEffectProbabilityPair);
                }
            }
        }

        public bool IsConsumable()
        {
            return consumable;
        }
    }

}

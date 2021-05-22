using Frankie.Combat;
using Frankie.Stats;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Inventory
{
    [CreateAssetMenu(menuName = ("Inventory/Equipable Item"))]
    public class EquipableItem : InventoryItem, IModifierProvider
    {
        [SerializeField] EquipLocation equipLocation;
        [SerializeField] BaseStatModifier[] baseStatModifiers = null;
        [SerializeField] StatusEffect[] statusEffectProbabilityPairs = null;

        public IEnumerable<float> GetAdditiveModifiers(Stat stat)
        {
            float value = 0f;
            foreach (BaseStatModifier baseStatModifier in baseStatModifiers)
            {
                if (baseStatModifier.stat == stat)
                {
                    value += Random.Range(baseStatModifier.minValue, baseStatModifier.maxValue);
                }
            }
            yield return value;
        }
    }
}
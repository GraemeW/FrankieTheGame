using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frankie.Core;
using Frankie.Stats;

namespace Frankie.Inventory
{
    [CreateAssetMenu(menuName = ("Inventory/Equipable Item"))]
    public class EquipableItem : InventoryItem, IModifierProvider
    {
        // Config Data
        [SerializeField] EquipLocation equipLocation;
        [SerializeField] BaseStatModifier[] baseStatModifiers = null;
        [SerializeField] Condition condition = null;

        public EquipLocation GetEquipLocation()
        {
            return equipLocation;
        }

        public IEnumerable<BaseStatModifier> GetBaseStatModifiers()
        {
            return baseStatModifiers;
        }

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

        public bool CanUseItem(Equipment equipment)
        {
            return condition.Check(GetEvaluators(equipment));
        }

        private IEnumerable<IPredicateEvaluator> GetEvaluators(Equipment equipment)
        {
            var predicateEvaluators = equipment.GetComponentsInChildren<IPredicateEvaluator>();

            return predicateEvaluators;
        }
    }
}

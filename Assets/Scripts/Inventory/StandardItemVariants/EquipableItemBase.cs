using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Frankie.Core.Predicates;
using Frankie.Stats;

namespace Frankie.Inventory
{
    
    public abstract class EquipableItemBase : InventoryItem, IModifierProvider
    {
        // Config Data
        [SerializeField] private List<BaseStatModifier> baseStatModifiers = new();
        [SerializeField] private Condition condition;

        #region StaticMethods
        private static IEnumerable<IPredicateEvaluator> GetEvaluators(Equipment equipment)
        {
            return equipment == null ? new List<IPredicateEvaluator>() : equipment.GetComponentsInChildren<IPredicateEvaluator>();
        }
        #endregion
        
        #region PublicMethods
        public abstract EquipLocation GetEquipLocation();
        public IList<BaseStatModifier> GetBaseStatModifiers() => baseStatModifiers;
        public bool CanUseItem(Equipment equipment) => condition.Check(GetEvaluators(equipment));

        public IEnumerable<float> GetAdditiveModifiers(Stat stat)
        {
            return from baseStatModifier in baseStatModifiers where baseStatModifier.stat == stat select Random.Range(baseStatModifier.minValue, baseStatModifier.maxValue);
        }
        #endregion
    }
}

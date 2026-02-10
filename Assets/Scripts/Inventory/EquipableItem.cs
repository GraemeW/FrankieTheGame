using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Frankie.Core;
using Frankie.Stats;

namespace Frankie.Inventory
{
    [CreateAssetMenu(menuName = ("Inventory/Equipable Item"))]
    public class EquipableItem : InventoryItem, IModifierProvider
    {
        // Config Data
        [SerializeField] private EquipLocation equipLocation;
        [SerializeField] private List<BaseStatModifier> baseStatModifiers = new();
        [SerializeField] private Condition condition;

        #region StaticMethods
        private static IEnumerable<IPredicateEvaluator> GetEvaluators(Equipment equipment)
        {
            return equipment == null ? new List<IPredicateEvaluator>() : equipment.GetComponentsInChildren<IPredicateEvaluator>();
        }
        #endregion
        
        #region PublicMethods
        public EquipLocation GetEquipLocation() => equipLocation;
        public IList<BaseStatModifier> GetBaseStatModifiers() => baseStatModifiers;
        public bool CanUseItem(Equipment equipment) => condition.Check(GetEvaluators(equipment));

        public IEnumerable<float> GetAdditiveModifiers(Stat stat)
        {
            return from baseStatModifier in baseStatModifiers where baseStatModifier.stat == stat select Random.Range(baseStatModifier.minValue, baseStatModifier.maxValue);
        }
        #endregion
    }
}

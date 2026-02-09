using System;
using System.Collections.Generic;
using UnityEngine;
using Frankie.Combat;

namespace Frankie.Inventory
{
    [CreateAssetMenu(menuName = ("Inventory/Action Item"))]
    public class ActionItem : InventoryItem, IBattleActionSuper
    {
        // Config Data
        [SerializeField] private bool consumable = true;
        [SerializeField] private BattleAction battleAction;

        private bool IsConsumable() => consumable;
        public bool IsItem() => true;
        public string GetName() => GetItemNamePretty(name);

        public bool Use(BattleActionData battleActionData, Action finished)
        {
            if (battleAction == null) { return false; }

            battleAction.Use(battleActionData, false, finished);
            if (!IsConsumable()) return true;
            
            if (!battleActionData.GetSender().TryGetComponent(out Knapsack knapsack)) { return true; }
            knapsack.RemoveItem(this, false);
            knapsack.SquishItemsInKnapsack();
            return true;
        }

        public void SetTargets(TargetingNavigationType targetingNavigationType, BattleActionData battleActionData, IEnumerable<BattleEntity> activeCharacters, IEnumerable<BattleEntity> activeEnemies)
        {
            if (battleAction == null) { return; }
            battleAction.SetTargets(targetingNavigationType, battleActionData, activeCharacters, activeEnemies);
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frankie.Combat;

namespace Frankie.Inventory
{
    [CreateAssetMenu(menuName = ("Inventory/Action Item"))]
    public class ActionItem : InventoryItem, IBattleActionSuper
    {
        // Config Data
        [SerializeField] bool consumable = true;
        [SerializeField] BattleAction battleAction = null;

        public bool IsConsumable()
        {
            return consumable;
        }

        public bool Use(BattleActionData battleActionData, Action finished)
        {
            if (battleAction == null) { return false; }

            battleAction.Use(battleActionData, finished);

            if (IsConsumable())
            {
                if (!battleActionData.GetSender().TryGetComponent(out Knapsack knapsack)) { return true; }
                knapsack.RemoveItem(this, false);
                knapsack.SquishItemsInKnapsack();
            }
            return true;
        }

        public void GetTargets(bool? traverseForward, BattleActionData battleActionData,
            IEnumerable<BattleEntity> activeCharacters, IEnumerable<BattleEntity> activeEnemies)
        {
            if (battleAction == null) { return; }

            battleAction.GetTargets(traverseForward, battleActionData, activeCharacters, activeEnemies);
        }

        public bool IsItem()
        {
            return true;
        }

        public string GetName()
        {
            return GetItemNamePretty(name);
        }
    }

}

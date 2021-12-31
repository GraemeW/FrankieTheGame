using Frankie.Combat;
using Frankie.Stats;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Inventory
{
    [CreateAssetMenu(menuName = ("Inventory/Action Item"))]
    public class ActionItem : InventoryItem, IBattleActionUser
    {
        // Config Data
        [SerializeField] bool consumable = true;
        [SerializeField] BattleAction battleAction = null;

        public bool IsConsumable()
        {
            return consumable;
        }

        public bool Use(CombatParticipant sender, IEnumerable<CombatParticipant> recipients, Action finished)
        {
            if (battleAction == null) { return false; }

            battleAction.Use(sender, recipients, finished);

            if (IsConsumable())
            {
                if (!sender.TryGetComponent(out Knapsack knapsack)) { return true; }
                knapsack.RemoveItem(this, false);
                knapsack.SquishItemsInKnapsack();
            }
            return true;
        }

        public IEnumerable<CombatParticipant> GetTargets(bool? traverseForward, IEnumerable<CombatParticipant> currentTargets, IEnumerable<CombatParticipant> activeCharacters, IEnumerable<CombatParticipant> activeEnemies)
        {
            if (battleAction == null) { yield break; }

            foreach(CombatParticipant combatParticipant in battleAction.GetTargets(traverseForward, currentTargets, activeCharacters, activeEnemies))
            {
                yield return combatParticipant;
            }
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

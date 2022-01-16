using Frankie.Combat;
using Frankie.Stats;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

        public List<CombatParticipant> GetTargets(bool? traverseForward, IEnumerable<CombatParticipant> currentTargets, IEnumerable<CombatParticipant> activeCharacters, IEnumerable<CombatParticipant> activeEnemies)
        {
            List<CombatParticipant> targets = new List<CombatParticipant>();
            if (battleAction == null) { return targets; }

            foreach (CombatParticipant target in battleAction.GetTargets(traverseForward, currentTargets, activeCharacters, activeEnemies))
            {
                targets.Add(target);
            }
            return targets;
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

using Frankie.Combat;
using Frankie.Inventory;
using Frankie.Stats;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Control.Specialization
{
    public class WorldItemGiverTaker : MonoBehaviour
    {
        [SerializeField] InventoryItem inventoryItem = null;

        public void GiveItem(PlayerStateHandler playerStateHandler) // Called via Unity events
        {
            if (inventoryItem == null) { return; }

            Party party = playerStateHandler.GetParty();

            foreach (CombatParticipant character in party.GetParty())
            {
                Knapsack knapsack = character.GetKnapsack();
                if (knapsack.AddToFirstEmptySlot(inventoryItem))
                {
                    break;
                }
            }
        }
    }

}

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
        [SerializeField] string messageFoundItem = "Wow!  Looks like {0} found {1}";
        [SerializeField] string messageInventoryFull = "Whoops, looks like everyones' knapsacks are full.";

        public void GiveItem(PlayerStateHandler playerStateHandler) // Called via Unity events
        {
            if (inventoryItem == null) { return; }

            Party party = playerStateHandler.GetParty();

            foreach (CombatParticipant character in party.GetParty())
            {
                Knapsack knapsack = character.GetKnapsack();
                if (knapsack.AddToFirstEmptySlot(inventoryItem, true))
                {
                    playerStateHandler.OpenSimpleDialogue(string.Format(messageFoundItem, character.GetCombatName(), inventoryItem.GetDisplayName()));
                    return;
                }
            }

            playerStateHandler.OpenSimpleDialogue(messageInventoryFull);
        }
    }

}

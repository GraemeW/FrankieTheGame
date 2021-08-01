using Frankie.Combat;
using Frankie.Inventory;
using Frankie.Saving;
using Frankie.Stats;
using Frankie.Utils;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Control.Specialization
{
    public class WorldItemGiverTaker : MonoBehaviour, ISaveable
    {
        // Tunables
        [SerializeField] InventoryItem inventoryItem = null;
        [SerializeField] int itemQuantity = 1;
        [SerializeField] string messageFoundItem = "Wow!  Looks like {0} found {1}";
        [SerializeField] string messageInventoryFull = "Whoops, looks like everyones' knapsacks are full.";
        [SerializeField] string messageNothing = "Oh, looks like it's NOTHING";

        // State
        LazyValue<int> currentItemQuantity;

        private void Awake()
        {
            currentItemQuantity = new LazyValue<int>(GetMaxItemQuantity);
        }

        private void Start()
        {
            currentItemQuantity.ForceInit();
        }

        public void GiveItem(PlayerStateHandler playerStateHandler) // Called via Unity events
        {
            if (inventoryItem == null) { return; }
            if (currentItemQuantity.value <= 0)
            {
                playerStateHandler.OpenSimpleDialogue(messageNothing);
                return; 
            }

            Party party = playerStateHandler.GetParty();

            foreach (CombatParticipant character in party.GetParty())
            {
                Knapsack knapsack = character.GetKnapsack();
                if (knapsack.AddToFirstEmptySlot(inventoryItem, true))
                {
                    currentItemQuantity.value--;
                    playerStateHandler.OpenSimpleDialogue(string.Format(messageFoundItem, character.GetCombatName(), inventoryItem.GetDisplayName()));
                    return;
                }
            }

            playerStateHandler.OpenSimpleDialogue(messageInventoryFull);
        }

        private int GetMaxItemQuantity()
        {
            return itemQuantity;
        }

        public LoadPriority GetLoadPriority()
        {
            throw new System.NotImplementedException();
        }

        public SaveState CaptureState()
        {
            SaveState saveState = new SaveState(LoadPriority.ObjectProperty, currentItemQuantity.value);
            return saveState;
        }

        public void RestoreState(SaveState state)
        {
            currentItemQuantity.value = (int)state.GetState();
        }
    }

}

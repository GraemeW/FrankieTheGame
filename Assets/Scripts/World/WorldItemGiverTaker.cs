using Frankie.Combat;
using Frankie.Inventory;
using Frankie.Saving;
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
        [SerializeField][Tooltip("{0} for character name, {1} for item")] string messageFoundItem = "Wow!  Looks like {0} found {1}.";
        [SerializeField] string messageInventoryFull = "Whoops, looks like everyones' knapsacks are full.";
        [SerializeField] bool announceNothing = true;
        [SerializeField] string messageNothing = "Oh, looks like it's NOTHING.";

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

        public void GiveItem(PlayerStateMachine playerStateHandler) // Called via Unity events
        {
            if (inventoryItem == null) { return; }
            if (currentItemQuantity.value <= 0)
            {
                if (announceNothing) { playerStateHandler.EnterDialogue(messageNothing); }
                return; 
            }

            PartyKnapsackConduit partyKnapsackConduit = playerStateHandler.GetComponent<PartyKnapsackConduit>();
            CombatParticipant receivingCharacter = partyKnapsackConduit.AddToFirstEmptyPartySlot(inventoryItem);

            if (receivingCharacter != null)
            {
                currentItemQuantity.value--;
                playerStateHandler.EnterDialogue(string.Format(messageFoundItem, receivingCharacter.GetCombatName(), inventoryItem.GetDisplayName()));
                return;
            }

            // Failsafe --> full inventory
            playerStateHandler.EnterDialogue(messageInventoryFull);
        }

        public void TakeItem(PlayerStateMachine playerStateHandler) // Called via Unity events
        {
            if (inventoryItem == null) { return; }

            if (playerStateHandler.TryGetComponent(out PartyKnapsackConduit partyKnapsackConduit))
            {
                partyKnapsackConduit.RemoveSingleItem(inventoryItem);
            }
        }

        public void TakeAllItems(PlayerStateMachine playerStateHandler) // Called via Unity events
        {
            if (inventoryItem == null) { return; }

            if (playerStateHandler.TryGetComponent(out PartyKnapsackConduit partyKnapsackConduit))
            {
                partyKnapsackConduit.RemoveAllItems(inventoryItem);
            }
        }

        private int GetMaxItemQuantity() => itemQuantity;

        public LoadPriority GetLoadPriority()
        {
            return LoadPriority.ObjectProperty;
        }

        public SaveState CaptureState()
        {
            if (currentItemQuantity == null) { Awake(); }
            SaveState saveState = new SaveState(LoadPriority.ObjectProperty, currentItemQuantity.value);
            return saveState;
        }

        public void RestoreState(SaveState state)
        {
            if (currentItemQuantity == null) { Awake(); }
            currentItemQuantity.value = (int)state.GetState(typeof(int));
        }
    }

}

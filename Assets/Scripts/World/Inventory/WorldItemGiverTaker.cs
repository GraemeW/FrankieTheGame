using UnityEngine;
using Frankie.Control;
using Frankie.Saving;
using Frankie.Combat;
using Frankie.Inventory;
using Frankie.Utils;

namespace Frankie.World
{
    public class WorldItemGiverTaker : MonoBehaviour, ISaveable
    {
        // Tunables
        [SerializeField] private InventoryItem inventoryItem;
        [SerializeField] private int itemQuantity = 1;
        [SerializeField][Tooltip("{0} for character name, {1} for item")] private string messageFoundItem = "Wow!  Looks like {0} found {1}.";
        [SerializeField] private string messageInventoryFull = "Whoops, looks like all the knapsacks are full.";
        [SerializeField] private bool announceNothing = true;
        [SerializeField] private string messageNothing = "Oh, looks like it's NOTHING.";
        [SerializeField] private InteractionEvent itemFound;

        // State
        private LazyValue<int> currentItemQuantity;

        private void Awake()
        {
            currentItemQuantity = new LazyValue<int>(GetMaxItemQuantity);
        }

        private void Start()
        {
            currentItemQuantity.ForceInit();
        }

        public void GiveItem(PlayerStateMachine playerStateMachine) // Called via Unity events
        {
            if (inventoryItem == null || currentItemQuantity.value <= 0)
            {
                if (announceNothing) { playerStateMachine.EnterDialogue(messageNothing); }
                return; 
            }

            var partyKnapsackConduit = playerStateMachine.GetComponent<PartyKnapsackConduit>();
            if (partyKnapsackConduit == null) { return; }
            
            if (!partyKnapsackConduit.AddToFirstEmptyPartySlot(inventoryItem, out CombatParticipant receivingCharacter))
            {
                playerStateMachine.EnterDialogue(messageInventoryFull);
                return;
            }
            
            currentItemQuantity.value--;
            playerStateMachine.EnterDialogue(string.Format(messageFoundItem, receivingCharacter.GetCombatName(), inventoryItem.GetDisplayName()));
            itemFound?.Invoke(playerStateMachine);
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
            currentItemQuantity ??= new LazyValue<int>(GetMaxItemQuantity);
            var saveState = new SaveState(LoadPriority.ObjectProperty, currentItemQuantity.value);
            return saveState;
        }

        public void RestoreState(SaveState state)
        {
            currentItemQuantity ??= new LazyValue<int>(GetMaxItemQuantity);
            currentItemQuantity.value = (int)state.GetState(typeof(int));
        }
    }
}

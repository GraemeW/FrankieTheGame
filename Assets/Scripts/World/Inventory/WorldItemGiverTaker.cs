using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using Frankie.Core.GameStateModifiers;
using Frankie.Control;
using Frankie.Saving;
using Frankie.Combat;
using Frankie.Inventory;
using Frankie.Utils;
using Frankie.Utils.Localization;

namespace Frankie.World
{
    [ExecuteInEditMode]
    public class WorldItemGiverTaker : MonoBehaviour, IGameStateModifierHandler, ISaveable, ILocalizable
    {
        // GameState Modifier Properties
        [SerializeField] private string backingHandlerGUID;
        public string handlerGUID { get => backingHandlerGUID; set => backingHandlerGUID = value; }
        
        [SerializeField][HideInInspector] private int backingListHashCheck;
        public int modifierListHashCheck { get => backingListHashCheck; set => backingListHashCheck = value; }
        
        [SerializeField][HideInInspector] private bool backingHasGameStateModifiers;
        public bool hasGameStateModifiers { get => backingHasGameStateModifiers; set => backingHasGameStateModifiers = value; }
        
        [SerializeField][HideInInspector] private List<string> backingGameStateModifierGUIDs;
        public List<string> gameStateModifierGUIDs { get => backingGameStateModifierGUIDs; set => backingGameStateModifierGUIDs = value ?? new List<string>(); } 
        
        // Tunables
        [SerializeField] private InventoryItem inventoryItem;
        [SerializeField] private int itemQuantity = 1;
        [SerializeField] private bool announceNothing = true;
        [SerializeField] private InteractionEvent itemFound;
        [Header("Messages - {0}: name, {1}: item")]
        [SerializeField][SimpleLocalizedString(LocalizationTableType.ChecksWorldObjects, true)] private LocalizedString localizedMessageFoundItem;
        [Header("Other Messages")]
        [SerializeField][SimpleLocalizedString(LocalizationTableType.ChecksWorldObjects, true)] private LocalizedString localizedMessageInventoryFull;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.ChecksWorldObjects, true)] private LocalizedString localizedMessageNothing;
        
        // Localization Properties
        public LocalizationTableType localizationTableType { get; } = LocalizationTableType.ChecksWorldObjects;
        
        // State
        private LazyValue<int> currentItemQuantity;

        #region UnityMethods
        private void Awake()
        {
            currentItemQuantity = new LazyValue<int>(GetMaxItemQuantity);
        }
        
        private int GetMaxItemQuantity() => itemQuantity;

        private void Start()
        {
            currentItemQuantity.ForceInit();
        }

        private void OnDestroy()
        {
            ILocalizable.TriggerOnDestroy(this);
            IGameStateModifierHandler.TriggerOnDestroy(this);
        }
        
        private void OnDrawGizmos()
        {
            IGameStateModifierHandler.TriggerOnGizmos(this);
        }
        #endregion

        #region PublicMethods
        public void GiveItem(PlayerStateMachine playerStateMachine) // Called via Unity events
        {
            if (inventoryItem == null || currentItemQuantity.value <= 0)
            {
                if (announceNothing) { playerStateMachine.EnterDialogue(localizedMessageNothing.GetSafeLocalizedString()); }
                return; 
            }

            var partyKnapsackConduit = playerStateMachine.GetComponent<PartyKnapsackConduit>();
            if (partyKnapsackConduit == null) { return; }
            
            if (!partyKnapsackConduit.AddToFirstEmptyPartySlot(inventoryItem, out CombatParticipant receivingCharacter))
            {
                playerStateMachine.EnterDialogue(localizedMessageInventoryFull.GetSafeLocalizedString());
                return;
            }
            
            currentItemQuantity.value--;
            playerStateMachine.EnterDialogue(string.Format(localizedMessageFoundItem.GetSafeLocalizedString(), receivingCharacter.GetCombatName(), inventoryItem.GetDisplayName()));
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
        #endregion

        #region LocalizationInterface
        public List<TableEntryReference> GetLocalizationEntries()
        {
            return new List<TableEntryReference>
            {
                localizedMessageFoundItem.TableEntryReference,
                localizedMessageInventoryFull.TableEntryReference,
                localizedMessageNothing.TableEntryReference,
            };
        }
        #endregion
        
        #region GameStateModifierHandlerInterface
        public IList<GameStateModifier> GetGameStateModifiers()
        {
            List<GameStateModifier> gameStateModifiers = new();
            var keyItem = inventoryItem as KeyItem;
            if (keyItem != null)  { gameStateModifiers.Add(keyItem); }
            return gameStateModifiers;
        }
        #endregion
        
        #region SaveInterface
        public LoadPriority GetLoadPriority() => LoadPriority.ObjectProperty;

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
        #endregion
    }
}

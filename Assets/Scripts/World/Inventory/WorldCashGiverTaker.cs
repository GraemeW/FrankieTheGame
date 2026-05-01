using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using Frankie.Control;
using Frankie.Saving;
using Frankie.Inventory;
using Frankie.Stats;
using Frankie.Utils;

namespace Frankie.World
{
    [ExecuteInEditMode]
    public class WorldCashGiverTaker : MonoBehaviour, ISaveable, ILocalizable
    {
        // Localization Properties
        public LocalizationTableType localizationTableType { get; } = LocalizationTableType.ChecksWorldObjects;
        
        // Tunables
        [Header("Configuration")]
        [SerializeField] private int transactionCash = 10;
        [SerializeField] private bool infiniteTransactions = false;
        [SerializeField][Tooltip("Ignored if infiniteTransactions set to true")][Min(1)] private int numberTransactions = 1;
        [SerializeField] private bool announceNothing = true;
        [SerializeField] private InteractionEvent transactionSuccessful;
        [Header("Messages - {0}: name, {1}: cash qty")]
        [SerializeField][SimpleLocalizedString(LocalizationTableType.ChecksWorldObjects, true)] private LocalizedString localizedMessageTransactionPositive;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.ChecksWorldObjects, true)] private LocalizedString localizedMessageTransactionNegative;
        [Header("Messages - {0}: name")]
        [SerializeField][SimpleLocalizedString(LocalizationTableType.ChecksWorldObjects, true)] private LocalizedString localizedMessageWalletFull;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.ChecksWorldObjects, true)] private LocalizedString localizedMessageWalletEmpty;
        [Header("Other Messages")]
        [SerializeField][SimpleLocalizedString(LocalizationTableType.ChecksWorldObjects, true)] private LocalizedString localizedMessageNothing;

        // State
        private LazyValue<int> numberTransactionsLeft;

        #region UnityMethods
        private void Awake()
        {
            numberTransactionsLeft = new LazyValue<int>(GetInitialTransactionCount);
        }
        private int GetInitialTransactionCount() => numberTransactions;

        private void Start()
        {
            numberTransactionsLeft.ForceInit();
        }
        
        private void OnDestroy()
        {
            ILocalizable.TriggerOnDestroy(this);
        }
        #endregion

        #region PublicMethods
        public void ConductTransaction(PlayerStateMachine playerStateHandler) // Called by Unity Events
        {
            if (IsNothingLeft(playerStateHandler)) { return; }

            var wallet = playerStateHandler.GetComponent<Wallet>();
            var party = playerStateHandler.GetComponent<Party>();
            string partyLeaderName = party.GetPartyLeaderName();

            if (IsWalletFullOrEmpty(playerStateHandler, wallet, partyLeaderName)) { return; }

            switch (transactionCash)
            {
                case > 0:
                    playerStateHandler.EnterDialogue(string.Format(localizedMessageTransactionPositive.GetSafeLocalizedString(), partyLeaderName, transactionCash.ToString()));
                    break;
                case < 0:
                    playerStateHandler.EnterDialogue(string.Format(localizedMessageTransactionNegative.GetSafeLocalizedString(), partyLeaderName, Mathf.Abs(transactionCash).ToString()));
                    break;
            }
            wallet.UpdateCash(transactionCash);
            if (!infiniteTransactions) { numberTransactionsLeft.value--; }
            transactionSuccessful?.Invoke(playerStateHandler);
        }
        #endregion

        #region PrivateMethods
        private bool IsNothingLeft(PlayerStateMachine playerStateHandler)
        {
            if (transactionCash == 0) { return true; }
            if (infiniteTransactions || numberTransactionsLeft.value > 0) { return false; }
            
            if (announceNothing) { playerStateHandler.EnterDialogue(localizedMessageNothing.GetSafeLocalizedString()); }
            return true;
        }

        private bool IsWalletFullOrEmpty(PlayerStateMachine playerStateHandler, Wallet wallet, string recipient)
        {
            switch (transactionCash)
            {
                case > 0 when wallet.IsWalletFull():
                    playerStateHandler.EnterDialogue(string.Format(localizedMessageWalletFull.GetSafeLocalizedString(), recipient));
                    return true;
                case < 0 when wallet.IsWalletEmpty():
                    playerStateHandler.EnterDialogue(string.Format(localizedMessageWalletFull.GetSafeLocalizedString(), recipient));
                    return true;
                default:
                    return false;
            }
        }
        #endregion
        
        #region LocalizationInterface
        public List<TableEntryReference> GetLocalizationEntries()
        {
            return new List<TableEntryReference>
            {
                localizedMessageTransactionPositive.TableEntryReference,
                localizedMessageTransactionNegative.TableEntryReference,
                localizedMessageWalletFull.TableEntryReference,
                localizedMessageWalletEmpty.TableEntryReference,
                localizedMessageNothing.TableEntryReference
            };
        }
        #endregion

        #region SaveImplementation
        public LoadPriority GetLoadPriority() => LoadPriority.ObjectProperty; 

        public SaveState CaptureState()
        {
            var saveState = new SaveState(GetLoadPriority(), numberTransactionsLeft.value);
            return saveState;
        }

        public void RestoreState(SaveState state)
        {
            numberTransactionsLeft.value = (int)state.GetState(typeof(int));
        }
        #endregion
    }
}

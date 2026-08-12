using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using LowDefMustard.Saving;
using LowDefMustard.Utils;
using LowDefMustard.Localization;
using Frankie.Core;
using Frankie.Control;
using Frankie.Inventory;
using Frankie.Stats;

namespace Frankie.World
{
    [ExecuteInEditMode]
    public class WorldCashGiverTaker : MonoBehaviour, ISaveable<int>, ILocalizable
    {
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

        // Localization Properties
        public LocalizationTableType localizationTableType { get; } = LocalizationTableType.ChecksWorldObjects;
        
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
#if UNITY_EDITOR
            numberTransactionsLeft ??= new LazyValue<int>(GetInitialTransactionCount); // [ExecuteInEditMode] can result in weird Unity state, so safety re-check/set
#endif
            
            numberTransactionsLeft.ForceInit();
        }
        
        private void OnDestroy()
        {
            ILocalizable.TriggerOnDestroy(this);
        }
        #endregion

        #region PublicMethods
        public void ConductTransaction(PlayerStateMachine playerStateMachine) // Called by Unity Events
        {
            if (IsNothingLeft(playerStateMachine)) { return; }

            var wallet = playerStateMachine.GetComponent<Wallet>();
            var party = playerStateMachine.GetComponent<Party>();
            string partyLeaderName = party.GetPartyLeaderName();

            if (IsWalletFullOrEmpty(playerStateMachine, wallet, partyLeaderName)) { return; }

            switch (transactionCash)
            {
                case > 0:
                    playerStateMachine.EnterDialogue(string.Format(localizedMessageTransactionPositive.GetSafeLocalizedString(), partyLeaderName, transactionCash.ToString(CultureInfo.InvariantCulture)));
                    break;
                case < 0:
                    playerStateMachine.EnterDialogue(string.Format(localizedMessageTransactionNegative.GetSafeLocalizedString(), partyLeaderName, Mathf.Abs(transactionCash).ToString(CultureInfo.InvariantCulture)));
                    break;
            }
            wallet.UpdateCash(transactionCash);
            if (!infiniteTransactions) { numberTransactionsLeft.value--; }
            transactionSuccessful?.Invoke(playerStateMachine);
        }
        #endregion

        #region PrivateMethods
        private bool IsNothingLeft(PlayerStateMachine playerStateMachine)
        {
            if (transactionCash == 0) { return true; }
            if (infiniteTransactions || numberTransactionsLeft.value > 0) { return false; }
            
            if (announceNothing) { playerStateMachine.EnterDialogue(localizedMessageNothing.GetSafeLocalizedString()); }
            return true;
        }

        private bool IsWalletFullOrEmpty(PlayerStateMachine playerStateMachine, Wallet wallet, string recipient)
        {
            switch (transactionCash)
            {
                case > 0 when wallet.IsWalletFull():
                    playerStateMachine.EnterDialogue(string.Format(localizedMessageWalletFull.GetSafeLocalizedString(), recipient));
                    return true;
                case < 0 when wallet.IsWalletEmpty():
                    playerStateMachine.EnterDialogue(string.Format(localizedMessageWalletFull.GetSafeLocalizedString(), recipient));
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
            numberTransactionsLeft ??= new LazyValue<int>(GetInitialTransactionCount);
            return new SaveState(GetLoadPriority(), numberTransactionsLeft.value);
        }

        public void RestoreState(SaveState saveState)
        {
            numberTransactionsLeft ??= new LazyValue<int>(GetInitialTransactionCount);
            if (TryManualGetDataFromState(saveState, out int value)) { numberTransactionsLeft.value = value; }
        }
        #endregion

        public SaveState ManualGetStateFromData(int data)
        {
            if (data < 0) { data = GetInitialTransactionCount();}
            return new SaveState(GetLoadPriority(), data);
        }

        public bool TryManualGetDataFromState(SaveState saveState, out int value)
        {
            if (saveState != null && saveState.TryGetState(out value))
            {
                value = value >= 0 ? value : GetInitialTransactionCount();
                return true;
            }
            value = GetInitialTransactionCount();
            return true;
        }
    }
}

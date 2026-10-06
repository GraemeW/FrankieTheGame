using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using LowDefMustard.Control;
using LowDefMustard.UIBox;
using LowDefMustard.Utils;
using LowDefMustard.Localization;
using Frankie.Core;
using Frankie.Control;
using Frankie.World;
using Frankie.Utils.Localization;

namespace Frankie.Inventory.UI
{
    [RequireComponent(typeof(UIToolkitMenuView))]
    public sealed class CashTransferBox : UIBox<UIBoxState>, ILocalizable
    {
        // Tunables
        [Header("Text")]
        [Header("Include {0} for funds amount")]
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedMessageDeposit;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedMessageWithdraw;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedConfirmChoiceAffirmative;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedConfirmChoiceNegative;
        [Header("Prefabs")]
        [SerializeField] private WalletUI walletUIPrefab;

        // State
        private readonly CashTransferModel cashTransferModel = new();
        private readonly List<IUIChoice> confirmChoices = new();
        private CashTransferState cashTransferState = CashTransferState.CashSelection;
        private CashAmountHandle cashAmountHandle;

        // Cached References
        private UIToolkitMenuView menuView;
        private WorldCanvas worldCanvas;
        private PlayerStateMachine playerStateMachine;
        private PlayerController playerController;
        private Shopper shopper;
        private Wallet wallet;
        private WalletUI walletUI;

        // Static
        private const int _maxTransferAmount = 999999999;

        // UIBox Configuration
        protected override EnumLookup<UIBoxState,UIBoxStateBehaviour> BuildStateBehaviours()
        {
            var cashTransferConfiguration = new EnumLookup<UIBoxState,UIBoxStateBehaviour>();
            var defaultStateBehaviour = new UIBoxStateBehaviour(
                choose: ImplementChoose,
                tryHandleBackNavigation: ImplementTryHandleBackNavigation
            );
            cashTransferConfiguration.TrySet(UIBoxState.Default, defaultStateBehaviour);
            return cashTransferConfiguration;
        }

        #region UnityMethods
        protected override bool TryAcquireDependencies()
        {
            worldCanvas = WorldCanvas.FindWorldCanvas();
            playerStateMachine = Player.FindPlayerStateMachine();
            if (worldCanvas == null || playerStateMachine == null) { return false; }

            playerController = playerStateMachine.GetComponent<PlayerController>();
            shopper = playerStateMachine.GetComponent<Shopper>();
            wallet = playerStateMachine.GetComponent<Wallet>();
            if (playerController == null) { return false; }

            playerController.AddInputReceiver(this, null);
            return true;
        }

        protected override void AwakeTriggered()
        {
            clearVolatileOptionsOnEnable = false;
            menuView = GetComponent<UIToolkitMenuView>();
            menuView.SetDataSource(cashTransferModel);
        }

        protected override void StartTriggered()
        {
            walletUI = Instantiate(walletUIPrefab, worldCanvas.transform);
            SetupCashTransferBoxUI();
        }

        protected override void DestroyTriggered()
        {
            if (walletUI != null) { Destroy(walletUI.gameObject); }
            playerStateMachine?.EnterWorld();
        }
        #endregion

        #region LocalizationMethods
        public LocalizationTableType localizationTableType { get; } = LocalizationTableType.UI;
        public List<TableEntryReference> GetLocalizationEntries()
        {
            // Note:  Confirm choices re-use localization keys from StandardConfirmationMenu (not returned, to prevent deletion of its keys)
            return new List<TableEntryReference>
            {
                localizedMessageDeposit.TableEntryReference,
                localizedMessageWithdraw.TableEntryReference,
            };
        }
        #endregion

        #region Initialization
        private void SetupCashTransferBoxUI()
        {
            switch (shopper.GetBankType())
            {
                case BankType.Deposit:
                    InitializeTransfer(wallet.GetCash(), localizedMessageDeposit, -1);
                    break;
                case BankType.Withdraw:
                    InitializeTransfer(wallet.GetPendingCash(), localizedMessageWithdraw, 1);
                    break;
                default:
                    Destroy(gameObject);
                    break;
            }
        }

        private void InitializeTransfer(int amountAvailable, LocalizedString localizedMessage, int walletTransferSign)
        {
            cashTransferModel.messageText = string.Format(localizedMessage.GetSafeLocalizedString(), $"${amountAvailable:N0}");

            cashAmountHandle = new CashAmountHandle(menuView, Mathf.Min(amountAvailable, _maxTransferAmount), HandleDigitClicked);
            menuView.AddEntry(cashAmountHandle);
            confirmChoices.Add(AddChoiceOption(localizedConfirmChoiceAffirmative.GetSafeLocalizedString(), () => wallet.TransferToWallet(walletTransferSign * cashAmountHandle.amount)));
            confirmChoices.Add(AddChoiceOption(localizedConfirmChoiceNegative.GetSafeLocalizedString(), null));

            SetCashTransferState(CashTransferState.CashSelection);
        }
        #endregion

        #region UIBoxInterfaceMethods
        private bool ImplementChoose(string nodeID)
        {
            switch (cashTransferState)
            {
                case CashTransferState.CashSelection:
                    SetCashTransferState(CashTransferState.CashConfirmation);
                    return true;
                case CashTransferState.CashConfirmation:
                    return StandardChoose(null);
                default:
                    return false;
            }
        }

        private bool ImplementTryHandleBackNavigation(ControllerInputType controllerInputType)
        {
            if (cashTransferState != CashTransferState.CashConfirmation) { return false; }
            SetCashTransferState(CashTransferState.CashSelection);
            return true;
        }
        #endregion

        #region PrivateMethods
        private void SetCashTransferState(CashTransferState setCashTransferState)
        {
            cashTransferState = setCashTransferState;
            ClearChoiceSelections();
            choiceOptions.Clear();
            switch (setCashTransferState)
            {
                case CashTransferState.CashConfirmation:
                    choiceOptions.AddRange(confirmChoices);
                    break;
                case CashTransferState.CashSelection:
                    choiceOptions.Add(cashAmountHandle);
                    break;
            }
            ShowCursorOnAnyInteraction(ControllerInputType.NavigateRight);
        }

        private void HandleDigitClicked()
        {
            if (cashTransferState != CashTransferState.CashSelection) { SetCashTransferState(CashTransferState.CashSelection); }
            else { SetHighlightedChoice(cashAmountHandle); }
        }
        #endregion
    }
}

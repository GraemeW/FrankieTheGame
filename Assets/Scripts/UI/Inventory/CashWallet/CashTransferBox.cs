using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using TMPro;
using Frankie.Core;
using Frankie.Control;
using Frankie.World;
using Frankie.Utils;
using Frankie.Utils.UI;
using Frankie.Utils.Localization;

namespace Frankie.Inventory.UI
{
    public class CashTransferBox : UIBox, ILocalizable
    {
        // Tunables
        [Header("Text")]
        [Header("Include {0} for funds amount")] 
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedMessageDeposit;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedMessageWithdraw;
        [Header("Cash Transfer Hookups")]
        [SerializeField] private TMP_Text messageField;
        [SerializeField] private CashTransferField hundredMillionField;
        [SerializeField] private CashTransferField tenMillionField;
        [SerializeField] private CashTransferField millionField;
        [SerializeField] private CashTransferField hundredThousandField;
        [SerializeField] private CashTransferField tenThousandField;
        [SerializeField] private CashTransferField thousandField;
        [SerializeField] private CashTransferField hundredField;
        [SerializeField] private CashTransferField tenField;
        [SerializeField] private CashTransferField oneField;
        [SerializeField] private UIChoiceButton confirmField;
        [SerializeField] private UIChoiceButton rejectField;
        [Header("Prefabs")]
        [SerializeField] private WalletUI walletUIPrefab;
        
        // State
        private CashTransferState cashTransferState = CashTransferState.CashSelection;
        private int amountAvailable = 0;
        private int amountToTransfer = 0;

        // Cached References
        private WorldCanvas worldCanvas;
        private PlayerStateMachine playerStateMachine;
        private PlayerController playerController;
        private Shopper shopper;
        private Wallet wallet;
        private WalletUI walletUI;

        // Static
        private const int _maxTransferAmount = 999999999;
        
        // UIBox Configuration
        protected override EnumLookupBase<UIBoxStateBehaviour> BuildStateBehaviours()
        {
            var cashTransferConfiguration = new EnumLookup<UIBoxState,UIBoxStateBehaviour>();
            var defaultStateBehaviour = new UIBoxStateBehaviour(
                moveCursor: ImplementMoveCursor,
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
        }

        protected override void StartTriggered()
        {
            SetupWalletUI();
            SetupCashTransferBoxUI();
        }

        private void SetupWalletUI()
        {
            walletUI = Instantiate(walletUIPrefab, worldCanvas.transform);
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
            BankType bankType = shopper.GetBankType();
            switch (bankType)
            {
                case BankType.Deposit:
                {
                    amountAvailable = wallet.GetCash();
                    amountToTransfer = 0;

                    messageField.text = string.Format(localizedMessageDeposit.GetSafeLocalizedString(), $"${amountAvailable:N0}");
                    InitializeButtons(() =>
                    {
                        wallet.TransferToWallet(-GetPendingCashToTransfer());
                        Destroy(gameObject);
                    });
                    break;
                }
                case BankType.Withdraw:
                {
                    amountAvailable = wallet.GetPendingCash();
                    amountToTransfer = 0;

                    messageField.text = string.Format(localizedMessageWithdraw.GetSafeLocalizedString(), $"${amountAvailable:N0}");
                    InitializeButtons(() =>
                    {
                        wallet.TransferToWallet(GetPendingCashToTransfer());
                        Destroy(gameObject);
                    });
                    break;
                }
                default:
                {
                    Destroy(gameObject);
                    break;
                }
            }
        }

        private void InitializeButtons(Action actionOnConfirm)
        {
            SetCashTransferState(CashTransferState.CashSelection);
            foreach (UIChoiceButton choiceButton in choiceOptions.OfType<UIChoiceButton>())
            {
                choiceButton.AddOnClickListener(() => SelectField(choiceButton));
            }
            RefreshFieldsToTransferAmount();

            if (actionOnConfirm != null && confirmField != null) { confirmField.AddOnClickListener(actionOnConfirm.Invoke); }
            if (rejectField) { rejectField.AddOnClickListener(() => Destroy(gameObject)); }
            SelectField(oneField);
        }
        #endregion

        #region UIBoxInterfaceMethods
        private bool ImplementMoveCursor(ControllerInputType controllerInputType, CursorMovementStyle cursorMovementStyle)
        {
            if (cashTransferState == CashTransferState.CashSelection)
            {
                if (controllerInputType is ControllerInputType.NavigateDown or ControllerInputType.NavigateUp)
                {
                    return AdjustNumber(controllerInputType);
                }
            }
            return StandardMoveCursor(controllerInputType, cursorMovementStyle);
        }

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
        private int GetPendingCashToTransfer() => amountToTransfer;

        private void SetCashTransferState(CashTransferState setCashTransferState)
        {
            cashTransferState = setCashTransferState;
            ClearChoiceSelections();
            switch (setCashTransferState)
            {
                case CashTransferState.CashConfirmation:
                {
                    choiceOptions.Clear();
                    choiceOptions.AddRange(new[] { confirmField, rejectField });
                    break;
                }
                case CashTransferState.CashSelection:
                {
                    choiceOptions.Clear();
                    choiceOptions.AddRange(new[]
                    {
                        hundredMillionField, tenMillionField, millionField,
                        hundredThousandField, tenThousandField, thousandField,
                        hundredField, tenField, oneField
                    });
                    break;
                }
            }
            ShowCursorOnAnyInteraction(ControllerInputType.NavigateRight);
        }

        private void SelectField(UIChoiceButton choiceOption)
        {
            ClearChoiceSelections();
            choiceOption.Highlight(true);
            highlightedChoiceOption = choiceOption;
        }

        private bool AdjustNumber(ControllerInputType controllerInputType)
        {
            if (controllerInputType is not (ControllerInputType.NavigateDown or ControllerInputType.NavigateUp)) { return false; }
            
            var cashTransferField = highlightedChoiceOption as CashTransferField;
            if (cashTransferField == null) { return false; }
            CashTransferFieldType cashTransferFieldType = cashTransferField.GetCashTransferFieldType();

            // Calculate adjusted value
            int modifier = 1;
            if (controllerInputType == ControllerInputType.NavigateDown) { modifier = -1; }
            modifier *= cashTransferFieldType switch
            {
                CashTransferFieldType.One => 1,
                CashTransferFieldType.Ten => 10,
                CashTransferFieldType.Hundred => 100,
                CashTransferFieldType.Thousand => 1000,
                CashTransferFieldType.TenThousand => 10000,
                CashTransferFieldType.HundredThousand => 100000,
                CashTransferFieldType.Million => 1000000,
                CashTransferFieldType.TenMillion => 10000000,
                CashTransferFieldType.HundredMillion => 100000000,
                _ => 0,
            };
            int modifiedAmount = Mathf.Clamp(amountToTransfer + modifier, 0, amountAvailable);
            modifiedAmount = Mathf.Min(modifiedAmount, _maxTransferAmount);
            
            amountToTransfer = modifiedAmount;
            RefreshFieldsToTransferAmount();
            return true;
        }

        private void RefreshFieldsToTransferAmount()
        {
            int workingNumber = amountToTransfer;
            oneField.SetText((workingNumber % 10).ToString(CultureInfo.InvariantCulture));
            workingNumber /= 10;
            tenField.SetText((workingNumber % 10).ToString(CultureInfo.InvariantCulture));
            workingNumber /= 10;
            hundredField.SetText((workingNumber % 10).ToString(CultureInfo.InvariantCulture));
            workingNumber /= 10;
            thousandField.SetText((workingNumber % 10).ToString(CultureInfo.InvariantCulture));
            workingNumber /= 10;
            tenThousandField.SetText((workingNumber % 10).ToString(CultureInfo.InvariantCulture));
            workingNumber /= 10;
            hundredThousandField.SetText((workingNumber % 10).ToString(CultureInfo.InvariantCulture));
            workingNumber /= 10;
            millionField.SetText((workingNumber % 10).ToString(CultureInfo.InvariantCulture));
            workingNumber /= 10;
            tenMillionField.SetText((workingNumber % 10).ToString(CultureInfo.InvariantCulture));
            workingNumber /= 10;
            hundredMillionField.SetText((workingNumber % 10).ToString(CultureInfo.InvariantCulture));
        }
        #endregion
    }
}

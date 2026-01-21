using System;
using System.Linq;
using UnityEngine;
using TMPro;
using Frankie.Core;
using Frankie.Control;
using Frankie.World;
using Frankie.Utils.UI;

namespace Frankie.Inventory.UI
{
    public class CashTransferBox : UIBox
    {
        // Tunables
        [Header("")]
        [SerializeField][Tooltip("Include {0} for funds amount")] private string messageDeposit = "You currently have {0} funds to deposit.  How much do you want to deposit?";
        [SerializeField][Tooltip("Include {0} for funds amount")] private string messageWithdraw = "You currently have {0} in the bank to withdraw.  How much do you want to take out?";
        [Header("Cash Transfer Fields")]
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
        [Header("Other Prefabs")]
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
        const int _maxTransferAmount = 999999999;

        #region UnityMethods
        private void Awake()
        {
            GetPlayerReference();
        }

        private void GetPlayerReference()
        {
            worldCanvas = WorldCanvas.FindWorldCanvas();
            playerStateMachine = Player.FindPlayerStateMachine();
            if (worldCanvas == null || playerStateMachine == null) { Destroy(gameObject); }

            playerController = playerStateMachine?.GetComponent<PlayerController>();
            shopper = playerStateMachine?.GetComponent<Shopper>();
            wallet = playerStateMachine?.GetComponent<Wallet>();
        }

        private void Start()
        {
            TakeControl(playerController, this, null); // input handled via player controller, immediate override
            HandleClientEntry();

            SetupWalletUI();
            SetupCashTransferBoxUI();
        }

        private void SetupWalletUI()
        {
            walletUI = Instantiate(walletUIPrefab, worldCanvas.transform);
        }

        private void OnDestroy()
        {
            if (walletUI != null) { Destroy(walletUI.gameObject); }

            HandleClientExit();
            playerStateMachine?.EnterWorld();
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

                    messageField.text = string.Format(messageDeposit, $"${amountAvailable:N0}");
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

                    messageField.text = string.Format(messageWithdraw, $"${amountAvailable:N0}");
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

            if (actionOnConfirm != null) { confirmField.AddOnClickListener(actionOnConfirm.Invoke); }
            rejectField.AddOnClickListener(() => Destroy(gameObject));
            SelectField(oneField);
        }
        #endregion

        #region UIBoxStandardInterface
        protected override bool MoveCursor(PlayerInputType playerInputType)
        {
            if (cashTransferState == CashTransferState.CashSelection)
            {
                if (playerInputType is PlayerInputType.NavigateDown or PlayerInputType.NavigateUp)
                {
                    return AdjustNumber(playerInputType);
                }
            }
            return base.MoveCursor(playerInputType);
        }

        protected override bool Choose(string nodeID)
        {
            switch (cashTransferState)
            {
                case CashTransferState.CashSelection:
                    SetCashTransferState(CashTransferState.CashConfirmation);
                    return true;
                case CashTransferState.CashConfirmation:
                    return base.Choose(null);
                default:
                    return false;
            }
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
            ShowCursorOnAnyInteraction(PlayerInputType.NavigateRight);
        }

        private void SelectField(UIChoiceButton choiceOption)
        {
            ClearChoiceSelections();
            choiceOption.Highlight(true);
            highlightedChoiceOption = choiceOption;
        }

        private bool AdjustNumber(PlayerInputType playerInputType)
        {
            if (playerInputType is PlayerInputType.NavigateDown or PlayerInputType.NavigateUp)
            {
                var cashTransferField = highlightedChoiceOption as CashTransferField;
                if (cashTransferField == null) { return false; }
                CashTransferFieldType cashTransferFieldType = cashTransferField.GetCashTransferFieldType();

                // Calculate adjusted value
                int modifier = 1;
                if (playerInputType == PlayerInputType.NavigateDown) { modifier = -1; }
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

                // Update cart
                amountToTransfer = modifiedAmount;

                // Update UI
                RefreshFieldsToTransferAmount();

                return true;
            }
            return false;
        }

        private void RefreshFieldsToTransferAmount()
        {
            int workingNumber = amountToTransfer;
            oneField.SetText((workingNumber % 10).ToString());
            workingNumber /= 10;
            tenField.SetText((workingNumber % 10).ToString());
            workingNumber /= 10;
            hundredField.SetText((workingNumber % 10).ToString());
            workingNumber /= 10;
            thousandField.SetText((workingNumber % 10).ToString());
            workingNumber /= 10;
            tenThousandField.SetText((workingNumber % 10).ToString());
            workingNumber /= 10;
            hundredThousandField.SetText((workingNumber % 10).ToString());
            workingNumber /= 10;
            millionField.SetText((workingNumber % 10).ToString());
            workingNumber /= 10;
            tenMillionField.SetText((workingNumber % 10).ToString());
            workingNumber /= 10;
            hundredMillionField.SetText((workingNumber % 10).ToString());
        }

        // Utility -- Deprecated, but maybe useful later
        private CashTransferField GetCashTransferField(CashTransferFieldType cashTransferFieldType)
        {
            return cashTransferFieldType switch
            {
                CashTransferFieldType.One => oneField,
                CashTransferFieldType.Ten => tenField,
                CashTransferFieldType.Hundred => hundredField,
                CashTransferFieldType.Thousand => thousandField,
                CashTransferFieldType.TenThousand => tenThousandField,
                CashTransferFieldType.HundredThousand => hundredThousandField,
                CashTransferFieldType.Million => millionField,
                CashTransferFieldType.TenMillion => tenMillionField,
                CashTransferFieldType.HundredMillion => hundredMillionField,
                _ => null,
            };
        }

        private int GetNumberForField(int wholeNumber, CashTransferFieldType cashTransferFieldType)
        {
            return cashTransferFieldType switch
            {
                CashTransferFieldType.One => wholeNumber % 10,
                CashTransferFieldType.Ten => ((wholeNumber / 10) % 10),
                CashTransferFieldType.Hundred => ((wholeNumber / 100) % 10),
                CashTransferFieldType.Thousand => ((wholeNumber / 1000) % 10),
                CashTransferFieldType.TenThousand => ((wholeNumber / 10000) % 10),
                CashTransferFieldType.HundredThousand => ((wholeNumber / 100000) % 10),
                CashTransferFieldType.Million => ((wholeNumber / 1000000) % 10),
                CashTransferFieldType.TenMillion => ((wholeNumber / 10000000) % 10),
                CashTransferFieldType.HundredMillion => ((wholeNumber / 100000000) % 10),
                _ => 0,
            };
        }
        #endregion

        #region Interfaces
        public override bool HandleGlobalInput(PlayerInputType playerInputType)
        {
            if (!handleGlobalInput) { return true; } // Spoof:  Cannot accept input, so treat as if global input already handled

            if (playerInputType is PlayerInputType.Option or PlayerInputType.Cancel)
            {
                if (cashTransferState == CashTransferState.CashConfirmation)
                {
                    SetCashTransferState(CashTransferState.CashSelection);
                    return true;
                }
            }

            return base.HandleGlobalInput(playerInputType);
        }
        #endregion
    }
}

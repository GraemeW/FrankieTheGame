using Frankie.Control;
using Frankie.Utils.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace Frankie.Inventory.UI
{
    public class CashTransferBox : UIBox
    {
        // Tunables
        [Header("")]
        [SerializeField][Tooltip("Include {0} for funds amount")] string messageDeposit = "You currently have {0} funds to deposit.  How much do you want to deposit?";
        [SerializeField][Tooltip("Include {0} for funds amount")] string messageWithdraw = "You currently have {0} in the bank to withdraw.  How much do you want to take out?";
        [Header("Cash Transfer Fields")]
        [SerializeField] TMP_Text messageField = null;
        [SerializeField] CashTransferField hundredMillionField = null;
        [SerializeField] CashTransferField tenMillionField = null;
        [SerializeField] CashTransferField millionField = null;
        [SerializeField] CashTransferField hundredThousandField = null;
        [SerializeField] CashTransferField tenThousandField = null;
        [SerializeField] CashTransferField thousandField = null;
        [SerializeField] CashTransferField hundredField = null;
        [SerializeField] CashTransferField tenField = null;
        [SerializeField] CashTransferField oneField = null;
        [SerializeField] UIChoiceOption confirmField = null;
        [SerializeField] UIChoiceOption rejectField = null;
        [Header("Other Prefabs")]
        [SerializeField] WalletUI walletUIPrefab = null;

        // State
        CashTransferState cashTransferState = CashTransferState.CashSelection;
        int amountAvailable = 0;
        int amountToTransfer = 0;

        // Cached References
        WorldCanvas worldCanvas = null;
        PlayerStateHandler playerStateHandler = null;
        PlayerController playerController = null;
        Shopper shopper = null;
        Wallet wallet = null;
        WalletUI walletUI = null;

        // Static
        int MAX_TRANSFER_AMOUNT = 999999999;

        #region UnityMethods
        private void Awake()
        {
            GetPlayerReference();
        }

        private void GetPlayerReference()
        {
            worldCanvas = GameObject.FindGameObjectWithTag("WorldCanvas")?.GetComponent<WorldCanvas>();
            playerStateHandler = GameObject.FindGameObjectWithTag("Player")?.GetComponent<PlayerStateHandler>();
            if (worldCanvas == null || playerStateHandler == null) { Destroy(gameObject); }

            playerController = playerStateHandler?.GetComponent<PlayerController>();
            shopper = playerStateHandler?.GetComponent<Shopper>();
            wallet = playerStateHandler?.GetComponent<Wallet>();
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
            playerStateHandler?.EnterWorld();
        }
        #endregion

        #region Initialization
        private void SetupCashTransferBoxUI()
        {
            BankType bankType = shopper.GetBankType();
            if (bankType == BankType.Deposit)
            {
                amountAvailable = wallet.GetCash();
                amountToTransfer = 0;

                messageField.text = string.Format(messageDeposit, $"${amountAvailable:N0}");
                InitializeButtons(amountAvailable, () => { wallet.TransferToWallet(-GetPendingCashToTransfer()); Destroy(gameObject); });
            }
            else if (bankType == BankType.Withdraw)
            {
                amountAvailable = wallet.GetPendingCash();
                amountToTransfer = 0;

                messageField.text = string.Format(messageWithdraw, $"${amountAvailable:N0}");
                InitializeButtons(amountAvailable, () => { wallet.TransferToWallet(GetPendingCashToTransfer()); Destroy(gameObject); });
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void InitializeButtons(int amountAvailable, Action actionOnConfirm)
        {
            SetCashTransferState(CashTransferState.CashSelection);
            foreach(UIChoiceOption choiceOption in choiceOptions)
            {
                choiceOption.AddOnClickListener(() => SelectField(choiceOption));
            }
            RefreshFieldsToTransferAmount();

            confirmField.AddOnClickListener(() => actionOnConfirm.Invoke());
            rejectField.AddOnClickListener(() => Destroy(gameObject));
            SelectField(oneField);
        }
        #endregion

        #region UIBoxStandardInterface
        protected override bool MoveCursor(PlayerInputType playerInputType)
        {
            if (cashTransferState == CashTransferState.CashSelection)
            {
                if (playerInputType == PlayerInputType.NavigateDown || playerInputType == PlayerInputType.NavigateUp)
                {
                    return AdjustNumber(playerInputType);
                }
            }
            return base.MoveCursor(playerInputType);
        }

        protected override bool Choose(string nodeID)
        {
            if (cashTransferState == CashTransferState.CashSelection)
            {
                SetCashTransferState(CashTransferState.CashConfirmation);
                return true;
            }
            else if (cashTransferState == CashTransferState.CashConfirmation)
            {
                return base.Choose(null);
            }
            return false;
        }
        #endregion

        #region PrivateMethods
        private int GetPendingCashToTransfer()
        {
            return amountToTransfer;
        }

        private void SetCashTransferState(CashTransferState cashTransferState)
        {
            this.cashTransferState = cashTransferState;
            ClearChoiceSelections();
            if (cashTransferState == CashTransferState.CashConfirmation)
            {
                choiceOptions.Clear();
                choiceOptions.AddRange(new[] { confirmField, rejectField });
            }
            else if (cashTransferState == CashTransferState.CashSelection)
            {
                choiceOptions.Clear();
                choiceOptions.AddRange(new[] { hundredMillionField, tenMillionField, millionField,
                    hundredThousandField, tenThousandField, thousandField, 
                    hundredField, tenField, oneField });
            }
            ShowCursorOnAnyInteraction(PlayerInputType.NavigateRight);
        }

        private void SelectField(UIChoiceOption choiceOption)
        {
            ClearChoiceSelections();
            choiceOption.Highlight(true);
            highlightedChoiceOption = choiceOption;
        }

        private bool AdjustNumber(PlayerInputType playerInputType)
        {
            if (playerInputType == PlayerInputType.NavigateDown || playerInputType == PlayerInputType.NavigateUp)
            {
                CashTransferField cashTransferField = highlightedChoiceOption as CashTransferField;
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
                modifiedAmount = Mathf.Min(modifiedAmount, MAX_TRANSFER_AMOUNT);

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

            if (playerInputType == PlayerInputType.Option || playerInputType == PlayerInputType.Cancel)
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
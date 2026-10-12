using System;
using UnityEngine;
using UnityEngine.UIElements;
using LowDefMustard.Control;
using LowDefMustard.UIBox;

namespace Frankie.Inventory.UI
{
    public sealed class CashAmountHandle : ChoiceHandle, IUIMoveInterceptor
    {
        // State
        private readonly CashAmountModel model = new();
        private readonly int maxAmount;
        private readonly Action onDigitClicked;
        private int selectedDigit = 0; // Power of ten (0 = ones)

        // Constructor
        public CashAmountHandle(UIToolkitBoxView view, int maxAmount, Action onDigitClicked) : base(view, null, null, typeof(CashAmountContainer))
        {
            this.maxAmount = maxAmount;
            this.onDigitClicked = onDigitClicked;
        }

        #region PublicMethods
        public int amount => model.amount;
        #endregion

        #region ChoiceHandle
        protected override VisualElement CreateChoiceElement()
        {
            var cashAmountElement = new CashAmountElement { dataSource = model };
            cashAmountElement.digitClicked += HandleDigitClicked;
            return cashAmountElement;
        }

        protected override void UnhookElement(VisualElement detachingElement)
        {
            if (detachingElement is CashAmountElement cashAmountElement) { cashAmountElement.digitClicked -= HandleDigitClicked; }
            base.UnhookElement(detachingElement);
        }

        protected override void SetHighlighted(bool enable) => model.caretDigit = enable ? selectedDigit : CashAmountModel.noCaret;
        public override void SetText(string text) { }
        #endregion

        #region IUIMoveInterceptor
        public bool TryMove(ControllerInputType controllerInputType, out bool isHighlightMove)
        {
            isHighlightMove = false;
            switch (controllerInputType)
            {
                case ControllerInputType.NavigateLeft:
                    isHighlightMove = true;
                    SelectDigit(selectedDigit + 1 >= CashAmountModel.digitCount ? 0 : selectedDigit + 1);
                    return true;
                case ControllerInputType.NavigateRight:
                    isHighlightMove = true;
                    SelectDigit(selectedDigit <= 0 ? CashAmountModel.digitCount - 1 : selectedDigit - 1);
                    return true;
                case ControllerInputType.NavigateUp:
                    StepAmount(1);
                    return true;
                case ControllerInputType.NavigateDown:
                    StepAmount(-1);
                    return true;
                default:
                    return false;
            }
        }
        #endregion

        #region PrivateMethods
        private void SelectDigit(int digit)
        {
            selectedDigit = digit;
            model.caretDigit = selectedDigit;
        }

        private void StepAmount(int direction)
        {
            int step = direction;
            for (int digit = 0; digit < selectedDigit; digit++) { step *= 10; }
            model.amount = Mathf.Clamp(model.amount + step, 0, maxAmount);
        }

        private void HandleDigitClicked(int digit)
        {
            if (isRemoved) { return; }
            selectedDigit = digit;
            onDigitClicked?.Invoke(); // Note:  The box highlights this choice, which moves the caret
        }
        #endregion
    }
}

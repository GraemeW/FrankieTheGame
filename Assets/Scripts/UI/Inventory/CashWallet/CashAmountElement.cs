using System;
using System.Collections.Generic;
using System.Globalization;
using Unity.Properties;
using UnityEngine.UIElements;
using LowDefMustard.UIBox;

namespace Frankie.Inventory.UI
{
    [UxmlElement]
    public sealed partial class CashAmountElement : VisualElement
    {
        // Const Tunables
        private const string _ussClassName = "cash-amount";
        private const string _symbolUssClassName = _ussClassName + "__symbol";
        private const string _digitUssClassName = _ussClassName + "__digit";
        private const string _digitSelectedUssClassName = _digitUssClassName + "--selected";
        private const string _digitLabelUssClassName = _ussClassName + "__digit-label";
        private const string _caretUssClassName = _ussClassName + "__caret";
        private const string _currencySymbol = "$";
        private const string _groupSeparator = ",";
        private const string _caret = "^";
        private const int _groupSize = 3;

        // State
        private int internalAmount = 0;
        private int internalCaretDigit = CashAmountModel.noCaret;
        private readonly List<Button> digitButtons = new(); // Indexed by power of ten (0 = ones)
        private readonly List<Label> digitLabels = new();

        // Events
        public event Action<int> digitClicked;

        // Bound Properties
        [CreateProperty, UxmlAttribute] public int amount
        {
            get => internalAmount;
            set
            {
                internalAmount = value;
                RefreshDigits();
            }
        }

        [CreateProperty, UxmlAttribute] public int caretDigit
        {
            get => internalCaretDigit;
            set
            {
                internalCaretDigit = value;
                for (int digit = 0; digit < digitButtons.Count; digit++) { digitButtons[digit].EnableInClassList(_digitSelectedUssClassName, digit == value); }
            }
        }

        // Constructor
        public CashAmountElement()
        {
            AddToClassList(_ussClassName);
            pickingMode = PickingMode.Ignore;

            for (int digit = 0; digit < CashAmountModel.digitCount; digit++) { BuildDigit(digit); }
            Add(CreateLabel(_currencySymbol, _symbolUssClassName));
            for (int digit = CashAmountModel.digitCount - 1; digit >= 0; digit--)
            {
                Add(digitButtons[digit]);
                if (digit > 0 && digit % _groupSize == 0) { Add(CreateLabel(_groupSeparator, _symbolUssClassName)); }
            }
            RefreshDigits();

            SetBinding(nameof(amount), UIToolkitBindings.ToTarget(nameof(CashAmountModel.amount)));
            SetBinding(nameof(caretDigit), UIToolkitBindings.ToTarget(nameof(CashAmountModel.caretDigit)));
        }

        #region PrivateMethods
        private void BuildDigit(int digit)
        {
            var digitButton = new Button { focusable = false }; // Keyboard/gamepad input handled via IInputReceiver - avoid UITK navigation double-firing
            digitButton.AddToClassList(_digitUssClassName);
            digitButton.clicked += () => digitClicked?.Invoke(digit);

            Label digitLabel = CreateLabel("", _digitLabelUssClassName);
            digitButton.Add(digitLabel);
            digitButton.Add(CreateLabel(_caret, _caretUssClassName));

            digitButtons.Add(digitButton);
            digitLabels.Add(digitLabel);
        }

        private void RefreshDigits()
        {
            int workingAmount = internalAmount;
            foreach (Label digitLabel in digitLabels)
            {
                digitLabel.text = (workingAmount % 10).ToString(CultureInfo.InvariantCulture);
                workingAmount /= 10;
            }
        }

        private static Label CreateLabel(string text, string labelUssClassName)
        {
            var label = new Label(text) { pickingMode = PickingMode.Ignore };
            label.AddToClassList(labelUssClassName);
            return label;
        }
        #endregion
    }
}

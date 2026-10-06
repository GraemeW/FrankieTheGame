using System.Globalization;
using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;
using LowDefMustard.UIBox;

namespace Frankie.Combat.UI
{
    public sealed class StatDigitsElement : VisualElement
    {
        // Odometer-style stat readout:  hundreds/tens/ones digits in a slot, leading zeros blanked
        
        // Const Tunables
        private const string _ussClassName = "stat-digits";
        private const string _digitUssClassName = _ussClassName + "__digit";

        // State
        private float internalValue = float.NaN;

        // Cached References
        private readonly Label hundredsLabel;
        private readonly Label tensLabel;
        private readonly Label onesLabel;

        [CreateProperty] public float value
        {
            get => internalValue;
            set
            {
                internalValue = value;
                UpdateDigits();
            }
        }

        public StatDigitsElement(string sourcePropertyName)
        {
            AddToClassList(_ussClassName);
            pickingMode = PickingMode.Ignore;
            hundredsLabel = AddDigit();
            tensLabel = AddDigit();
            onesLabel = AddDigit();
            SetBinding(nameof(value), UIToolkitBindings.ToTarget(sourcePropertyName));
        }

        #region PrivateMethods
        private Label AddDigit()
        {
            var digitLabel = new Label { pickingMode = PickingMode.Ignore };
            digitLabel.AddToClassList(_digitUssClassName);
            Add(digitLabel);
            return digitLabel;
        }

        private void UpdateDigits()
        {
            if (float.IsNaN(internalValue) || float.IsInfinity(internalValue)) { hundredsLabel.text = tensLabel.text = onesLabel.text = ""; return; }

            int roundedValue = internalValue < 1 ? Mathf.FloorToInt(internalValue) : Mathf.RoundToInt(internalValue);
            int hundreds = roundedValue / 100;
            int tens = roundedValue % 100 / 10;
            int ones = roundedValue % 10;

            hundredsLabel.text = hundreds > 0 ? hundreds.ToString(CultureInfo.InvariantCulture) : "";
            tensLabel.text = hundreds > 0 || tens > 0 ? tens.ToString(CultureInfo.InvariantCulture) : "";
            onesLabel.text = ones.ToString(CultureInfo.InvariantCulture);
        }
        #endregion
    }
}

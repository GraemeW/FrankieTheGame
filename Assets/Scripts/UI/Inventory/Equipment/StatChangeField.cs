using System.Globalization;
using UnityEngine;
using TMPro;
using Frankie.Stats;
using Frankie.Utils.Localization;

namespace Frankie.Inventory.UI
{
    public class StatChangeField : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI statField;
        [SerializeField] private TextMeshProUGUI oldValueField;
        [SerializeField] private TextMeshProUGUI newValueField;
        [SerializeField] private Color neutralDeltaColor = Color.gray;
        [SerializeField] private Color betterDeltaColor = Color.green;
        [SerializeField] private Color worseDeltaColor = Color.red;

        public void Setup(Stat stat, float oldValue, float newValue)
        {
            statField.text = LocalizationNames.GetLocalizedName(stat);
            int oldValueRounded = Mathf.RoundToInt(oldValue);
            oldValueField.text = oldValueRounded.ToString(CultureInfo.InvariantCulture);
            int newValueRounded = Mathf.RoundToInt(newValue);
            newValueField.text = newValueRounded.ToString(CultureInfo.InvariantCulture);
            newValueField.color = neutralDeltaColor;

            if (oldValueRounded < newValueRounded)
            {
                newValueField.color = betterDeltaColor;
            }
            else if (newValueRounded < oldValueRounded)
            {
                newValueField.color = worseDeltaColor;
            }
        }
    }
}

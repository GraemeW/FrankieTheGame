using UnityEngine;
using TMPro;

namespace Frankie.Stats.UI
{
    public class StatField : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI statField;
        [SerializeField] private TextMeshProUGUI valueField;

        public void Setup(Stat stat, float value)
        {
            statField.text = StatDisplay.GetLocalizedName(stat);
            valueField.text = Mathf.RoundToInt(value).ToString();
        }

        public void Setup(Stat stat, float numerator, float denominator)
        {
            statField.text = StatDisplay.GetLocalizedName(stat);
            string parsedValue = $"{Mathf.RoundToInt(numerator)}/{Mathf.RoundToInt(denominator)}";
            valueField.text = parsedValue;
        }
    }
}

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
            statField.text = stat.ToString();
            valueField.text = Mathf.RoundToInt(value).ToString();
        }

        public void Setup(string stat, float value)
        {
            statField.text = stat;
            valueField.text = Mathf.RoundToInt(value).ToString();
        }

        public void Setup(string stat, float numerator, float denominator)
        {
            statField.text = stat;
            string parsedValue = $"{Mathf.RoundToInt(numerator)}/{Mathf.RoundToInt(denominator)}";
            valueField.text = parsedValue;
        }
    }
}

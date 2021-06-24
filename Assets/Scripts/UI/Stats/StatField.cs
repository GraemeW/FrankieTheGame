using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace Frankie.Stats.UI
{
    public class StatField : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI statField = null;
        [SerializeField] TextMeshProUGUI valueField = null;

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

        public void Setup (string stat, float numerator, float denominator)
        {
            statField.text = stat;
            string parsedValue = string.Format("{0}/{1}", Mathf.RoundToInt(numerator), Mathf.RoundToInt(denominator));
            valueField.text = parsedValue;
        }
    }
}
using UnityEngine;
using TMPro;
using Frankie.Stats;

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
            statField.text = stat.ToString();
            int oldValueRounded = Mathf.RoundToInt(oldValue);
            oldValueField.text = oldValueRounded.ToString();
            int newValueRounded = Mathf.RoundToInt(newValue);
            newValueField.text = newValueRounded.ToString();
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

using UnityEngine;
using TMPro;
using Frankie.Stats;

namespace Frankie.Inventory.UI
{
    public class StatChangeField : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI statField = null;
        [SerializeField] TextMeshProUGUI oldValueField = null;
        [SerializeField] TextMeshProUGUI newValueField = null;
        [SerializeField] Color neutralDeltaColor = Color.gray;
        [SerializeField] Color betterDeltaColor = Color.green;
        [SerializeField] Color worseDeltaColor = Color.red;

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

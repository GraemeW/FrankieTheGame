using UnityEngine;
using TMPro;

namespace Frankie.Settings.UI
{
    public class ResolutionMonitor : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI resolutionTextElement = null;

        private void Update()
        {
            resolutionTextElement.text = $"{Screen.width} x {Screen.height}";
        }
    }
}

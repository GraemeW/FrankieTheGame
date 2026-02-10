using UnityEngine;
using TMPro;

namespace Frankie.Rendering.UI
{
    public class ResolutionMonitor : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI resolutionTextElement;

        private void Update()
        {
            resolutionTextElement.text = $"{Screen.width} x {Screen.height}";
        }
    }
}

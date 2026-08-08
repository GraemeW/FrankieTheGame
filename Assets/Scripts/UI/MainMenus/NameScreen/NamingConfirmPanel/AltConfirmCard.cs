using TMPro;
using UnityEngine;

namespace Frankie.Menu.UI
{
    public class AltConfirmCard : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI questionField;
        [SerializeField] private TextMeshProUGUI answerField;

        public void Setup(string question, string answer)
        {
            if (!string.IsNullOrEmpty(question)) { questionField.text = question; }
            if (!string.IsNullOrEmpty(answer)) { answerField.text = answer; }
        }
    }
}

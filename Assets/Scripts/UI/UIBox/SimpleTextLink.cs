using UnityEngine;
using TMPro;

namespace Frankie.Utils.UI
{
    public class SimpleTextLink : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI textField;
        [SerializeField] private bool disableOnLoad = true;

        private void Start()
        {
            if (!disableOnLoad) { return; }
            
            textField.text = "";
            gameObject.SetActive(false);
        }

        public void Setup(string text)
        {
            if (!gameObject.activeSelf) { gameObject.SetActive(true); }
            textField.text = text;
        }
    }
}

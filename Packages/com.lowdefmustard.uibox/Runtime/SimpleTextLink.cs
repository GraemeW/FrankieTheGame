using UnityEngine;
using TMPro;

namespace LowDefMustard.UIBox
{
    public class SimpleTextLink : MonoBehaviour
    {
        // Note:  Internal fields for test visibility
        [SerializeField] internal TextMeshProUGUI textField;
        [SerializeField] internal bool disableOnLoad = true;

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

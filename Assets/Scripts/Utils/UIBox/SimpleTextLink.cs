using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace Frankie.Utils.UI
{
    public class SimpleTextLink : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI textField = null;
        [SerializeField] bool disableOnLoad = true;

        private void Start()
        {
            if (disableOnLoad)
            {
                textField.text = "";
                gameObject.SetActive(false);
            }
        }

        public void Setup(string text)
        {
            if (gameObject.activeSelf == false) { gameObject.SetActive(true); }
            textField.text = text;
        }
    }
}

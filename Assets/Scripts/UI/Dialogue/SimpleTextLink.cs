using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace Frankie.Dialogue.UI
{
    public class SimpleTextLink : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI textField = null;

        private void Start()
        {
            textField.text = "";
            gameObject.SetActive(false);
        }

        public void Setup(string text)
        {
            if (gameObject.activeSelf == false) { gameObject.SetActive(true); }
            textField.text = text;
        }
    }
}

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
            textField.text = text;
        }
    }
}

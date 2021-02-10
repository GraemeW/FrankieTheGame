using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace Frankie.Combat.UI
{
    public class DamageText : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI damageTextElement = null;

        public void DestroyText() // called by animation event
        {
            Destroy(gameObject);
        }

        public void SetText(float damageAmount)
        {
            damageTextElement.text = Mathf.Abs(Mathf.RoundToInt(damageAmount)).ToString();
        }

        public void SetColor(Color color)
        {
            damageTextElement.color = color;
        }
    }

}

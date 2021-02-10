using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Combat.UI
{
    public class DamageTextSpawner : MonoBehaviour
    {
        [SerializeField] Color damageTextColor = Color.red;
        [SerializeField] Color healingTextColor = Color.green;
        [SerializeField] DamageText damageTextPrefab = null;

        public void Spawn(float damageAmount)
        {
            DamageText damageTextInstance = Instantiate(damageTextPrefab, transform);
            damageTextInstance.SetText(damageAmount);
            if (damageAmount > 0)
            {
                damageTextInstance.SetColor(healingTextColor);
            }
            else if (damageAmount < 0)
            {
                damageTextInstance.SetColor(damageTextColor);
            }
        }
    }
}
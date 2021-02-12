using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Frankie.Combat
{
    public class ActiveStatusEffect : MonoBehaviour
    {
        // State
        StatusEffect statusEffect = null;
        float timer = 0f;
        float tickTimer = 0f;

        public void Setup(StatusEffect statusEffect)
        {
            this.statusEffect = statusEffect;
            HandleInstantEffects();
        }

        private void Update()
        {
            HandleRecurringEffects();
            timer += Time.deltaTime;
            tickTimer += Time.deltaTime;
        }

        public string GetEffectName()
        {
            // Split apart name on lower case followed by upper case w/ or w/out underscores
            return Regex.Replace(statusEffect.name, "([a-z])_?([A-Z])", "$1 $2");
        }

        private void HandleInstantEffects()
        {
            if (statusEffect.statusEffectType == StatusEffectType.Frozen)
            {

            }
        }

        private void HandleRecurringEffects()
        {
            if (statusEffect.statusEffectType == StatusEffectType.Bleeding)
            {

            }
            else if (statusEffect.statusEffectType == StatusEffectType.Burning)
            {

            }
            else if (statusEffect.statusEffectType == StatusEffectType.Electrified)
            {

            }
            else if (statusEffect.statusEffectType == StatusEffectType.Frozen)
            {

            }
        }
    }   
}

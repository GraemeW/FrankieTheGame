using Frankie.Stats;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Combat.UI
{
    public class StatusEffectBobble : MonoBehaviour
    {
        // Tunables
        [Header("Hookups")]
        [SerializeField] GameObject healingIcon = null;
        [SerializeField] GameObject damageIcon = null;
        [SerializeField] GameObject brawnIcon = null;
        [SerializeField] GameObject beautyIcon = null;
        [SerializeField] GameObject smartsIcon = null;
        [SerializeField] GameObject nimbleIcon = null;
        [SerializeField] GameObject luckIcon = null;
        [SerializeField] GameObject pluckIcon = null;
        [SerializeField] GameObject increaseModifier = null;
        [SerializeField] GameObject decreaseModifier = null;

        // State
        PersistentStatus persistentStatus = null;
        Stat statusEffectType = default; // Default, override in setup
        bool isIncrease = true; // Default, override in setup

        // Methods
        public void Setup(PersistentStatus persistentStatus)
        {
            if (persistentStatus == null) { return; }

            this.persistentStatus = persistentStatus;
            this.statusEffectType = persistentStatus.GetStatusEffectType();
            this.isIncrease = persistentStatus.IsIncrease();
            this.persistentStatus.persistentStatusTimedOut += RemoveIconOnTimeout;
            ToggleOffIcons();
            SetModifier(); // Must call before ToggleIcon (modifier impacts icon)
            ToggleIcon();
        }

        private void ToggleOffIcons()
        {
            healingIcon.SetActive(false);
            damageIcon.SetActive(false);
            brawnIcon.SetActive(false);
            beautyIcon.SetActive(false);
            smartsIcon.SetActive(false);
            nimbleIcon.SetActive(false);
            luckIcon.SetActive(false);
            pluckIcon.SetActive(false);
        }

        private void ToggleIcon()
        {
            switch (statusEffectType)
            {
                case Stat.HP:
                    if (isIncrease)
                    {
                        healingIcon.SetActive(true);
                    }
                    else
                    {
                        damageIcon.SetActive(true);
                    }
                    break;
                case Stat.AP:
                    break; // TODO:  Add an AP icon
                case Stat.Brawn:
                    brawnIcon.SetActive(true);
                    break;
                case Stat.Beauty:
                    beautyIcon.SetActive(true);
                    break;
                case Stat.Smarts:
                    smartsIcon.SetActive(true);
                    break;
                case Stat.Nimble:
                    nimbleIcon.SetActive(true);
                    break;
                case Stat.Luck:
                    luckIcon.SetActive(true);
                    break;
                case Stat.Pluck:
                    pluckIcon.SetActive(true);
                    break;
            }
        }

        private void SetModifier()
        {
            if (isIncrease)
            {
                increaseModifier.SetActive(true);
                decreaseModifier.SetActive(false);
            }
            else
            {
                decreaseModifier.SetActive(true);
                increaseModifier.SetActive(false);
            }
        }

        private void RemoveIconOnTimeout()
        {
            if (persistentStatus != null) { persistentStatus.persistentStatusTimedOut -= RemoveIconOnTimeout; }
            Destroy(gameObject);
        }
    }
}


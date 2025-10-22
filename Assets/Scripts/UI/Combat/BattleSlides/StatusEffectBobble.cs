using Frankie.Stats;
using UnityEngine;

namespace Frankie.Combat.UI
{
    public class StatusEffectBobble : MonoBehaviour
    {
        // Tunables
        [Header("Hookups")]
        [SerializeField] private GameObject healingIcon;
        [SerializeField] private GameObject damageIcon;
        [SerializeField] private GameObject brawnIcon;
        [SerializeField] private GameObject beautyIcon;
        [SerializeField] private GameObject smartsIcon;
        [SerializeField] private GameObject nimbleIcon;
        [SerializeField] private GameObject luckIcon;
        [SerializeField] private GameObject pluckIcon;
        [SerializeField] private GameObject increaseModifier;
        [SerializeField] private GameObject decreaseModifier;

        // State
        private PersistentStatus persistentStatus;
        private Stat statusEffectType; // Default, override in setup
        private bool isIncrease = true; // Default, override in setup

        // Methods
        public void Setup(PersistentStatus setPersistentStatus)
        {
            if (setPersistentStatus == null) { return; }

            persistentStatus = setPersistentStatus;
            statusEffectType = setPersistentStatus.GetStatusEffectType();
            isIncrease = setPersistentStatus.IsIncrease();
            persistentStatus.persistentStatusTimedOut += RemoveIconOnTimeout;
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
            if (gameObject != null) { Destroy(gameObject); }
        }
    }
}

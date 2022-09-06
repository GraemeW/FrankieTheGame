using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Frankie.Combat.UI
{
    public class CooldownTimer : MonoBehaviour
    {
        // Tunables
        [SerializeField] Image cooldownImage = null;

        // State
        float cooldownTime = 1f;
        float currentTime = 0f;

        void Update()
        {
            currentTime += Time.deltaTime;
            SetImageFraction();
        }

        public void ResetTimer(float cooldownTime)
        {
            if (cooldownTime <= 0) { cooldownTime = 1f; currentTime = 1f; }

            this.cooldownTime = cooldownTime;
            currentTime = 0f;

            SetImageFraction();
        }

        private void SetImageFraction()
        {
            if (currentTime > cooldownTime) { return; }

            float fraction = Mathf.Clamp(currentTime / cooldownTime, 0f, 1f);
            cooldownImage.fillAmount = fraction;
        }
    }
}
using UnityEngine;
using UnityEngine.UI;

namespace Frankie.Combat.UI
{
    public class CooldownTimer : MonoBehaviour
    {
        // Tunables
        [SerializeField] private Image cooldownImage;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Color pauseColor = Color.lightSkyBlue;

        // State
        private float cooldownTime = 1f;
        private float currentTime;

        private void Update()
        {
            currentTime += Time.deltaTime;
            SetImageFraction();
        }

        public void ResetTimer(float setCooldownTime)
        {
            if (setCooldownTime <= 0) { setCooldownTime = 1f; currentTime = 1f; }

            cooldownTime = setCooldownTime;
            currentTime = 0f;

            SetImageFraction();
        }

        public void SetPaused(bool paused)
        {
            if (backgroundImage == null) { return; }
            backgroundImage.color = paused ? pauseColor: Color.black;
        }

        private void SetImageFraction()
        {
            if (currentTime > cooldownTime) { return; }

            float fraction = Mathf.Clamp(currentTime / cooldownTime, 0f, 1f);
            cooldownImage.fillAmount = fraction;
        }
    }
}

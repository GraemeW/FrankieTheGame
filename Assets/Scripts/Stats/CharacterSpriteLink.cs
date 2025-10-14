using System;
using UnityEngine;
using Frankie.Control;

namespace Frankie.Stats
{
    public class CharacterSpriteLink : MonoBehaviour
    {
        // Tunables - Hookups
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Animator animator;
        // Tunables - Parameters
        [SerializeField] private float startingImmunityFlashPeriod = 0.1f;
        [SerializeField] private float immunityFlashPeriodGrowth = 1.25f;
        [SerializeField] private float immuneAlpha = 0.4f;

        // State
        private float standardAlpha;
        private bool isCharacterFlashing;
        private float flashTimer;
        private float currentImmunityFlashPeriod;
        
        // Events
        public event Action<float, float> characterLookUpdated;
        public event Action<float> characterSpeedUpdated;
        
        #region UnityMethods

        private void Awake()
        {
            if (spriteRenderer != null) { standardAlpha = spriteRenderer.color.a; }
        }

        private void Update()
        {
            UpdateCharacterFlash();
        }

        #endregion
        
        #region Getters
        public SpriteRenderer GetSpriteRenderer() => spriteRenderer;
        public Animator GetAnimator() => animator;
        #endregion
        
        // Animator Setter Methods
        public void UpdateCharacterAnimation(float xLook, float yLook, float speed)
        {
            Mover.SetAnimatorSpeed(animator, speed);
            Mover.SetAnimatorxLook(animator, xLook);
            Mover.SetAnimatoryLook(animator, yLook);
            characterLookUpdated?.Invoke(xLook, yLook);
            characterSpeedUpdated?.Invoke(speed);
        }

        public void UpdateCharacterAnimation(float xLook, float yLook)
        {
            Mover.SetAnimatorxLook(animator, xLook);
            Mover.SetAnimatoryLook(animator, yLook);
            characterLookUpdated?.Invoke(xLook, yLook);
        }

        public void UpdateCharacterAnimation(float speed)
        {
            Mover.SetAnimatorSpeed(animator, speed);
            characterSpeedUpdated?.Invoke(speed);
        }

        public void SetIsFlashing(bool isFlashing)
        {
            isCharacterFlashing = isFlashing;
            currentImmunityFlashPeriod = startingImmunityFlashPeriod;
            flashTimer = 0f;
            float currentAlpha = isCharacterFlashing ? immuneAlpha : standardAlpha;
            
            var color = new Color(spriteRenderer.color.r, spriteRenderer.color.g, spriteRenderer.color.b, currentAlpha);
            spriteRenderer.color = color;
        }

        private void UpdateCharacterFlash()
        {
            if (!isCharacterFlashing) { return; }
            
            flashTimer += Time.deltaTime;
            if (flashTimer >= currentImmunityFlashPeriod)
            {
                ToggleCharacterAlpha();
                currentImmunityFlashPeriod *= immunityFlashPeriodGrowth; // slow flash down as time progresses
                flashTimer = 0f;
            }
        }

        private void ToggleCharacterAlpha()
        {
            if (spriteRenderer == null) { return; }

            float targetAlpha = Mathf.Approximately(spriteRenderer.color.a, standardAlpha) ? immuneAlpha : standardAlpha;
            var color = new Color(spriteRenderer.color.r, spriteRenderer.color.g, spriteRenderer.color.b, targetAlpha);
            spriteRenderer.color = color;
        }
    }
}

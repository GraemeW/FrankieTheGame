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
        [SerializeField] private float immunityFlashPeriodGrowth = 1.15f;
        [SerializeField] private float immuneAlphaLow = 0.4f;
        [SerializeField] private float immuneAlphaHigh = 0.85f;

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
        public void UpdateCharacterAnimation(MovementAnimationParameters movementAnimationParameters)
        {
            Mover.SetAnimatorSpeed(animator, movementAnimationParameters.speed);
            Mover.SetAnimatorXLook(animator, movementAnimationParameters.xLookDirection);
            Mover.SetAnimatorYLook(animator, movementAnimationParameters.yLookDirection);
            UpdateSpriteOffset(movementAnimationParameters.pixelPerfectOffset);
            characterLookUpdated?.Invoke(movementAnimationParameters.xLookDirection, movementAnimationParameters.yLookDirection);
            characterSpeedUpdated?.Invoke(movementAnimationParameters.speed);
        }

        public void UpdateSpriteOffset(Vector2 pixelPerfectOffset)
        {
            if (spriteRenderer == null) { return; }
            spriteRenderer.gameObject.transform.localPosition = pixelPerfectOffset;
        }

        public void UpdateCharacterLook(float xLook, float yLook)
        {
            Mover.SetAnimatorXLook(animator, xLook);
            Mover.SetAnimatorYLook(animator, yLook);
            characterLookUpdated?.Invoke(xLook, yLook);
        }

        public void UpdateCharacterSpeed(float speed)
        {
            Mover.SetAnimatorSpeed(animator, speed);
            characterSpeedUpdated?.Invoke(speed);
        }

        public void SetIsFlashing(bool isFlashing)
        {
            if (spriteRenderer == null) { return; }
            
            isCharacterFlashing = isFlashing;
            currentImmunityFlashPeriod = startingImmunityFlashPeriod;
            flashTimer = 0f;
            float currentAlpha = isCharacterFlashing ? immuneAlphaLow : standardAlpha;
            
            var color = new Color(spriteRenderer.color.r, spriteRenderer.color.g, spriteRenderer.color.b, currentAlpha);
            spriteRenderer.color = color;
        }

        private void UpdateCharacterFlash()
        {
            if (spriteRenderer == null) { return; }
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
            
            float targetAlpha = Mathf.Approximately(spriteRenderer.color.a, immuneAlphaLow) ? immuneAlphaHigh : immuneAlphaLow;
            var color = new Color(spriteRenderer.color.r, spriteRenderer.color.g, spriteRenderer.color.b, targetAlpha);
            spriteRenderer.color = color;
        }
    }
}

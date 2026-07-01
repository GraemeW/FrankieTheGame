using System;
using UnityEngine;

namespace Frankie.Control
{
    public class CharacterMoveLink : MonoBehaviour
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
        
        // Cached
        private Vector2 lookDirection;
        
        // Events
        public event Action<Vector2> characterLookUpdated;
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
        public Vector2 GetLookDirection() => lookDirection;
        #endregion
        
        // Animator Setter Methods
        public void UpdateCharacterAnimation(MovementAnimationParameters movementAnimationParameters)
        {
            UpdateSpriteOffset(movementAnimationParameters.pixelPerfectOffset);
            UpdateCharacterSpeed(movementAnimationParameters.speed);
            UpdateCharacterLook(movementAnimationParameters.lookDirection);
        }

        public void UpdateSpriteOffset(Vector2 pixelPerfectOffset)
        {
            if (spriteRenderer == null) { return; }
            spriteRenderer.gameObject.transform.localPosition = pixelPerfectOffset;
        }

        public void UpdateCharacterLook(Vector2 newLookDirection)
        {
            lookDirection = newLookDirection;
            Mover.SetAnimatorXLook(animator, lookDirection.x);
            Mover.SetAnimatorYLook(animator, lookDirection.y);
            characterLookUpdated?.Invoke(lookDirection);
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

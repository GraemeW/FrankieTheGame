using System;
using UnityEngine;
using Frankie.Control;

namespace Frankie.Stats
{
    public class CharacterSpriteLink : MonoBehaviour
    {
        // Tunables
        [SerializeField] SpriteRenderer spriteRenderer = null;
        [SerializeField] Animator animator = null;

        // Events
        public event Action<float, float> characterLookUpdated;
        public event Action<float> characterSpeedUpdated;


        // Getters
        public SpriteRenderer GetSpriteRenderer() => spriteRenderer;
        public Animator GetAnimator() => animator;

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
    }
}

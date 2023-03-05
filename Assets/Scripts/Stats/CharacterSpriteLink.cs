using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frankie.Inventory;

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
            animator.SetFloat("xLook", xLook);
            animator.SetFloat("yLook", yLook);
            animator.SetFloat("Speed", speed);
            characterLookUpdated?.Invoke(xLook, yLook);
            characterSpeedUpdated?.Invoke(speed);
        }

        public void UpdateCharacterAnimation(float xLook, float yLook)
        {
            animator.SetFloat("xLook", xLook);
            animator.SetFloat("yLook", yLook);
            characterLookUpdated?.Invoke(xLook, yLook);
        }

        public void UpdateCharacterAnimation(float speed)
        {
            animator.SetFloat("Speed", speed);
            characterSpeedUpdated?.Invoke(speed);
        }
    }
}

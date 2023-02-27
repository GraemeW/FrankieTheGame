using Frankie.Stats;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Inventory
{
    public class Wearable : MonoBehaviour
    {
        // Tunables
        [SerializeField] Animator[] animators;

        // Cached References
        CharacterSpriteLink characterSpriteLink = null;

        // Unity Methods
        private void OnEnable()
        {
            if (characterSpriteLink != null)
            {
                characterSpriteLink.characterLookUpdated += UpdateAnimatorLooks;
                characterSpriteLink.characterSpeedUpdated += UpdateAnimatorSpeeds;
            }
        }

        private void OnDisable()
        {
            if (characterSpriteLink != null)
            {
                characterSpriteLink.characterLookUpdated -= UpdateAnimatorLooks;
                characterSpriteLink.characterSpeedUpdated -= UpdateAnimatorSpeeds;
            }
        }

        // Public Methods
        public void AttachToCharacter(CharacterSpriteLink characterSpriteLink)
        {
            Transform attachRoot = characterSpriteLink.GetAttachedObjectsRoot();
            transform.parent = attachRoot;

            this.characterSpriteLink = characterSpriteLink;
            characterSpriteLink.characterLookUpdated += UpdateAnimatorLooks;
            characterSpriteLink.characterSpeedUpdated += UpdateAnimatorSpeeds;
        }

        public void RemoveFromCharacter()
        {
            Destroy(gameObject);
        }

        // Private Methods
        private void UpdateAnimatorLooks(float xLook, float  yLook)
        {
            if (animators == null || animators.Length == 0) { return; }

            foreach (Animator animator in animators)
            {
                if (animator.runtimeAnimatorController == null) { continue; }

                animator.SetFloat("xLook", xLook);
                animator.SetFloat("yLook", yLook);
            }
        }

        private void UpdateAnimatorSpeeds(float speed)
        {
            if (animators == null || animators.Length == 0) { return; }

            foreach (Animator animator in animators)
            {
                if (animator.runtimeAnimatorController == null) { return; }

                animator.SetFloat("Speed", speed);
            }
        }
            
    }
}

using Frankie.Control;
using Frankie.Stats;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Inventory
{
    [RequireComponent(typeof(Animator))]
    public class Wearable : MonoBehaviour, IModifierProvider
    {
        // Tunables
        [SerializeField] WearableItem wearableItem = null;
        [SerializeField] BaseStatModifier[] baseStatModifiers = null;

        // Cached References
        Animator animator = null;
        CharacterSpriteLink characterSpriteLink = null;

        // Unity Methods
        private void Awake()
        {
            animator = GetComponent<Animator>();
        }

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
        public WearableItem GetWearableItem() => wearableItem;

        public void AttachToCharacter(WearablesLink wearablesLink)
        {
            Transform attachRoot = wearablesLink.GetAttachedObjectsRoot();
            transform.parent = attachRoot;

            characterSpriteLink = wearablesLink.GetCharacterSpriteLink();
            if (characterSpriteLink == null) { return; }

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
            if (animator.runtimeAnimatorController == null) { return; }

            Mover.SetAnimatorxLook(animator, xLook);
            Mover.SetAnimatoryLook(animator, yLook);
        }

        private void UpdateAnimatorSpeeds(float speed)
        {
            if (animator.runtimeAnimatorController == null) { return; }
            Mover.SetAnimatorSpeed(animator, speed);
        }

        // Interface Methods
        public IEnumerable<float> GetAdditiveModifiers(Stat stat)
        {
            float value = 0f;
            foreach (BaseStatModifier baseStatModifier in baseStatModifiers)
            {
                if (baseStatModifier.stat == stat)
                {
                    value += Random.Range(baseStatModifier.minValue, baseStatModifier.maxValue);
                }
            }
            yield return value;
        }
    }
}

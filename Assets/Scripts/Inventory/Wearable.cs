using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Frankie.Control;
using Frankie.Stats;

namespace Frankie.Inventory
{
    [RequireComponent(typeof(Animator))]
    public class Wearable : MonoBehaviour, IModifierProvider
    {
        // Tunables
        [SerializeField] private WearableItem wearableItem;
        [SerializeField] private List<BaseStatModifier> baseStatModifiers =  new();

        // Cached References
        private Animator animator;
        private CharacterSpriteLink characterSpriteLink;

        // Unity Methods
        private void Awake()
        {
            animator = GetComponent<Animator>();
        }

        private void OnEnable()
        {
            if (characterSpriteLink == null) { return; }
            characterSpriteLink.characterLookUpdated += UpdateAnimatorLooks;
            characterSpriteLink.characterSpeedUpdated += UpdateAnimatorSpeeds;
        }

        private void OnDisable()
        {
            if (characterSpriteLink == null) { return; }
            characterSpriteLink.characterLookUpdated -= UpdateAnimatorLooks;
            characterSpriteLink.characterSpeedUpdated -= UpdateAnimatorSpeeds;
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
        private void UpdateAnimatorLooks(float xLook, float yLook)
        {
            if (animator.runtimeAnimatorController == null) { return; }

            Mover.SetAnimatorXLook(animator, xLook);
            Mover.SetAnimatorYLook(animator, yLook);
        }

        private void UpdateAnimatorSpeeds(float speed)
        {
            if (animator.runtimeAnimatorController == null) { return; }
            Mover.SetAnimatorSpeed(animator, speed);
        }

        // Interface Methods
        public IEnumerable<float> GetAdditiveModifiers(Stat stat)
        {
            return from baseStatModifier in baseStatModifiers where baseStatModifier.stat == stat select Random.Range(baseStatModifier.minValue, baseStatModifier.maxValue);
        }
    }
}

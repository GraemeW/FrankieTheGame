using UnityEngine;
using Frankie.Control;

namespace Frankie.Inventory
{
    [RequireComponent(typeof(Animator))]
    public class Wearable : MonoBehaviour
    {
        // Tunables
        [SerializeField] private WearableItem wearableItem;

        // Cached References
        private Animator animator;
        private CharacterMoveLink characterMoveLink;

        // Unity Methods
        private void Awake()
        {
            animator = GetComponent<Animator>();
        }

        private void OnEnable()
        {
            if (characterMoveLink == null) { return; }
            characterMoveLink.characterLookUpdated += UpdateAnimatorLooks;
            characterMoveLink.characterSpeedUpdated += UpdateAnimatorSpeeds;
        }

        private void OnDisable()
        {
            if (characterMoveLink == null) { return; }
            characterMoveLink.characterLookUpdated -= UpdateAnimatorLooks;
            characterMoveLink.characterSpeedUpdated -= UpdateAnimatorSpeeds;
        }

        // Public Methods
        public WearableItem GetWearableItem() => wearableItem;

        public void AttachToCharacter(WearablesLink wearablesLink)
        {
            Transform attachRoot = wearablesLink.GetAttachedObjectsRoot();
            transform.parent = attachRoot;

            characterMoveLink = wearablesLink.GetCharacterSpriteLink();
            if (characterMoveLink == null) { return; }

            characterMoveLink.characterLookUpdated += UpdateAnimatorLooks;
            characterMoveLink.characterSpeedUpdated += UpdateAnimatorSpeeds;
        }

        public void RemoveFromCharacter()
        {
            Destroy(gameObject);
        }

        // Private Methods
        private void UpdateAnimatorLooks(Vector2 lookDirection)
        {
            if (animator.runtimeAnimatorController == null) { return; }

            Mover.SetAnimatorXLook(animator, lookDirection.x);
            Mover.SetAnimatorYLook(animator, lookDirection.y);
        }

        private void UpdateAnimatorSpeeds(float speed)
        {
            if (animator.runtimeAnimatorController == null) { return; }
            Mover.SetAnimatorSpeed(animator, speed);
        }
    }
}

using UnityEngine;
using Frankie.Control;
using Frankie.Core;

namespace Frankie.Inventory
{
    [RequireComponent(typeof(Animator))]
    public class Wearable : MonoBehaviour
    {
        // Tunables
        [SerializeField] private GameObject colliderObject; 
        
        // State
        private WearableItem wearableItem;
        
        // Cached References
        private Animator animator;
        private CharacterMoveLink characterMoveLink;

        #region UnityMethods
        private void Awake()
        {
            animator = GetComponent<Animator>();
        }

        private void OnEnable()
        {
            SubscribeAnimatorToCharacterMoveLink(true);
        }

        private void OnDisable()
        {
            SubscribeAnimatorToCharacterMoveLink(false);
        }

        private void SubscribeAnimatorToCharacterMoveLink(bool enable)
        {
            if (characterMoveLink == null) { return; }
            characterMoveLink.characterLookUpdated -= UpdateAnimatorLooks;
            characterMoveLink.characterSpeedUpdated -= UpdateAnimatorSpeeds;
            if (enable)
            {
                characterMoveLink.characterLookUpdated += UpdateAnimatorLooks;
                characterMoveLink.characterSpeedUpdated += UpdateAnimatorSpeeds;
            }
        }
        #endregion

        #region PublicMethods
        public WearableItem GetWearableItem() => wearableItem;

        public void Setup(WearableItem setWearableItem, WearablesLink wearablesLink)
        {
            wearableItem = setWearableItem;
            if (wearableItem == null)
            {
                Destroy(gameObject);
                return;
            }
            
            TrySetupAttachedObjectsRoot(wearablesLink);
            if (wearablesLink.TryGetCharacterMoveLink(out characterMoveLink)) { SubscribeAnimatorToCharacterMoveLink(true);}
            SetupCollisionLayer();
        }
        #endregion

        // Private Methods
        private void TrySetupAttachedObjectsRoot(WearablesLink wearablesLink)
        {
            if (wearablesLink.TryGetAttachedObjectsRoot(out Transform attachedObjectsRoot))
            {
                transform.parent = attachedObjectsRoot;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void SetupCollisionLayer()
        {
            if (colliderObject == null || wearableItem == null) { return; }

            bool shouldColliderMatchPlayer = wearableItem.ShouldColliderMatchPlayer();
            colliderObject.layer = shouldColliderMatchPlayer ? Player.GetPlayerLayer() : Player.GetIgnoreRaycastLayer();
        }
        
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

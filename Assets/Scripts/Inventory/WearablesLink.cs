using UnityEngine;
using Frankie.Control;

namespace Frankie.Inventory
{
    public class WearablesLink : MonoBehaviour
    {
        // Tunables
        [SerializeField] private Transform attachedObjectsRoot;
        
        // Cached References
        private CharacterMoveLink characterMoveLink;

        #region UnityMethods
        private void Awake()
        {
            characterMoveLink = GetComponent<CharacterMoveLink>();
        }
        #endregion

        #region PublicMethods

        public bool TryGetCharacterMoveLink(out CharacterMoveLink passCharacterMoveLink)
        {
            passCharacterMoveLink = characterMoveLink;
            return passCharacterMoveLink != null;
        }

        public bool TryGetAttachedObjectsRoot(out Transform passAttachedObjectsRoot)
        {
            passAttachedObjectsRoot = attachedObjectsRoot;
            return passAttachedObjectsRoot != null;
        }
        
        public void SpawnWearable(WearableItem wearableItem)
        {
            if (wearableItem == null || IsWearingItem(wearableItem, out _)) { return; }
            Wearable wearablePrefab = wearableItem.GetWearablePrefab();
            if (wearablePrefab == null) { return; }
            
            Wearable spawnedWearable = Instantiate(wearablePrefab, attachedObjectsRoot);
            spawnedWearable.Setup(wearableItem, this);
        }

        public void RemoveWearable(WearableItem wearableItem)
        {
            if (wearableItem == null) { return; }
            if (!IsWearingItem(wearableItem, out Wearable wearable)) { return; }
            Destroy(wearable.gameObject);
        }
        #endregion
        
        #region PrivateMethods
        private bool IsWearingItem(WearableItem wearableItem, out Wearable wearable)
        {
            string wearableItemID = wearableItem.GetGUID();
            wearable = null;

            foreach (Transform checkWearableObject in attachedObjectsRoot)
            {
                if (!checkWearableObject.TryGetComponent(out Wearable checkWearable)) { continue; }

                WearableItem checkWearableItem = checkWearable.GetWearableItem();
                if (checkWearableItem == null) { continue; }
                if (checkWearableItem.GetGUID() != wearableItemID) { continue; }
                
                wearable = checkWearable;
                return true;
            }
            return false;
        }
        #endregion
    }
}

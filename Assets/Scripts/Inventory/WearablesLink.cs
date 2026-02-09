using System.Collections.Generic;
using UnityEngine;
using Frankie.Saving;
using Frankie.Stats;

namespace Frankie.Inventory
{
    public class WearablesLink : MonoBehaviour, IModifierProvider, ISaveable
    {
        // Tunables
        [SerializeField] private Transform attachedObjectsRoot;

        // Cached References
        private CharacterSpriteLink characterSpriteLink;

        #region UnityMethods
        private void Awake()
        {
            characterSpriteLink = GetComponent<CharacterSpriteLink>();
        }
        #endregion

        #region PublicMethods
        public CharacterSpriteLink GetCharacterSpriteLink() => characterSpriteLink;
        public Transform GetAttachedObjectsRoot() => attachedObjectsRoot;

        public bool IsWearingItem(Wearable wearable)
        {
            WearableItem wearableItem = wearable.GetWearableItem();
            return wearableItem != null && IsWearingItem(wearableItem);
        }

        public bool IsWearingItem(WearableItem wearableItem)
        {
            string wearableItemID = wearableItem.GetItemID();

            foreach (Transform checkWearableObject in attachedObjectsRoot)
            {
                if (!checkWearableObject.TryGetComponent(out Wearable checkWearable)) { continue; }

                WearableItem checkWearableItem = checkWearable.GetWearableItem();
                if (checkWearableItem == null) { continue; }

                if (checkWearableItem.GetItemID() == wearableItemID) { return true; }
            }
            return false;
        }
        #endregion

        #region ModifierInterface
        public IEnumerable<float> GetAdditiveModifiers(Stat stat)
        {
            if (attachedObjectsRoot == null) { yield break; }

            foreach (Transform wearableObject in attachedObjectsRoot)
            {
                if (!wearableObject.TryGetComponent(out IModifierProvider modifierProvider)) { yield break; }

                foreach (float modifier in modifierProvider.GetAdditiveModifiers(stat))
                {
                    yield return modifier;
                }
            }
        }
        #endregion

        #region SaveInterface
        public LoadPriority GetLoadPriority() => LoadPriority.ObjectProperty;

        public SaveState CaptureState()
        {
            var wearableItemIDs = new List<string>();
            foreach (Transform wearableObject in attachedObjectsRoot)
            {
                if (!wearableObject.TryGetComponent(out Wearable wearable)) { continue; }
                WearableItem wearableItem = wearable.GetWearableItem();
                if (wearableItem == null) { continue; }

                wearableItemIDs.Add(wearableItem.GetItemID());
            }
            return new SaveState(GetLoadPriority(), wearableItemIDs);
        }

        public void RestoreState(SaveState saveState)
        {
            if (saveState.GetState(typeof(List<string>)) is not List<string> wearableItemIDs) { return; }

            foreach (string wearableItemID in wearableItemIDs)
            {
                if (string.IsNullOrEmpty(wearableItemID)) { continue; }

                var wearableItem = InventoryItem.GetFromID(wearableItemID) as WearableItem;
                if (wearableItem == null) { continue; }

                Wearable wearablePrefab = wearableItem.GetWearablePrefab();
                if (wearablePrefab == null) { continue; }
                Wearable wearable = Instantiate(wearablePrefab, attachedObjectsRoot);

                wearable.AttachToCharacter(this);
            }
        }
        #endregion
    }
}

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Frankie.Saving;
using Frankie.Stats;

namespace Frankie.Inventory
{
    public class WearablesLink : MonoBehaviour, IModifierProvider, ISaveable<List<WearableItem>>
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
            string wearableItemID = wearableItem.GetGUID();

            foreach (Transform checkWearableObject in attachedObjectsRoot)
            {
                if (!checkWearableObject.TryGetComponent(out Wearable checkWearable)) { continue; }

                WearableItem checkWearableItem = checkWearable.GetWearableItem();
                if (checkWearableItem == null) { continue; }

                if (checkWearableItem.GetGUID() == wearableItemID) { return true; }
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

        public SaveState CaptureState() => ManualGetStateFromData(GetAttachedWearableItems()); 

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
        
        public SaveState ManualGetStateFromData(List<WearableItem> data)
        {
            var wearableItemIDs = new List<string>();
            if (data != null)
            {
                wearableItemIDs.AddRange(from wearableItem in data where wearableItem != null select wearableItem.GetGUID());
            }
            return new SaveState(GetLoadPriority(), wearableItemIDs);
        }

        public List<WearableItem> ManualGetDataFromState(SaveState saveState)
        {
            var wearableItems = new List<WearableItem>();
            if (saveState?.GetState(typeof(List<string>)) is not List<string> wearableItemIDs) { return wearableItems; }

            wearableItems.AddRange(wearableItemIDs.Select(InventoryItem.GetFromID).OfType<WearableItem>());
            return wearableItems;
        }

        private List<WearableItem> GetAttachedWearableItems()
        {
            var wearableItems = new List<WearableItem>();
            foreach (Transform wearableObject in attachedObjectsRoot)
            {
                if (!wearableObject.TryGetComponent(out Wearable wearable)) { continue; }
                WearableItem wearableItem = wearable.GetWearableItem();
                if (wearableItem == null) { continue; }

                wearableItems.Add(wearableItem);
            }
            return wearableItems;
        }
        #endregion
    }
}

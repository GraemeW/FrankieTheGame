using Frankie.Saving;
using Frankie.Stats;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Inventory
{
    public class WearablesLink : MonoBehaviour, IModifierProvider, ISaveable
    {
        // Tunables
        [SerializeField] Transform attachedObjectsRoot = null;

        // Cached References
        CharacterSpriteLink characterSpriteLink = null;

        // Unity Methods
        private void Awake()
        {
            characterSpriteLink = GetComponent<CharacterSpriteLink>();
        }

        // Public Methods
        public CharacterSpriteLink GetCharacterSpriteLink() => characterSpriteLink;

        public Transform GetAttachedObjectsRoot() => attachedObjectsRoot;

        // Modifier Interface
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

        // Save Interface
        public LoadPriority GetLoadPriority()
        {
            return LoadPriority.ObjectProperty;
        }

        public SaveState CaptureState()
        {
            List<string> wearableItemIDs = new List<string>();
            foreach (Transform wearableObject in attachedObjectsRoot)
            {
                if (wearableObject.TryGetComponent(out Wearable wearable))
                {
                    WearableItem wearableItem = wearable.GetWearableItem();
                    if (wearableItem == null) { continue; }

                    wearableItemIDs.Add(wearableItem.GetItemID());
                }
            }

            SaveState saveState = new SaveState(GetLoadPriority(), wearableItemIDs);
            return saveState;
        }

        public void RestoreState(SaveState saveState)
        {
            List<string> wearableItemIDs = saveState.GetState(typeof(List<string>)) as List<string>;
            if (wearableItemIDs == null) { return; }

            foreach (string wearableItemID in wearableItemIDs)
            {
                if (string.IsNullOrEmpty(wearableItemID)) { continue; }

                WearableItem wearableItem = InventoryItem.GetFromID(wearableItemID) as WearableItem;
                if (wearableItem == null) { continue; }

                Wearable wearablePrefab = wearableItem.GetWearablePrefab();
                if (wearablePrefab == null) { continue; }
                Wearable wearable = Instantiate(wearablePrefab, attachedObjectsRoot);

                wearable.AttachToCharacter(this);
            }
        }
    }
}

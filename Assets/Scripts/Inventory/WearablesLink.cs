using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using LowDefMustard.Core;
using LowDefMustard.Utils;
using Frankie.Control;
using UnityEngine.AddressableAssets;

namespace Frankie.Inventory
{
    public class WearablesLink : MonoBehaviour
    {
        // Notes:
        //  - Wearable prefabs load when worn (AddressablesKeyedAssetCache, keyed by item GUID ~ released when taken off)
        //  - As a result, the wearable appears once its load completes
        
        // Tunables
        [SerializeField] private Transform attachedObjectsRoot;
        
        // Editor Field References
        internal const string attachedObjectsRootField = nameof(attachedObjectsRoot);
        
        // State
        private readonly AddressablesKeyedAssetCache<WearableItem, SingleAssetSlot, GameObject> wearablePrefabs = new(LDMLogCategory.Inventory, GetOwnerKey, GetReference);

        // Cached References
        private CharacterMoveLink characterMoveLink;

        #region UnityMethods
        private void Awake()
        {
            characterMoveLink = GetComponent<CharacterMoveLink>();
        }

        private void OnDestroy()
        {
            wearablePrefabs.ReleaseAll();
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
            if (wearableItem == null || wearablePrefabs.IsRequested(wearableItem, SingleAssetSlot.Asset) || IsWearingItem(wearableItem, out _)) { return; }

            AsyncOperationHandle<GameObject> wearableLoad = wearablePrefabs.StartLoad(wearableItem, SingleAssetSlot.Asset);
            if (!wearableLoad.IsValid()) { return; } // No wearable prefab
            if (wearableLoad.IsDone) { AttachWearable(wearableItem); }
            else { wearableLoad.Completed += _ => AttachWearable(wearableItem); }
        }

        public void RemoveWearable(WearableItem wearableItem)
        {
            if (wearableItem == null) { return; }
            if (IsWearingItem(wearableItem, out Wearable wearable)) { Destroy(wearable.gameObject); }
            wearablePrefabs.Release(wearableItem); // Also cancels a wearable still loading
        }
        #endregion
        
        #region PrivateCache
        private static string GetOwnerKey(WearableItem wearableItem) => wearableItem.GetGUID();
        private static AssetReference GetReference(WearableItem wearableItem, SingleAssetSlot _) => wearableItem.GetWearablePrefab();
        #endregion
        
        #region PrivateMethods
        private void AttachWearable(WearableItem wearableItem)
        {
            // Skipped if taken off (or destroyed) while loading, or already attached (e.g. taken off + put back on before the first load finished)
            if (this == null || !wearablePrefabs.IsRequested(wearableItem, SingleAssetSlot.Asset) || IsWearingItem(wearableItem, out _)) { return; }
            if (!wearablePrefabs.TryGet(wearableItem, SingleAssetSlot.Asset, out GameObject wearablePrefab)) { wearablePrefabs.Release(wearableItem); return; } // Load failure logged by the cache

            if (!wearablePrefab.TryGetComponent(out Wearable _))
            {
                LDMLog.Warning(LDMLogCategory.Inventory, $"The wearable prefab for '{wearableItem.name}' has no Wearable component, so it isn't worn.", wearableItem);
                wearablePrefabs.Release(wearableItem);
                return;
            }

            GameObject spawnedObject = Instantiate(wearablePrefab, attachedObjectsRoot);
            spawnedObject.GetComponent<Wearable>().Setup(wearableItem, this);
        }

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

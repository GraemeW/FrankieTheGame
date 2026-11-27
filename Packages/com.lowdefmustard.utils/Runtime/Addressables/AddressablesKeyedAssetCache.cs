using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using LowDefMustard.Core;
using Object = UnityEngine.Object;

namespace LowDefMustard.Utils
{
    public sealed class AddressablesKeyedAssetCache<TOwner, TSlot, TAsset> where TSlot : struct, Enum where TAsset : Object
    {
        // Notes:
        //  - Keyed by the owner's key (+ slot) via `getOwnerKey` (notably -> NOT by its reference) ~ such that a build copy of an owner hits the same entry
        //  - StartPreload is non-blocking (safe during a scene load);  Preload waits;  TryGet stays synchronous for spawn call sites

        // State
        private readonly LDMLogCategory category;
        private readonly Func<TOwner, string> getOwnerKey;
        private readonly Func<TOwner, TSlot, AssetReference> getReference;
        private readonly TSlot[] slots = (TSlot[])Enum.GetValues(typeof(TSlot));
        private readonly Dictionary<HandleKey<TSlot>, AsyncOperationHandle<TAsset>> handles = new();

        // Constructor
        public AddressablesKeyedAssetCache(LDMLogCategory category, Func<TOwner, string> getOwnerKey, Func<TOwner, TSlot, AssetReference> getReference)
        {
            this.category = category;
            this.getOwnerKey = getOwnerKey;
            this.getReference = getReference;
            
            if (this.getOwnerKey == null) { LogMissingDelegate(nameof(getOwnerKey), "every owner shares one entry"); this.getOwnerKey = _ => string.Empty; }
            if (this.getReference == null) { LogMissingDelegate(nameof(getReference), "it loads nothing"); this.getReference = (_, _) => null; }
        }

        #region Loading
        public List<AsyncOperationHandle<TAsset>> StartPreload(IEnumerable<TOwner> owners)
        {
            // Non-blocking;  kicks off StartLoad for each owner and passes back the handles
            return GetOwners(owners)
                .SelectMany(owner => slots.Select(slot => StartLoad(owner, slot)))
                .Where(handle => handle.IsValid())
                .ToList();
        }

        public AsyncOperationHandle<TAsset> StartLoad(TOwner owner, TSlot slot)
        {
            // Non-blocking;  hands back the existing handle if this owner's slot is already loading / loaded (default when it has no reference)
            if (!TryGetReference(owner, slot, out AssetReference assetReference)) { return default; }

            HandleKey<TSlot> key = GetKey(owner, slot);
            if (handles.TryGetValue(key, out AsyncOperationHandle<TAsset> existingHandle) && existingHandle.IsValid()) { return existingHandle; }

            AsyncOperationHandle<TAsset> handle = Addressables.LoadAssetAsync<TAsset>(assetReference.RuntimeKey);
            handles[key] = handle;
            Object context = owner as Object;
            handle.Completed += completedHandle =>
            {
                if (completedHandle.Status == AsyncOperationStatus.Succeeded) { return; }
                LDMLog.Warning(category, $"Couldn't load the {typeof(TAsset).Name} for {typeof(TOwner).Name} '{key.ownerKey}' ({key.slot}) ({completedHandle.OperationException?.Message}). Check it's an Addressables entry.", context);
            };
            return handle;
        }
        
        public IEnumerator Preload(IEnumerable<TOwner> owners)
        {
            // Blocking in use (Coroutine);  waits on each pending handle completion before yielding to the next
            foreach (AsyncOperationHandle<TAsset> pendingHandle in StartPreload(owners).Where(handle => !handle.IsDone)) { yield return pendingHandle; }
        }
        #endregion

        #region Lookups
        public bool IsReady(IEnumerable<TOwner> owners) => GetOwners(owners).All(owner => slots.All(slot => IsReady(owner, slot)));

        public bool IsRequested(TOwner owner, TSlot slot) => !IsNull(owner) && handles.TryGetValue(GetKey(owner, slot), out AsyncOperationHandle<TAsset> handle) && handle.IsValid();

        public bool TryGet(TOwner owner, TSlot slot, out TAsset asset)
        {
            asset = null;
            if (!TryGetReference(owner, slot, out _)) { return false; } // Owner has no reference in this slot

            if (!IsRequested(owner, slot)) { LDMLog.Warning(category, $"The {typeof(TAsset).Name} for {typeof(TOwner).Name} '{getOwnerKey(owner)}' ({slot}) wasn't preloaded, so loading now (may hitch). Preload it before it's needed.", owner as Object); }
            AsyncOperationHandle<TAsset> handle = StartLoad(owner, slot);

            if (!handle.IsDone)
            {
                if (AddressablesSceneLoadState.isLoadingScene)
                {
                    LDMLog.Warning(category, $"The {typeof(TAsset).Name} for {typeof(TOwner).Name} '{getOwnerKey(owner)}' ({slot}) was needed during a scene load, before it finished loading. Waiting would deadlock the scene load, so it isn't used.", owner as Object);
                    return false;
                }
                handle.WaitForCompletion();
            }

            asset = handle.Status == AsyncOperationStatus.Succeeded ? handle.Result : null;
            return asset != null;
        }
        #endregion

        #region Release
        public void Release(TOwner owner)
        {
            if (IsNull(owner)) { return; }
            string ownerKey = getOwnerKey(owner);
            foreach (TSlot slot in slots) { Release(new HandleKey<TSlot>(ownerKey, slot)); }
        }

        public void ReleaseAllExcept(IEnumerable<TOwner> keptOwners)
        {
            var keptOwnerKeys = new HashSet<string>(GetOwners(keptOwners).Select(getOwnerKey));
            foreach (HandleKey<TSlot> key in handles.Keys.Where(key => !keptOwnerKeys.Contains(key.ownerKey)).ToList()) { Release(key); }
        }

        public void ReleaseAll()
        {
            foreach (HandleKey<TSlot> key in handles.Keys.ToList()) { Release(key); }
        }
        #endregion

        #region PrivateMethods
        private void LogMissingDelegate(string delegateName, string consequence) { LDMLog.Error(category, $"The {typeof(TAsset).Name} cache for {typeof(TOwner).Name} was created without '{delegateName}', so {consequence}. Pass it to the constructor."); }
        private static bool IsNull(TOwner owner) => owner == null || (owner is Object unityObject && unityObject == null); // Unity's destroyed-object check, for Object owners
        private static IEnumerable<TOwner> GetOwners(IEnumerable<TOwner> owners) => (owners ?? Enumerable.Empty<TOwner>()).Where(owner => !IsNull(owner));

        private HandleKey<TSlot> GetKey(TOwner owner, TSlot slot) => new(getOwnerKey(owner), slot);

        private bool TryGetReference(TOwner owner, TSlot slot, out AssetReference assetReference)
        {
            assetReference = IsNull(owner) ? null : getReference(owner, slot);
            return assetReference != null && assetReference.RuntimeKeyIsValid();
        }

        private bool IsReady(TOwner owner, TSlot slot)
        {
            if (!TryGetReference(owner, slot, out _)) { return true; } // Nothing to load
            return handles.TryGetValue(GetKey(owner, slot), out AsyncOperationHandle<TAsset> handle) && handle.IsValid() && handle.IsDone;
        }

        private void Release(HandleKey<TSlot> key)
        {
            if (handles.TryGetValue(key, out AsyncOperationHandle<TAsset> handle) && handle.IsValid()) { Addressables.Release(handle); }
            handles.Remove(key);
        }
        #endregion
    }
}

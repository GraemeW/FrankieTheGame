using System.Collections.Generic;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

namespace LowDefMustard.Utils
{
    public sealed class SwappableAssetHandle<T> where T : Object
    {
        // Notes:
        //  - A replaced handle stays loaded until ReleaseReplaced ~ i.e. until the caller is off that asset (releasing a streamed clip mid-play cuts it)
        //  - Each load adds a reference (the same asset shares one Addressables operation), so releasing a replaced load never unloads a still-current asset

        // State
        private readonly List<AsyncOperationHandle<T>> replacedHandles = new();
        public AsyncOperationHandle<T> current { get; private set; }

        #region PublicMethods
        public bool TryLoad(AssetReferenceT<T> assetReference)
        {
            if (assetReference == null || !assetReference.RuntimeKeyIsValid()) { return false; }

            if (current.IsValid()) { replacedHandles.Add(current); }
            current = Addressables.LoadAssetAsync<T>(assetReference.RuntimeKey);
            return true;
        }

        public void RetireCurrent()
        {
            if (current.IsValid()) { replacedHandles.Add(current); }
            current = default;
        }

        public void ReleaseReplaced()
        {
            foreach (AsyncOperationHandle<T> replacedHandle in replacedHandles) { if (replacedHandle.IsValid()) { Addressables.Release(replacedHandle); } }
            replacedHandles.Clear();
        }

        public void ReleaseAll()
        {
            ReleaseReplaced();
            if (current.IsValid()) { Addressables.Release(current); }
            current = default;
        }
        #endregion
    }
}

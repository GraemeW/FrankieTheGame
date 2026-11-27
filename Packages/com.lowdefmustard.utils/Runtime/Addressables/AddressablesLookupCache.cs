using System;
using System.Collections.Generic;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using LowDefMustard.Core;
using Object = UnityEngine.Object;

namespace LowDefMustard.Utils
{
    public sealed class AddressablesLookupCache<T> : IAddressablesCacheLifecycle where T : Object
    {
        // Notes:
        //  - StartBuild loads async (e.g. warmed up behind a splash screen), lookups complete it synchronously on demand
        //  - never blocks during an Addressables scene load:  WaitForCompletion there otherwise deadlocks the scene load in builds
        //  - optional secondary key for a second lookup (e.g. Zone by scene name)
        //  - an asset with a blank key is logged + skipped (the rest still load);  a blank secondary key just skips that lookup
        //  - Seed:  replaces the lookups directly (e.g. tests), counts as built + starts no load

        // State
        private readonly LDMLogCategory category;
        private readonly string label;
        private readonly Func<T, string> getKey;
        private readonly Func<T, string> getSecondaryKey;
        
        private AsyncOperationHandle<IList<T>> handle;
        private bool isBuildLogged;
        private int loadVersion; // Ignores completions from a load replaced by Release / Reset / Seed
        
        // Lookups
        public Dictionary<string, T> lookup { get; private set; }
        public Dictionary<string, T> secondaryLookup { get; private set; }

        // Constructor
        public AddressablesLookupCache(LDMLogCategory category, string label, Func<T, string> getKey, Func<T, string> getSecondaryKey = null)
        {
            this.category = category;
            this.label = label;
            this.getKey = getKey ?? throw new ArgumentNullException(nameof(getKey));
            this.getSecondaryKey = getSecondaryKey;
        }

        #region Lookups
        public T Get(string key) => GetFrom(key, false);
        public T GetBySecondaryKey(string key) => GetFrom(key, true);

        public IReadOnlyDictionary<string, T> GetAll()
        {
            BuildIfEmpty();
            return lookup;
        }

        public void Add(T asset)
        {
            string key = getKey(asset);
            if (string.IsNullOrWhiteSpace(key)) { AddressablesCacheLog.LogMissingKey(category, label, asset); return; }

            lookup ??= new Dictionary<string, T>();
            string duplicateKey = lookup.TryGetValue(key, out T existing) ? key : null;
            lookup[key] = asset;

            string secondaryKey = getSecondaryKey?.Invoke(asset);
            if (!string.IsNullOrWhiteSpace(secondaryKey))
            {
                secondaryLookup ??= new Dictionary<string, T>();
                if (duplicateKey == null && secondaryLookup.TryGetValue(secondaryKey, out existing)) { duplicateKey = secondaryKey; }
                secondaryLookup[secondaryKey] = asset;
            }

            if (duplicateKey != null) { AddressablesCacheLog.LogDuplicate(category, label, duplicateKey, existing, asset); }
        }

        public void Seed(Dictionary<string, T> seededLookup, Dictionary<string, T> seededSecondaryLookup = null)
        {
            Release();
            lookup = seededLookup;
            secondaryLookup = seededSecondaryLookup;
            isBuildLogged = true;
        }
        #endregion

        #region Lifecycle
        public void StartBuild()
        {
            if (lookup != null) { return; }
            Release();
            lookup = new Dictionary<string, T>();
            secondaryLookup = getSecondaryKey != null ? new Dictionary<string, T>() : null;
            isBuildLogged = false;
            handle = Addressables.LoadAssetsAsync<T>(label, Add);
            int startedLoadVersion = ++loadVersion;
            handle.Completed += _ => { if (startedLoadVersion == loadVersion) { LogBuildOnce(); } }; // Callback cleared by Addressables after Completed
        }

        public void BuildIfEmpty()
        {
            StartBuild();
            if (IsReady()) { return; }
            if (AddressablesSceneLoadState.isLoadingScene)
            {
                LDMLog.Warning(category, $"The '{label}' cache was read during a scene load, before it finished loading. Waiting would deadlock the scene load, so lookups may miss. Read it after the scene loads (Start), or warm it up before loading scenes.");
                return;
            }

            handle.WaitForCompletion();
            LogBuildOnce();
        }

        public bool IsReady()
        {
            if (lookup == null || (handle.IsValid() && !handle.IsDone)) { return false; }
            LogBuildOnce();
            return true;
        }

        public void Release()
        {
            if (handle.IsValid()) { Addressables.Release(handle); }
            handle = default;
            loadVersion++;
        }

        public void Reset()
        {
            Release();
            lookup = null;
            secondaryLookup = null;
        }
        #endregion

        #region PrivateMethods
        private T GetFrom(string key, bool isSecondary)
        {
            if (string.IsNullOrWhiteSpace(key)) { return null; }
            BuildIfEmpty();
            return (isSecondary ? secondaryLookup : lookup)?.GetValueOrDefault(key);
        }

        private void LogBuildOnce()
        {
            if (isBuildLogged) { return; }
            isBuildLogged = true;
            AddressablesCacheLog.LogBuild(category, label, handle.IsValid() && handle.Status == AsyncOperationStatus.Succeeded, lookup?.Count ?? 0);
        }
        #endregion
    }
}

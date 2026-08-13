using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LowDefMustard.Utils
{
    [Serializable]
    public class EnumKeyedCollection<TEnum, TData> : IEnumKeyedCollection, IEnumerable<(TEnum key, TData value)>, ISerializationCallbackReceiver where TEnum : struct, Enum
    {
        // Tunables
        [SerializeField] private List<Entry> entries = new();
        
        #region Cache
        private Dictionary<TEnum, TData> cache;
        private void EnsureCache() { if (cache == null || cache.Count != entries.Count) { RebuildCache(); } }
        private void RebuildCache()
        {
            cache = new Dictionary<TEnum, TData>(entries.Count);
            foreach (Entry entry in entries) { cache[entry.key] = entry.value; }
        }
        #endregion
        
        #region Access
        public bool TryGet(TEnum key, out TData value)
        {
            EnsureCache();
            return cache.TryGetValue(key, out value);
        }

        public void Set(TEnum key, TData value)
        {
            EnsureCache();
            cache[key] = value;

            for (int i = 0; i < entries.Count; i++)
            {
                if (!EqualityComparer<TEnum>.Default.Equals(entries[i].key, key)) { continue; }
                Entry entry = entries[i];
                entry.value = value;
                entries[i] = entry;
                return;
            }
            entries.Add(new Entry { key = key, value = value });
        }

        public IEnumerator<(TEnum key, TData value)> GetEnumerator()
        {
            foreach (TEnum enumValue in (TEnum[])Enum.GetValues(typeof(TEnum)))
            {
                TryGet(enumValue, out TData value);
                yield return (enumValue, value);
            }
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        #endregion

        #region IEnumKeyedCollection
        Type IEnumKeyedCollection.GetEnumType() => typeof(TEnum);
        string IEnumKeyedCollection.GetListName() => nameof(entries);
        
        void IEnumKeyedCollection.SyncEntriesToEnum()
        {
            var enumValues = (TEnum[])Enum.GetValues(typeof(TEnum));

            var byKey = new Dictionary<TEnum, Entry>(entries.Count);
            foreach (Entry entry in entries) { byKey[entry.key] = entry; } // last-wins on accidental duplicate key

            var reconciled = new List<Entry>(enumValues.Length);
            foreach (TEnum enumValue in enumValues)
            {
                reconciled.Add(byKey.TryGetValue(enumValue, out Entry existing) ? existing : new Entry { key = enumValue, value = default });
                byKey.Remove(enumValue);
            }
            
            foreach (Entry orphan in byKey.Values)
            {
                Debug.LogWarning($"[EnumKeyedCollection] Dropping entry for removed enum member '{orphan.key}' (value was: {orphan.value}). This data is now unreachable.");
            }

            entries = reconciled;
            cache = null; // invalidate; lazily rebuilt on next TryGet
        }
        #endregion
        
        #region ISerializationCallbackReceiver
        public void OnBeforeSerialize() { }

        public void OnAfterDeserialize()
        {
            // Reliable rebuild point: domain reload, scene/asset load, Undo/Redo.
            RebuildCache();
        }
        #endregion
        
        #region DataStructures
        [Serializable]
        private struct Entry
        {
            public TEnum key;
            public TData value;
        }
        #endregion
    }
}

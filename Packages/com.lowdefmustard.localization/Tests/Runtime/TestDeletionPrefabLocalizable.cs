using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Localization.Tables;

namespace LowDefMustard.Localization.Tests
{
    // Testing Notes:
    //  - This must be a. top-level (not nested inside a test script) && b. in Runtime Assembly (resolvable outside Editor)
    //  - Required for any prefab that gets saved to and reloaded from a prefab asset on disk
    
    public sealed class TestDeletionPrefabLocalizable : MonoBehaviour, ILocalizableCore
    {
        public string iCachedName { get; set; }
        public List<string> extraKeys = new();
        public Enum localizationTableTypeValue => TestTableType.ScratchAssetDeletion;

        public List<TableEntryReference> GetLocalizationEntries()
        {
            var entries = new List<TableEntryReference> { "Deletion.Prefab.Shared" };
            entries.AddRange(extraKeys.Select(key => (TableEntryReference)key));
            return entries;
        }
    }
}

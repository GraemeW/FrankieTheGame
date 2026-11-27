using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Unity.Scripting.LifecycleManagement;
using LowDefMustard.Core;

namespace LowDefMustard.Utils.Tests.Editor
{
    public class AddressablesLookupCacheTests
    {
        // Tested:
        //  - Constructor - a null getKey throws
        //  - Add - blank key warns + skips, blank secondary key skips that lookup only, duplicate key logs + overwrites
        //  - Seed - counts as built (lookups use it, no load starts)
        // Not Tested:
        //  - StartBuild / BuildIfEmpty / IsReady against a label load ~ consumers' cache tests should cover it

        // Const Tunables
        private const string _label = "TestAsset";
        [NoAutoStaticsCleanup] private static readonly Regex _missingKeyLog = new($"has no {_label} ID\\. Skipping it");
        [NoAutoStaticsCleanup] private static readonly Regex _duplicateLog = new($"Duplicate {_label} ID 'Dup'");

        // State
        private readonly TestObjectTracker tracker = new();

        #region Setup
        [TearDown]
        public void TearDown()
        {
            tracker.DestroyAll();
        }
        #endregion

        #region Helpers
        private static AddressablesLookupCache<GameObject> CreateCache(Func<GameObject, string> getSecondaryKey = null)
        {
            var cache = new AddressablesLookupCache<GameObject>(LDMLogCategory.Utilities, _label, asset => asset.name, getSecondaryKey);
            cache.Seed(new Dictionary<string, GameObject>(), getSecondaryKey != null ? new Dictionary<string, GameObject>() : null);
            return cache;
        }
        #endregion

        #region Tests
        [Test]
        public void Constructor_NullGetKey_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => _ = new AddressablesLookupCache<GameObject>(LDMLogCategory.Utilities, _label, null));
        }

        [Test]
        public void Add_BlankKey_LogsWarningAndSkips()
        {
            AddressablesLookupCache<GameObject> cache = CreateCache(_ => "Secondary");

            LogAssert.Expect(LogType.Warning, _missingKeyLog);
            cache.Add(tracker.CreateGameObject(" "));

            Assert.IsEmpty(cache.lookup);
            Assert.IsEmpty(cache.secondaryLookup);
        }

        [Test]
        public void Add_BlankSecondaryKey_AddsPrimaryOnly()
        {
            AddressablesLookupCache<GameObject> cache = CreateCache(_ => "");
            GameObject asset = tracker.CreateGameObject("AssetA");

            cache.Add(asset);

            Assert.AreSame(asset, cache.Get("AssetA"));
            Assert.IsEmpty(cache.secondaryLookup);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void Add_DuplicateKey_LogsErrorAndOverwrites()
        {
            AddressablesLookupCache<GameObject> cache = CreateCache();
            cache.Add(tracker.CreateGameObject("Dup"));
            GameObject secondAsset = tracker.CreateGameObject("Dup");

            LogAssert.Expect(LogType.Error, _duplicateLog);
            cache.Add(secondAsset);

            Assert.AreSame(secondAsset, cache.Get("Dup"));
        }

        [Test]
        public void Seed_LookupsUseSeededEntries_WithoutLoading()
        {
            var cache = new AddressablesLookupCache<GameObject>(LDMLogCategory.Utilities, _label, asset => asset.name);
            GameObject asset = tracker.CreateGameObject("AssetA");

            cache.Seed(new Dictionary<string, GameObject> { { "AssetA", asset } });

            Assert.IsTrue(cache.IsReady());
            Assert.AreSame(asset, cache.Get("AssetA"));
            LogAssert.NoUnexpectedReceived(); // i.e. no build log ~ a load would log one
        }
        #endregion
    }
}

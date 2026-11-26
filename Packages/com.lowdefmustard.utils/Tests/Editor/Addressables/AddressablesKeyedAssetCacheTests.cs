using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.TestTools;
using Unity.Scripting.LifecycleManagement;
using LowDefMustard.Core;

namespace LowDefMustard.Utils.Tests.Editor
{
    public class AddressablesKeyedAssetCacheTests
    {
        // Tested:
        //  - Constructor - null delegates log an Error each + the cache behaves empty (no throw)
        //  - Preload, then TryGet - hits every slot without a warning;  IsReady after
        //  - TryGet on a slot never requested - loads anyway + warns (the miss path)
        //  - TryGet on an empty slot - false, no warning;  IsReady treats it as nothing to load
        //  - An owner copy with the same key hits the original's entry
        //  - StartLoad / IsRequested - a second load hands back the same handle
        //  - Release (one owner) / ReleaseAllExcept / ReleaseAll
        // Not Tested:
        //  - Lookup during a scene load (Warning, no asset) - needs a load still in flight, which the Asset Database catalogue completes too quickly to catch
        // Note:  Loads the project's own AudioClip entries (any two) ~ skips if the project has fewer

        // State
        private AddressablesKeyedAssetCache<TestOwner, TestSlot, AudioClip> cache;
        private List<string> audioClipGuids;
        [NoAutoStaticsCleanup] private static readonly Regex _missWarning = new("wasn't preloaded");

        #region DataStructures
        private enum TestSlot { First, Second }

        private sealed class TestOwner
        {
            public readonly string key;
            public readonly Dictionary<TestSlot, AssetReference> references = new();
            public TestOwner(string key) => this.key = key;
        }
        #endregion

        #region Setup
        [SetUp]
        public void SetUp()
        {
            audioClipGuids = AddressableAssetSettingsDefaultObject.Settings == null ? new List<string>() : AddressableAssetSettingsDefaultObject.Settings.groups
                .Where(group => group != null)
                .SelectMany(group => group.entries)
                .Where(entry => AssetDatabase.GetMainAssetTypeAtPath(entry.AssetPath) == typeof(AudioClip))
                .Take(2)
                .Select(entry => entry.guid)
                .ToList();
            if (audioClipGuids.Count < 2) { Assert.Ignore("Needs two Addressables AudioClip entries in the project"); }

            cache = new AddressablesKeyedAssetCache<TestOwner, TestSlot, AudioClip>(LDMLogCategory.Utilities, owner => owner.key, (owner, slot) => owner.references.GetValueOrDefault(slot));
        }

        [TearDown]
        public void TearDown() => cache?.ReleaseAll();
        #endregion

        #region Helpers
        private TestOwner CreateOwner(string key, bool hasSecondSlot = true)
        {
            var owner = new TestOwner(key);
            owner.references[TestSlot.First] = new AssetReferenceAudioClip(audioClipGuids[0]);
            if (hasSecondSlot) { owner.references[TestSlot.Second] = new AssetReferenceAudioClip(audioClipGuids[1]); }
            return owner;
        }

        private void PreloadAndWait(params TestOwner[] owners)
        {
            foreach (var handle in cache.StartPreload(owners)) { handle.WaitForCompletion(); }
        }
        #endregion

        #region Tests
        [Test]
        public void Constructor_NullDelegates_LogsErrorsAndBehavesEmpty()
        {
            LogAssert.Expect(LogType.Error, new Regex("created without 'getOwnerKey'"));
            LogAssert.Expect(LogType.Error, new Regex("created without 'getReference'"));
            var emptyCache = new AddressablesKeyedAssetCache<TestOwner, TestSlot, AudioClip>(LDMLogCategory.Utilities, null, null);
            TestOwner owner = CreateOwner("OwnerA");

            Assert.IsFalse(emptyCache.StartLoad(owner, TestSlot.First).IsValid());
            Assert.IsFalse(emptyCache.TryGet(owner, TestSlot.First, out AudioClip asset));
            Assert.IsNull(asset);
            Assert.IsTrue(emptyCache.IsReady(new[] { owner }));
            Assert.IsFalse(emptyCache.IsRequested(owner, TestSlot.First));
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void TryGet_AfterPreload_HitsEverySlotWithoutWarning()
        {
            TestOwner owner = CreateOwner("OwnerA");
            PreloadAndWait(owner);

            Assert.IsTrue(cache.IsReady(new[] { owner }));
            Assert.IsTrue(cache.TryGet(owner, TestSlot.First, out AudioClip first));
            Assert.IsTrue(cache.TryGet(owner, TestSlot.Second, out AudioClip second));
            Assert.IsNotNull(first);
            Assert.IsNotNull(second);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void TryGet_NeverRequested_LoadsAndWarns()
        {
            TestOwner owner = CreateOwner("OwnerA");
            LogAssert.Expect(LogType.Warning, _missWarning);

            Assert.IsTrue(cache.TryGet(owner, TestSlot.First, out AudioClip asset));
            Assert.IsNotNull(asset);
        }

        [Test]
        public void TryGet_EmptySlot_ReturnsFalseWithoutWarning()
        {
            TestOwner owner = CreateOwner("OwnerA", false);
            PreloadAndWait(owner);

            Assert.IsFalse(cache.TryGet(owner, TestSlot.Second, out AudioClip asset));
            Assert.IsNull(asset);
            Assert.IsTrue(cache.IsReady(new[] { owner }));
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void TryGet_OwnerCopyWithSameKey_HitsTheOriginalsEntry()
        {
            TestOwner owner = CreateOwner("OwnerA");
            TestOwner ownerCopy = CreateOwner("OwnerA"); // Same key, different object (e.g. a bundle-loaded copy)
            PreloadAndWait(owner);

            Assert.IsTrue(cache.IsRequested(ownerCopy, TestSlot.First));
            Assert.IsTrue(cache.TryGet(ownerCopy, TestSlot.First, out AudioClip asset));
            Assert.IsNotNull(asset);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void StartLoad_Twice_ReturnsTheSameHandle()
        {
            TestOwner owner = CreateOwner("OwnerA");
            Assert.IsFalse(cache.IsRequested(owner, TestSlot.First));

            var firstHandle = cache.StartLoad(owner, TestSlot.First);
            var secondHandle = cache.StartLoad(owner, TestSlot.First);

            Assert.IsTrue(cache.IsRequested(owner, TestSlot.First));
            Assert.AreEqual(firstHandle, secondHandle);
        }

        [Test]
        public void Release_Owner_ReleasesOnlyItsSlots()
        {
            TestOwner releasedOwner = CreateOwner("OwnerA");
            TestOwner keptOwner = CreateOwner("OwnerB");
            PreloadAndWait(releasedOwner, keptOwner);

            cache.Release(releasedOwner);

            Assert.IsFalse(cache.IsRequested(releasedOwner, TestSlot.First));
            Assert.IsFalse(cache.IsRequested(releasedOwner, TestSlot.Second));
            Assert.IsTrue(cache.IsReady(new[] { keptOwner }));
        }

        [Test]
        public void ReleaseAllExcept_KeepsListedOwners_ReleasesOthers()
        {
            TestOwner keptOwner = CreateOwner("OwnerA");
            TestOwner releasedOwner = CreateOwner("OwnerB");
            PreloadAndWait(keptOwner, releasedOwner);

            cache.ReleaseAllExcept(new[] { keptOwner });

            Assert.IsTrue(cache.IsReady(new[] { keptOwner }));
            Assert.IsFalse(cache.IsReady(new[] { releasedOwner }));
        }

        [Test]
        public void ReleaseAll_ReleasesEveryOwner()
        {
            TestOwner ownerA = CreateOwner("OwnerA");
            TestOwner ownerB = CreateOwner("OwnerB");
            PreloadAndWait(ownerA, ownerB);

            cache.ReleaseAll();

            Assert.IsFalse(cache.IsRequested(ownerA, TestSlot.First));
            Assert.IsFalse(cache.IsRequested(ownerB, TestSlot.Second));
        }
        #endregion
    }
}

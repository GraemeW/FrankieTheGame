using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using Unity.Scripting.LifecycleManagement;
using LowDefMustard.Utils.Editor;

namespace LowDefMustard.Utils.Tests.Editor
{
    public class AddressablesCacheInvalidatorTests
    {
        // Covers which caches an asset change resets, finding an owner's cache, and the IAddressablesCache contract on every owner
        // Not covered:  OnPostprocessAllAssets itself (needs asset imports) ~ verified manually

        #region DataStructures
        private class OwnerA { }
        private class DerivedFromA : OwnerA { }
        private class OwnerB { }
        private class Unrelated { }

        private sealed class CountingCache : IAddressablesCacheLifecycle
        {
            public int resetCount;
            public void StartBuild() { }
            public void BuildIfEmpty() { }
            public bool IsReady() => true;
            public void Release() { }
            public void Reset() => resetCount++;
        }

        private sealed class CacheOwner : IAddressablesCache
        {
            [NoAutoStaticsCleanup] public static readonly CountingCache cache = new();
            public static IAddressablesCacheLifecycle addressablesCache => cache;
        }

        private sealed class OwnerWithoutCache : IAddressablesCache { }

        private sealed class OwnerWithTwoCaches : IAddressablesCache
        {
            public static IAddressablesCacheLifecycle firstCache => CacheOwner.cache;
            public static IAddressablesCacheLifecycle secondCache => CacheOwner.cache;
        }
        #endregion

        #region GetCacheOwnersToReset
        [NoAutoStaticsCleanup] private static readonly Type[] _owners = { typeof(OwnerA), typeof(OwnerB) };

        [Test]
        public void GetCacheOwnersToReset_ResetAll_ReturnsEveryOwner()
        {
            CollectionAssert.AreEqual(_owners, AddressablesCacheInvalidator.GetCacheOwnersToReset(_owners, Enumerable.Empty<Type>(), true));
        }

        [Test]
        public void GetCacheOwnersToReset_DerivedAssetType_ResetsItsOwnerOnly()
        {
            CollectionAssert.AreEqual(new[] { typeof(OwnerA) }, AddressablesCacheInvalidator.GetCacheOwnersToReset(_owners, new[] { typeof(DerivedFromA), typeof(DerivedFromA) }, false));
        }

        [Test]
        public void GetCacheOwnersToReset_UnrelatedAssetType_ResetsNothing()
        {
            CollectionAssert.IsEmpty(AddressablesCacheInvalidator.GetCacheOwnersToReset(_owners, new[] { typeof(Unrelated) }, false));
        }
        #endregion

        #region AddressablesCacheOwners
        [Test]
        public void GetCache_OneCacheProperty_ReturnsItsCache()
        {
            int resetCount = CacheOwner.cache.resetCount; // Note:  Asset changes reset it too (an unrecorded deletion resets every owner)

            AddressablesCacheOwners.GetCache(typeof(CacheOwner)).Reset();

            Assert.AreSame(CacheOwner.cache, AddressablesCacheOwners.GetCache(typeof(CacheOwner)));
            Assert.AreEqual(resetCount + 1, CacheOwner.cache.resetCount);
        }

        [Test]
        public void GetCache_NoCacheProperty_ReturnsNull()
        {
            Assert.IsNull(AddressablesCacheOwners.GetCache(typeof(OwnerWithoutCache)));
        }

        [Test]
        public void GetCache_TwoCacheProperties_ReturnsNull()
        {
            Assert.IsNull(AddressablesCacheOwners.GetCache(typeof(OwnerWithTwoCaches)));
        }

        [Test]
        public void OwnerTypes_OutsideTestAssemblies_EachExposeOneCache()
        {
            // The IAddressablesCache contract ~ TypeCache spans every loaded assembly -> also checks consumers' caches
            List<Type> owners = AddressablesCacheOwners.GetOwnerTypes().Where(type => !TestAssemblyCheck.IsTestAssemblyType(type)).ToList();

            List<string> brokenOwners = owners.Where(owner => AddressablesCacheOwners.GetCacheProperties(owner).Count != 1 || AddressablesCacheOwners.GetCache(owner) == null).Select(owner => owner.FullName).ToList();
            CollectionAssert.IsEmpty(brokenOwners, "IAddressablesCache owners need exactly one public static IAddressablesCacheLifecycle property, returning their cache");
        }

        [Test]
        public void OwnerTypes_SubclassesOfAnOwner_DeclareNoCacheOfTheirOwn()
        {
            List<string> subclassesWithCaches = TypeCache.GetTypesDerivedFrom<IAddressablesCache>()
                .Where(type => type.BaseType != null && typeof(IAddressablesCache).IsAssignableFrom(type.BaseType) && !TestAssemblyCheck.IsTestAssemblyType(type))
                .Where(type => AddressablesCacheOwners.GetCacheProperties(type).Count > 0)
                .Select(type => type.FullName).ToList();
            CollectionAssert.IsEmpty(subclassesWithCaches, "Subclasses share their base type's cache");
        }

        [Test]
        public void OwnerTypes_ExcludeSubclassesOfAnOwner()
        {
            Assert.IsTrue(AddressablesCacheOwners.GetOwnerTypes().All(owner => owner.BaseType == null || !typeof(IAddressablesCache).IsAssignableFrom(owner.BaseType)));
        }
        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using LowDefMustard.Utils.Editor;

namespace LowDefMustard.Utils.Tests.Editor
{
    public class AddressablesCacheInvalidatorTests
    {
        // Covers which caches an asset change resets, and that every cache owner can be reset
        // Not covered:  OnPostprocessAllAssets itself (needs real asset imports) ~ verified live via a quest reimport

        #region DataStructures
        private class OwnerA { }
        private class DerivedFromA : OwnerA { }
        private class OwnerB { }
        private class Unrelated { }

        private sealed class ResettableCache : IAddressablesCache
        {
            public static int resetCount;
            public static void ResetCache() => resetCount++;
        }

        private sealed class NonResettableCache : IAddressablesCache { }
        #endregion

        #region GetCacheOwnersToReset
        private static readonly Type[] _owners = { typeof(OwnerA), typeof(OwnerB) };

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

        #region ResetMethod
        [Test]
        public void GetResetMethod_DeclaredStaticReset_IsFoundAndInvocable()
        {
            ResettableCache.resetCount = 0;

            AddressablesCacheInvalidator.GetResetMethod(typeof(ResettableCache)).Invoke(null, null);

            Assert.AreEqual(1, ResettableCache.resetCount);
        }

        [Test]
        public void GetResetMethod_NotDeclared_ReturnsNull()
        {
            Assert.IsNull(AddressablesCacheInvalidator.GetResetMethod(typeof(NonResettableCache)));
        }

        [Test]
        public void CacheOwnerTypes_OutsideTestAssemblies_AllDeclareResetCache()
        {
            // TypeCache spans every loaded assembly -> also checks consumers' caches
            List<Type> owners = AddressablesCacheInvalidator.GetCacheOwnerTypes().Where(type => !TestAssemblyCheck.IsTestAssemblyType(type)).ToList();

            List<string> missingReset = owners.Where(owner => AddressablesCacheInvalidator.GetResetMethod(owner) == null).Select(owner => owner.FullName).ToList();
            CollectionAssert.IsEmpty(missingReset, "IAddressablesCache owners need a public static ResetCache()");
        }

        [Test]
        public void CacheOwnerTypes_ExcludeSubclassesOfAnOwner()
        {
            Assert.IsTrue(AddressablesCacheInvalidator.GetCacheOwnerTypes().All(owner => owner.BaseType == null || !typeof(IAddressablesCache).IsAssignableFrom(owner.BaseType)));
        }
        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using LowDefMustard.Utils;
using LowDefMustard.Utils.Editor;
using LowDefMustard.Utils.Tests;
using Frankie.Utils.Addressables;

namespace Frankie.Tests.Editor
{
    public class AddressablesLoaderTests
    {
        // Tested:
        //  - StartAllCacheBuilds + AreAllCachesReady - ready once every label cache finishes (what the splash screen's last stage waits on)
        //  - GetCaches - lists every IAddressablesCache owner's cache (a missing one would never warm up)
        // Not Tested:
        //  - The splash gate itself (SplashMenuController) - runtime coroutine + input ~ verified in a dev player build

        #region Tests
        [Test]
        public void AreAllCachesReady_AfterStartAndComplete_IsTrue()
        {
            AddressablesLoader.StartAllCacheBuilds();

            foreach (IAddressablesCacheLifecycle cache in AddressablesLoader.GetCaches()) { cache.BuildIfEmpty(); }

            Assert.IsTrue(AddressablesLoader.AreAllCachesReady());
        }

        [Test]
        public void GetCaches_CoversEveryCacheOwner()
        {
            IAddressablesCacheLifecycle[] loaderCaches = AddressablesLoader.GetCaches();
            List<Type> owners = AddressablesCacheOwners.GetOwnerTypes().Where(type => !TestAssemblyCheck.IsTestAssemblyType(type)).ToList();

            List<string> missingOwners = owners.Where(owner => !loaderCaches.Contains(AddressablesCacheOwners.GetCache(owner))).Select(owner => owner.FullName).ToList();
            CollectionAssert.IsEmpty(missingOwners, "Add these IAddressablesCache owners to AddressablesLoader.GetCaches()");
        }
        #endregion
    }
}

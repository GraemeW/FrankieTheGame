using System;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using LowDefMustard.Utils.Tests;

namespace LowDefMustard.Zones.Tests.Editor
{
    public class SceneLoaderBaseTests
    {
        // Tested:
        //  - SetCurrentZone / SetLastZone - events + current zone caching
        //  - TryGetCurrentSceneGuid - the current zone's stored GUID;  warns + false (no out value) when it has none
        //  - LoadNewSceneAsync - a zone w/o a scene GUID warns + stops before loading (current zone + loading flag untouched)
        // Not Tested:
        //  - TryGetCurrentSceneGuid's zone-less fallback - editor reads the open scene's GUID, builds return false (needs a scene w/o a zone + a build)

        // Const
        private const string _sceneGuid = "0123456789abcdef0123456789abcdef";

        // State
        private readonly TestObjectTracker tracker = new();
        private Zone zoneA;
        private Zone zoneB;
        private bool originalIsCurrentlyLoading;
        private Zone originalCurrentZone;
        private Zone originalLastZone;
        private Action<Zone> zoneUpdatedHandler;
        private Action<Zone> leavingZoneHandler;

        #region Setup
        [SetUp]
        public void SetUp()
        {
            zoneA = tracker.Create<Zone>("ZoneA");
            zoneA.preventLocalizationForTests = true;
            zoneB = tracker.Create<Zone>("ZoneB");
            zoneB.preventLocalizationForTests = true;
            originalIsCurrentlyLoading = SceneLoaderBase.isCurrentlyLoading;
            originalCurrentZone = SceneLoaderBase.currentZone;
            originalLastZone = SceneLoaderBase.lastZone;
        }

        [TearDown]
        public void TearDown()
        {
            SceneLoaderBase.currentZone = originalCurrentZone;
            SceneLoaderBase.lastZone = originalLastZone;
            if (zoneUpdatedHandler != null) { SceneLoaderBase.zoneUpdated -= zoneUpdatedHandler; }
            if (leavingZoneHandler != null) { SceneLoaderBase.leavingZone -= leavingZoneHandler; }
            SceneLoaderBase.isCurrentlyLoading = originalIsCurrentlyLoading;
            tracker.DestroyAll();
        }
        #endregion

        #region Tests
        [Test]
        public void SetCurrentZone_InvokesZoneUpdated_AndCachesForGetCurrentZone()
        {
            Zone receivedZone = null;
            zoneUpdatedHandler = zone => receivedZone = zone;
            SceneLoaderBase.zoneUpdated += zoneUpdatedHandler;

            SceneLoaderBase.SetCurrentZone(zoneA);

            Assert.AreSame(zoneA, receivedZone);
            Assert.AreSame(zoneA, SceneLoaderBase.GetCurrentZone());
        }

        [Test]
        public void SetLastZone_InvokesLeavingZone_WithPriorCurrentZone()
        {
            SceneLoaderBase.SetCurrentZone(zoneA);

            Zone receivedZone = null;
            leavingZoneHandler = zone => receivedZone = zone;
            SceneLoaderBase.leavingZone += leavingZoneHandler;

            SceneLoaderBase.SetLastZone();

            Assert.AreSame(zoneA, receivedZone);
        }

        [Test]
        public void TryGetCurrentSceneGuid_ZoneWithGuid_ReturnsIt()
        {
            zoneA.sceneReference = new SceneReference("SceneA") { sceneGuid = _sceneGuid };
            SceneLoaderBase.SetCurrentZone(zoneA);

            Assert.IsTrue(SceneLoaderBase.TryGetCurrentSceneGuid(out string sceneGuid));
            Assert.AreEqual(_sceneGuid, sceneGuid);
        }

        [Test]
        public void TryGetCurrentSceneGuid_ZoneWithoutGuid_WarnsAndReturnsFalse()
        {
            zoneA.sceneReference = new SceneReference("SceneA");
            SceneLoaderBase.SetCurrentZone(zoneA);
            LogAssert.Expect(LogType.Warning, new Regex("Couldn't find the current scene's GUID"));

            Assert.IsFalse(SceneLoaderBase.TryGetCurrentSceneGuid(out string sceneGuid));
            Assert.IsNull(sceneGuid);
        }

        [Test]
        public void LoadNewSceneAsync_ZoneWithoutGuid_WarnsAndKeepsCurrentZone()
        {
            SceneLoaderBase.SetCurrentZone(zoneA);
            SceneLoaderBase.isCurrentlyLoading = false;
            zoneB.sceneReference = new SceneReference("SceneB");
            LogAssert.Expect(LogType.Warning, new Regex("Zone 'ZoneB' has no scene GUID, so its scene can't load"));

            Assert.IsFalse(SceneLoaderBase.LoadNewSceneAsync(zoneB).MoveNext()); // Stops before any load

            Assert.AreSame(zoneA, SceneLoaderBase.GetCurrentZone());
            Assert.IsFalse(SceneLoaderBase.isCurrentlyLoading);
        }
        #endregion
    }
}

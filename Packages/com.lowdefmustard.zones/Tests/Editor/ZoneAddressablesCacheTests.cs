using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using LowDefMustard.Utils.Tests;

namespace LowDefMustard.Zones.Tests.Editor
{
    public class ZoneAddressablesCacheTests
    {
        // State
        private readonly TestObjectTracker tracker = new();
        private Dictionary<string, Zone> originalZoneLookupCache;
        private Dictionary<string, Zone> originalSceneNameCache;

        #region Setup
        [SetUp]
        public void SetUp()
        {
            originalZoneLookupCache = Zone.zoneLookupCache;
            originalSceneNameCache = Zone.sceneNameCache;
        }

        [TearDown]
        public void TearDown()
        {
            Zone.SeedCache(originalZoneLookupCache, originalSceneNameCache);
            tracker.DestroyAll();
        }

        private Zone CreateZone(string zoneName, string sceneName)
        {
            var zone = tracker.Create<Zone>(zoneName);
            zone.preventLocalizationForTests = true;
            zone.sceneReference = sceneName;
            return zone;
        }
        #endregion

        #region Tests
        [Test]
        public void GetFromName_ReturnsSeededZone_WithoutBuildingCache()
        {
            Zone zoneA = CreateZone("ZoneA", "SceneA");
            Zone.SeedCache(new Dictionary<string, Zone> { { "ZoneA", zoneA } }, new Dictionary<string, Zone> { { "SceneA", zoneA } });

            Assert.AreSame(zoneA, Zone.GetFromName("ZoneA"));
        }

        [Test]
        public void GetFromName_UnknownName_ReturnsNull()
        {
            Zone.SeedCache(new Dictionary<string, Zone> { { "EmptyZone", null } }, new Dictionary<string, Zone> { { "EmptyScene", null } });

            Assert.IsNull(Zone.GetFromName("Missing"));
        }

        [Test]
        public void GetFromSceneName_ReturnsSeededZone()
        {
            Zone zoneA = CreateZone("ZoneA", "SceneA");
            Zone.SeedCache(new Dictionary<string, Zone> { { "ZoneA", zoneA } }, new Dictionary<string, Zone> { { "SceneA", zoneA } });

            Assert.AreSame(zoneA, Zone.GetFromSceneName("SceneA"));
        }

        [Test]
        public void GetFromSceneGuid_MatchingGuid_ReturnsThatZone()
        {
            Zone zoneA = CreateZone("ZoneA", "SceneA");
            Zone zoneB = CreateZone("ZoneB", "SceneB");
            zoneA.sceneReference.sceneGuid = "guid-a";
            zoneB.sceneReference.sceneGuid = "guid-b";
            Zone.SeedCache(new Dictionary<string, Zone> { { "ZoneA", zoneA }, { "ZoneB", zoneB } }, new Dictionary<string, Zone> { { "SceneA", zoneA }, { "SceneB", zoneB } });

            Assert.AreSame(zoneB, Zone.GetFromSceneGuid("guid-b"));
        }

        [TestCase(null)]
        [TestCase(" ")]
        [TestCase("guid-missing")]
        public void GetFromSceneGuid_BlankOrUnknownGuid_ReturnsNull(string sceneGuid)
        {
            Zone zoneA = CreateZone("ZoneA", "SceneA"); // No stored GUID ~ never matches
            Zone zoneB = CreateZone("ZoneB", "SceneB");
            zoneB.sceneReference.sceneGuid = "guid-b";
            Zone.SeedCache(new Dictionary<string, Zone> { { "ZoneA", zoneA }, { "ZoneB", zoneB } }, new Dictionary<string, Zone> { { "SceneA", zoneA }, { "SceneB", zoneB } });

            Assert.IsNull(Zone.GetFromSceneGuid(sceneGuid));
        }

        [Test]
        public void BuildIfEmpty_AlreadySeeded_LeavesCacheUntouched()
        {
            var seededCache = new Dictionary<string, Zone> { { "ZoneA", null } };
            Zone.SeedCache(seededCache, seededCache);

            Zone.addressablesCache.BuildIfEmpty();

            // If BuildZoneCache had actually run, sceneNameCache would be a new dictionary
            Assert.AreSame(seededCache, Zone.sceneNameCache);
            Assert.AreSame(seededCache, Zone.zoneLookupCache);
        }

        [Test]
        public void AddZoneToCache_AddsToBothLookups()
        {
            Zone.SeedCache(new Dictionary<string, Zone>(), new Dictionary<string, Zone>());
            Zone zoneA = CreateZone("ZoneA", "SceneA");

            Zone.AddZoneToCache(zoneA);

            Assert.AreSame(zoneA, Zone.zoneLookupCache["ZoneA"]);
            Assert.AreSame(zoneA, Zone.sceneNameCache["SceneA"]);
        }

        [Test]
        public void AddZoneToCache_DuplicateZoneName_LogsErrorAndOverwrites()
        {
            Zone.SeedCache(new Dictionary<string, Zone>(), new Dictionary<string, Zone>());
            Zone firstZone = CreateZone("Dup", "SceneA");
            Zone secondZone = CreateZone("Dup", "SceneB");

            Zone.AddZoneToCache(firstZone);

            LogAssert.Expect(LogType.Error, new Regex(@"\[Zones\]\S* Duplicate Zone ID "));
            Zone.AddZoneToCache(secondZone);

            Assert.AreSame(secondZone, Zone.zoneLookupCache["Dup"]);
        }
        
        [Test]
        public void AddZoneToCache_DuplicateSceneName_LogsErrorAndOverwrites()
        {
            Zone.SeedCache(new Dictionary<string, Zone>(), new Dictionary<string, Zone>());
            Zone firstZone = CreateZone("ZoneA", "SharedScene");
            Zone secondZone = CreateZone("ZoneB", "SharedScene");

            Zone.AddZoneToCache(firstZone);

            LogAssert.Expect(LogType.Error, new Regex(@"\[Zones\]\S* Duplicate Zone ID "));
            Zone.AddZoneToCache(secondZone);

            Assert.AreSame(secondZone, Zone.sceneNameCache["SharedScene"]);
        }
        #endregion
    }
}

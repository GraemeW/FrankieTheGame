using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace LowDefMustard.Zones.Tests.Editor
{
    public class ZoneAddressablesCacheTests
    {
        // State
        private readonly List<Zone> createdZones = new();
        private Dictionary<string, Zone> originalZoneLookupCache;
        private Dictionary<string, Zone> originalSceneReferenceCache;

        #region Setup
        [SetUp]
        public void SetUp()
        {
            originalZoneLookupCache = Zone.zoneLookupCache;
            originalSceneReferenceCache = Zone.sceneReferenceCache;
        }

        [TearDown]
        public void TearDown()
        {
            Zone.zoneLookupCache = originalZoneLookupCache;
            Zone.sceneReferenceCache = originalSceneReferenceCache;
            foreach (Zone zone in createdZones) { Object.DestroyImmediate(zone); }
            createdZones.Clear();
        }

        private Zone CreateZone(string zoneName, string sceneName)
        {
            var zone = ScriptableObject.CreateInstance<Zone>();
            zone.preventLocalizationForTests = true;
            zone.name = zoneName;
            zone.sceneReference = sceneName;
            createdZones.Add(zone);
            return zone;
        }
        #endregion

        #region Tests
        [Test]
        public void GetFromName_ReturnsSeededZone_WithoutBuildingCache()
        {
            Zone zoneA = CreateZone("ZoneA", "SceneA");
            Zone.zoneLookupCache = new Dictionary<string, Zone> { { "ZoneA", zoneA } };
            Zone.sceneReferenceCache = new Dictionary<string, Zone> { { "SceneA", zoneA } };

            Assert.AreSame(zoneA, Zone.GetFromName("ZoneA"));
        }

        [Test]
        public void GetFromName_UnknownName_ReturnsNull()
        {
            Zone.zoneLookupCache = new Dictionary<string, Zone>();
            Zone.sceneReferenceCache = new Dictionary<string, Zone>();

            Assert.IsNull(Zone.GetFromName("Missing"));
        }

        [Test]
        public void GetFromSceneReference_ReturnsSeededZone()
        {
            Zone zoneA = CreateZone("ZoneA", "SceneA");
            Zone.zoneLookupCache = new Dictionary<string, Zone> { { "ZoneA", zoneA } };
            Zone.sceneReferenceCache = new Dictionary<string, Zone> { { "SceneA", zoneA } };

            Assert.AreSame(zoneA, Zone.GetFromSceneReference("SceneA"));
        }

        [Test]
        public void BuildCacheIfEmpty_AlreadyBuilt_LeavesCacheUntouched()
        {
            var seededCache = new Dictionary<string, Zone>();
            Zone.sceneReferenceCache = seededCache;
            Zone.zoneLookupCache = new Dictionary<string, Zone>();

            Zone.BuildCacheIfEmpty();

            // If BuildZoneCache had actually run, sceneReferenceCache would be a new dictionary
            Assert.AreSame(seededCache, Zone.sceneReferenceCache);
        }

        [Test]
        public void AddZoneToCache_AddsToBothLookups()
        {
            Zone.zoneLookupCache = new Dictionary<string, Zone>();
            Zone.sceneReferenceCache = new Dictionary<string, Zone>();
            Zone zoneA = CreateZone("ZoneA", "SceneA");

            Zone.AddZoneToCache(zoneA);

            Assert.AreSame(zoneA, Zone.zoneLookupCache["ZoneA"]);
            Assert.AreSame(zoneA, Zone.sceneReferenceCache["SceneA"]);
        }

        [Test]
        public void AddZoneToCache_DuplicateZoneName_LogsErrorAndOverwrites()
        {
            Zone.zoneLookupCache = new Dictionary<string, Zone>();
            Zone.sceneReferenceCache = new Dictionary<string, Zone>();
            Zone firstZone = CreateZone("Dup", "SceneA");
            Zone secondZone = CreateZone("Dup", "SceneB");

            Zone.AddZoneToCache(firstZone);

            LogAssert.Expect(LogType.Error, new Regex("^Looks like there's a duplicate ID for objects:"));
            Zone.AddZoneToCache(secondZone);

            Assert.AreSame(secondZone, Zone.zoneLookupCache["Dup"]);
        }
        #endregion
    }
}

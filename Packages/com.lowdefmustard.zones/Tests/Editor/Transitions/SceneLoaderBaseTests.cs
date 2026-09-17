using System;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LowDefMustard.Zones.Tests.Editor
{
    public class SceneLoaderBaseTests
    {
        // State
        private Zone zoneA;
        private Zone originalCurrentZone;
        private Zone originalLastZone;
        private Action<Zone> zoneUpdatedHandler;
        private Action<Zone> leavingZoneHandler;

        #region Setup
        [SetUp]
        public void SetUp()
        {
            zoneA = ScriptableObject.CreateInstance<Zone>();
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
            Object.DestroyImmediate(zoneA);
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
        #endregion
    }
}

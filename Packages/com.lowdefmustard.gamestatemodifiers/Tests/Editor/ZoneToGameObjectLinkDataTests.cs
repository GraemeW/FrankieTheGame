using NUnit.Framework;

namespace LowDefMustard.GameStateModifiers.Tests.Editor
{
    public class ZoneToGameObjectLinkDataTests
    {
        [Test]
        public void Constructor_SetsAllFieldsFromArguments()
        {
            var data = new ZoneToGameObjectLinkData("Zone", "Object", "Parent", "guid-1");

            Assert.AreEqual("Zone", data.zoneName);
            Assert.AreEqual("Object", data.gameObjectName);
            Assert.AreEqual("Parent", data.parentObjectName);
            Assert.AreEqual("guid-1", data.guid);
        }

        [Test]
        public void UpdateRecord_UpdatesZoneGameObjectParent_LeavesGuidUnchanged()
        {
            var data = new ZoneToGameObjectLinkData("OldZone", "OldObject", "OldParent", "guid-1");

            data.UpdateRecord("NewZone", "NewObject", "NewParent");

            Assert.AreEqual("NewZone", data.zoneName);
            Assert.AreEqual("NewObject", data.gameObjectName);
            Assert.AreEqual("NewParent", data.parentObjectName);
            Assert.AreEqual("guid-1", data.guid); // guid is the identity, UpdateRecord never touches it
        }

        [Test]
        public void GetParentLabelStem_NullParent_ReturnsEmptyString()
        {
            var data = new ZoneToGameObjectLinkData("Zone", "Object", null, "guid-1");

            Assert.AreEqual("", data.GetParentLabelStem());
        }

        [Test]
        public void GetParentLabelStem_EmptyParent_ReturnsEmptyString()
        {
            var data = new ZoneToGameObjectLinkData("Zone", "Object", "", "guid-1");

            Assert.AreEqual("", data.GetParentLabelStem());
        }

        [Test]
        public void GetParentLabelStem_NonEmptyParent_ReturnsParentPlusDot()
        {
            var data = new ZoneToGameObjectLinkData("Zone", "Object", "Parent", "guid-1");

            Assert.AreEqual("Parent.", data.GetParentLabelStem());
        }

        [Test]
        public void Equals_SameGuid_DifferentOtherFields_ReturnsTrue()
        {
            var a = new ZoneToGameObjectLinkData("ZoneA", "ObjectA", "ParentA", "guid-1");
            var b = new ZoneToGameObjectLinkData("ZoneB", "ObjectB", "ParentB", "guid-1");

            Assert.IsTrue(a.Equals(b)); // equality is guid-only by design
        }

        [Test]
        public void Equals_DifferentGuid_ReturnsFalse()
        {
            var a = new ZoneToGameObjectLinkData("Zone", "Object", "Parent", "guid-1");
            var b = new ZoneToGameObjectLinkData("Zone", "Object", "Parent", "guid-2");

            Assert.IsFalse(a.Equals(b));
        }

        [Test]
        public void Equals_ObjectOverload_WrongType_ReturnsFalse()
        {
            var a = new ZoneToGameObjectLinkData("Zone", "Object", "Parent", "guid-1");

            Assert.IsFalse(a.Equals("not a ZoneToGameObjectLinkData"));
        }

        [Test]
        public void GetHashCode_MatchesGuidStringHashCode()
        {
            var data = new ZoneToGameObjectLinkData("Zone", "Object", "Parent", "guid-1");

            Assert.AreEqual("guid-1".GetHashCode(), data.GetHashCode());
        }

        [Test]
        public void GetHashCode_EqualInstances_ProduceSameHash()
        {
            var a = new ZoneToGameObjectLinkData("ZoneA", "ObjectA", "ParentA", "guid-1");
            var b = new ZoneToGameObjectLinkData("ZoneB", "ObjectB", "ParentB", "guid-1");

            Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
        }

        [Test]
        public void FieldRefHelpers_MatchActualFieldNames()
        {
            Assert.AreEqual("zoneName", ZoneToGameObjectLinkData.GetZoneNameRef());
            Assert.AreEqual("gameObjectName", ZoneToGameObjectLinkData.GetGameObjectNameRef());
            Assert.AreEqual("parentObjectName", ZoneToGameObjectLinkData.GetParentObjectNameRef());
            Assert.AreEqual("guid", ZoneToGameObjectLinkData.GetGuidRef());
        }
    }
}

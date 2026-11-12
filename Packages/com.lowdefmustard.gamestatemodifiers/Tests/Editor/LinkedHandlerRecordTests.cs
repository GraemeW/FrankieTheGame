using NUnit.Framework;

namespace LowDefMustard.GameStateModifiers.Tests.Editor
{
    public class LinkedHandlerRecordTests
    {
        [Test]
        public void Constructor_SetsAllFieldsFromArguments()
        {
            var data = new LinkedHandlerRecord("Zone", "Object", "Parent", "guid-1");

            Assert.AreEqual("Zone", data.zoneName);
            Assert.AreEqual("Object", data.gameObjectName);
            Assert.AreEqual("Parent", data.parentObjectName);
            Assert.AreEqual("guid-1", data.handlerGUID);
        }

        [Test]
        public void UpdateRecord_UpdatesZoneGameObjectParentType_LeavesGuidUnchanged()
        {
            var data = new LinkedHandlerRecord("OldZone", "OldObject", "OldParent", "guid-1");

            data.UpdateRecord("NewZone", "NewObject", "NewParent", "NewType");

            Assert.AreEqual("NewZone", data.zoneName);
            Assert.AreEqual("NewObject", data.gameObjectName);
            Assert.AreEqual("NewParent", data.parentObjectName);
            Assert.AreEqual("NewType", data.handlerTypeName);
            Assert.AreEqual("guid-1", data.handlerGUID); // guid is the identity, UpdateRecord never touches it
        }

        [Test]
        public void GetParentLabelStem_NullParent_ReturnsEmptyString()
        {
            var data = new LinkedHandlerRecord("Zone", "Object", null, "guid-1");

            Assert.AreEqual("", data.GetParentLabelStem());
        }

        [Test]
        public void GetParentLabelStem_EmptyParent_ReturnsEmptyString()
        {
            var data = new LinkedHandlerRecord("Zone", "Object", "", "guid-1");

            Assert.AreEqual("", data.GetParentLabelStem());
        }

        [Test]
        public void GetParentLabelStem_NonEmptyParent_ReturnsParentPlusDot()
        {
            var data = new LinkedHandlerRecord("Zone", "Object", "Parent", "guid-1");

            Assert.AreEqual("Parent.", data.GetParentLabelStem());
        }

        [Test]
        public void Equals_SameGuid_DifferentOtherFields_ReturnsTrue()
        {
            var a = new LinkedHandlerRecord("ZoneA", "ObjectA", "ParentA", "guid-1");
            var b = new LinkedHandlerRecord("ZoneB", "ObjectB", "ParentB", "guid-1");

            Assert.IsTrue(a.Equals(b)); // equality is guid-only by design
        }

        [Test]
        public void Equals_DifferentGuid_ReturnsFalse()
        {
            var a = new LinkedHandlerRecord("Zone", "Object", "Parent", "guid-1");
            var b = new LinkedHandlerRecord("Zone", "Object", "Parent", "guid-2");

            Assert.IsFalse(a.Equals(b));
        }

        [Test]
        public void Equals_ObjectOverload_WrongType_ReturnsFalse()
        {
            var a = new LinkedHandlerRecord("Zone", "Object", "Parent", "guid-1");

            Assert.IsFalse(a.Equals("not a LinkedHandlerRecord"));
        }

        [Test]
        public void GetHashCode_MatchesGuidStringHashCode()
        {
            var data = new LinkedHandlerRecord("Zone", "Object", "Parent", "guid-1");

            Assert.AreEqual("guid-1".GetHashCode(), data.GetHashCode());
        }

        [Test]
        public void GetHashCode_EqualInstances_ProduceSameHash()
        {
            var a = new LinkedHandlerRecord("ZoneA", "ObjectA", "ParentA", "guid-1");
            var b = new LinkedHandlerRecord("ZoneB", "ObjectB", "ParentB", "guid-1");

            Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
        }

        [Test]
        public void FieldRefHelpers_MatchActualFieldNames()
        {
            Assert.AreEqual("zoneName", LinkedHandlerRecord.GetZoneNameRef());
            Assert.AreEqual("gameObjectName", LinkedHandlerRecord.GetGameObjectNameRef());
            Assert.AreEqual("parentObjectName", LinkedHandlerRecord.GetParentObjectNameRef());
            Assert.AreEqual("handlerGUID", LinkedHandlerRecord.GetHandlerGUIDRef());
        }
    }
}

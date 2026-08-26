using NUnit.Framework;
using UnityEngine;

namespace LowDefMustard.Utils.Tests.Editor
{
    public class SerializableDataTests
    {
        [Test]
        public void SerializableVector2_RoundTripsFromVector3()
        {
            // Constructor takes a Vector3 and discards z - a lossy conversion by design
            var source = new Vector3(1.5f, -2.5f, 99f);

            var serializable = new SerializableVector2(source);

            Assert.AreEqual(new Vector2(1.5f, -2.5f), serializable.ToVector());
        }

        [Test]
        public void SerializableVector3_RoundTripsFromVector3()
        {
            var source = new Vector3(1.5f, -2.5f, 3.5f);

            var serializable = new SerializableVector3(source);

            Assert.AreEqual(source, serializable.ToVector());
        }

        [Test]
        public void SerializablePolygon_DefaultsToEmptyPointsList()
        {
            var polygon = new SerializablePolygon();

            Assert.IsNotNull(polygon.points);
            Assert.AreEqual(0, polygon.points.Count);
        }

        [Test]
        public void SerializablePolygon_PointsListIsMutable()
        {
            var polygon = new SerializablePolygon();

            polygon.points.Add(new Vector2(1, 1));
            polygon.points.Add(new Vector2(2, 2));

            Assert.AreEqual(2, polygon.points.Count);
        }
    }
}

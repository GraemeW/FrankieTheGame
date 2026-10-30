using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace LowDefMustard.Rendering.Tests.Editor
{
    public class BattleEffectShapeTests
    {
        // Notes:
        //  - Runs over every concrete BattleEffectShape in the project (incl. any outside this package)
        //  - Purposely not using a test-only shape subclass since the dispatch generator + shape drawer would pick it up project-wide

        // Static Methods
        private static IEnumerable<Type> GetShapeTypes() => TypeCache.GetTypesDerivedFrom<BattleEffectShape>().Where(type => !type.IsAbstract);

        #region Tests
        [Test]
        public void AllShapes_HaveUniqueNonNegativeIds()
        {
            var ids = new Dictionary<int, Type>();
            foreach (Type shapeType in GetShapeTypes())
            {
                var shape = (BattleEffectShape)Activator.CreateInstance(shapeType);
                Assert.IsTrue(shape.TryGetShapeId(out int shapeId), $"{shapeType.Name} has no [BattleEffectShapeId]");
                Assert.IsFalse(ids.ContainsKey(shapeId), $"{shapeType.Name} reuses shape ID {shapeId}");
                ids[shapeId] = shapeType;
            }
            Assert.IsNotEmpty(ids);
        }

        [Test]
        public void AllShapes_ParametersFitTheDataLayoutCapacity()
        {
            foreach (Type shapeType in GetShapeTypes())
            {
                var shape = (BattleEffectShape)Activator.CreateInstance(shapeType);
                var parameters = new BattleEffectFloatRange[BattleEffectDataLayout.shapeParameterCapacity];
                Assert.DoesNotThrow(() => shape.GetParameters(parameters), shapeType.Name);
            }
        }

        [TestCase(typeof(BattleEffectCircle), 0)]
        [TestCase(typeof(BattleEffectBox), 1)]
        [TestCase(typeof(BattleEffectTriangle), 2)]
        [TestCase(typeof(BattleEffectSegment), 3)]
        [TestCase(typeof(BattleEffectDiamond), 4)]
        [TestCase(typeof(BattleEffectStar), 5)]
        [TestCase(typeof(BattleEffectPolygon), 6)]
        [TestCase(typeof(BattleEffectLens), 7)]
        [TestCase(typeof(BattleEffectChevron), 8)]
        [TestCase(typeof(BattleEffectWedge), 9)]
        [TestCase(typeof(BattleEffectSquiggle), 10)]
        [TestCase(typeof(BattleEffectJaggedLine), 11)]
        [TestCase(typeof(BattleEffectZiggurat), 12)]
        [TestCase(typeof(BattleEffectTrapezoid), 13)]
        public void PackageShapes_KeepTheirStableIds(Type shapeType, int expectedId)
        {
            var shape = (BattleEffectShape)Activator.CreateInstance(shapeType);

            Assert.IsTrue(shape.TryGetShapeId(out int shapeId));
            Assert.AreEqual(expectedId, shapeId);
        }

        [Test]
        public void GetParameters_SingleParameterShapes_WriteSlotZero()
        {
            var parameters = new BattleEffectFloatRange[BattleEffectDataLayout.shapeParameterCapacity];

            new BattleEffectDiamond().GetParameters(parameters);
            Assert.AreEqual(1f, parameters[0].GetStart(), "diamond aspect ratio");

            new BattleEffectChevron().GetParameters(parameters);
            Assert.AreEqual(0.6f, parameters[0].GetStart(), 1e-5f, "chevron depth ratio");

            new BattleEffectLens().GetParameters(parameters);
            Assert.AreEqual(0.4f, parameters[0].GetStart(), 1e-5f, "lens aspect ratio");

            new BattleEffectTriangle().GetParameters(parameters);
            Assert.AreEqual(0.866f, parameters[0].GetStart(), 1e-5f, "triangle aspect ratio");

            new BattleEffectPolygon().GetParameters(parameters);
            Assert.AreEqual(6f, parameters[0].GetStart(), "polygon sides");
        }

        [Test]
        public void GetParameters_Squiggle_WritesSettingsInAdapterOrder()
        {
            var parameters = new BattleEffectFloatRange[BattleEffectDataLayout.shapeParameterCapacity];

            new BattleEffectSquiggle().GetParameters(parameters);

            float[] expected = { 0.9f, 0.2f, 0.1f, 0f, 0f, 0.25f, 0.5f, 1f };
            for (int i = 0; i < expected.Length; i++) { Assert.AreEqual(expected[i], parameters[i].GetStart(), 1e-5f, $"parameter {i}"); }
        }

        [Test]
        public void GetParameters_SquiggleWaveform_WritesItsIndex()
        {
            var parameters = new BattleEffectFloatRange[BattleEffectDataLayout.shapeParameterCapacity];
            var squiggle = new BattleEffectSquiggle();
            JsonUtility.FromJsonOverwrite($"{{\"waveform\":{(int)BattleEffectWaveform.Steps}}}", squiggle);

            squiggle.GetParameters(parameters);

            Assert.AreEqual(3f, parameters[4].GetStart());
            Assert.AreEqual(3f, parameters[4].GetEnd());
        }

        [Test]
        public void GetParameters_SquiggleTrapezoid_WritesItsIndexThenPlateau()
        {
            var parameters = new BattleEffectFloatRange[BattleEffectDataLayout.shapeParameterCapacity];
            var squiggle = new BattleEffectSquiggle();
            JsonUtility.FromJsonOverwrite($"{{\"waveform\":{(int)BattleEffectWaveform.Trapezoid},\"plateauFraction\":{{\"start\":0.1,\"end\":0.9}}}}", squiggle);

            squiggle.GetParameters(parameters);

            Assert.AreEqual(4f, parameters[4].GetStart(), "waveform");
            Assert.AreEqual(0.1f, parameters[5].GetStart(), 1e-5f, "plateau start");
            Assert.AreEqual(0.9f, parameters[5].GetEnd(), 1e-5f, "plateau end");
        }

        [Test]
        public void GetParameters_SquigglePulse_WritesItsIndexPlateauDutyThenBumps()
        {
            var parameters = new BattleEffectFloatRange[BattleEffectDataLayout.shapeParameterCapacity];
            var squiggle = new BattleEffectSquiggle();
            JsonUtility.FromJsonOverwrite($"{{\"waveform\":{(int)BattleEffectWaveform.Pulse},\"plateauFraction\":{{\"start\":0.5,\"end\":0.5}},\"dutyFraction\":{{\"start\":0.2,\"end\":0.4}},\"bumps\":2}}", squiggle);

            squiggle.GetParameters(parameters);

            Assert.AreEqual(5f, parameters[4].GetStart(), "waveform");
            Assert.AreEqual(0.5f, parameters[5].GetStart(), 1e-5f, "plateau");
            Assert.AreEqual(0.2f, parameters[6].GetStart(), 1e-5f, "duty start");
            Assert.AreEqual(0.4f, parameters[6].GetEnd(), 1e-5f, "duty end");
            Assert.AreEqual(2f, parameters[7].GetStart(), "bumps");
        }

        [Test]
        public void GetParameters_SquigglePulse_ClampsBumpsToOne()
        {
            var parameters = new BattleEffectFloatRange[BattleEffectDataLayout.shapeParameterCapacity];
            var squiggle = new BattleEffectSquiggle();
            JsonUtility.FromJsonOverwrite("{\"bumps\":0}", squiggle);

            squiggle.GetParameters(parameters);

            Assert.AreEqual(1f, parameters[7].GetStart(), "zero-filled = one bump");
        }

        [Test]
        public void GetParameters_JaggedLine_WritesSettingsInAdapterOrder()
        {
            var parameters = new BattleEffectFloatRange[BattleEffectDataLayout.shapeParameterCapacity];

            new BattleEffectJaggedLine().GetParameters(parameters);

            float[] expected = { 1f, 0.25f, 0.1f, 0.3f, 0f, 1f, 0f, 0f };
            for (int i = 0; i < expected.Length; i++) { Assert.AreEqual(expected[i], parameters[i].GetStart(), 1e-5f, $"parameter {i}"); }
        }

        [Test]
        public void GetParameters_Wedge_WritesSettingsInAdapterOrder()
        {
            var parameters = new BattleEffectFloatRange[BattleEffectDataLayout.shapeParameterCapacity];
            var wedge = new BattleEffectWedge();
            JsonUtility.FromJsonOverwrite("{\"halfAngleDegrees\":{\"start\":135,\"end\":90},\"apexOffsetSU\":{\"start\":2,\"end\":-1}}", wedge);

            wedge.GetParameters(parameters);

            Assert.AreEqual(135f, parameters[0].GetStart(), "half angle start");
            Assert.AreEqual(90f, parameters[0].GetEnd(), "half angle end");
            Assert.AreEqual(2f, parameters[1].GetStart(), "apex offset start");
            Assert.AreEqual(-1f, parameters[1].GetEnd(), "apex offset end");
        }

        [Test]
        public void GetParameters_Box_WritesSettingsInAdapterOrder()
        {
            var parameters = new BattleEffectFloatRange[BattleEffectDataLayout.shapeParameterCapacity];
            var box = new BattleEffectBox();
            JsonUtility.FromJsonOverwrite("{\"aspectRatio\":{\"start\":0.5,\"end\":2},\"cornerRadiusSU\":{\"start\":0,\"end\":0.25}}", box);

            box.GetParameters(parameters);

            Assert.AreEqual(0.5f, parameters[0].GetStart(), "aspect ratio start");
            Assert.AreEqual(2f, parameters[0].GetEnd(), "aspect ratio end");
            Assert.AreEqual(0f, parameters[1].GetStart(), "corner radius start");
            Assert.AreEqual(0.25f, parameters[1].GetEnd(), "corner radius end");
        }

        [Test]
        public void GetParameters_Trapezoid_WritesBottomThenTopHalfWidth()
        {
            var parameters = new BattleEffectFloatRange[BattleEffectDataLayout.shapeParameterCapacity];
            var trapezoid = new BattleEffectTrapezoid();
            JsonUtility.FromJsonOverwrite("{\"bottomHalfWidthSU\":{\"start\":3,\"end\":3},\"topHalfWidthSU\":{\"start\":1,\"end\":2}}", trapezoid);

            trapezoid.GetParameters(parameters);

            Assert.AreEqual(3f, parameters[0].GetStart(), "bottom half width");
            Assert.AreEqual(1f, parameters[1].GetStart(), "top half width start");
            Assert.AreEqual(2f, parameters[1].GetEnd(), "top half width end");
        }

        [Test]
        public void GetParameters_Star_WritesPointsAsAConstantThenInnerRatio()
        {
            var parameters = new BattleEffectFloatRange[BattleEffectDataLayout.shapeParameterCapacity];
            var star = new BattleEffectStar();
            JsonUtility.FromJsonOverwrite("{\"points\":7,\"innerRatio\":{\"start\":0.3,\"end\":0.9}}", star);

            star.GetParameters(parameters);

            Assert.AreEqual(7f, parameters[0].GetStart(), "points start");
            Assert.AreEqual(7f, parameters[0].GetEnd(), "points end");
            Assert.AreEqual(0.3f, parameters[1].GetStart(), 1e-5f, "inner ratio start");
            Assert.AreEqual(0.9f, parameters[1].GetEnd(), 1e-5f, "inner ratio end");
        }

        [Test]
        public void GetParameters_Ziggurat_WritesSettingsInAdapterOrder()
        {
            var parameters = new BattleEffectFloatRange[BattleEffectDataLayout.shapeParameterCapacity];

            new BattleEffectZiggurat().GetParameters(parameters);

            float[] expected = { 0.2f, 2f, 0.1f, 0.3f, 0.2f, 1f, 1f, 0f };
            for (int i = 0; i < expected.Length; i++) { Assert.AreEqual(expected[i], parameters[i].GetStart(), 1e-5f, $"parameter {i}"); }
        }
        #endregion
    }
}

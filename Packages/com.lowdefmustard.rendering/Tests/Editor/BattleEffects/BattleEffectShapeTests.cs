using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;

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
        [TestCase(typeof(BattleEffectSquiggle), 1)]
        [TestCase(typeof(BattleEffectDiamond), 2)]
        [TestCase(typeof(BattleEffectSegment), 3)]
        [TestCase(typeof(BattleEffectChevron), 4)]
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
            Assert.AreEqual(1f, parameters[0].GetStart(), "diamond aspect");

            new BattleEffectChevron().GetParameters(parameters);
            Assert.AreEqual(0.6f, parameters[0].GetStart(), 1e-5f, "chevron depth");
        }
        #endregion
    }
}

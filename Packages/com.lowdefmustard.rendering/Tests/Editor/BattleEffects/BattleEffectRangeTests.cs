using NUnit.Framework;
using UnityEngine;

namespace LowDefMustard.Rendering.Tests.Editor
{
    public class BattleEffectRangeTests
    {
        [Test]
        public void FloatRange_SingleValue_IsConstant()
        {
            var range = new BattleEffectFloatRange(0.5f);

            Assert.AreEqual(0.5f, range.GetStart());
            Assert.AreEqual(0.5f, range.GetEnd());
        }

        [Test]
        public void FloatRange_StartEnd_AreKept()
        {
            var range = new BattleEffectFloatRange(1f, -2f);

            Assert.AreEqual(1f, range.GetStart());
            Assert.AreEqual(-2f, range.GetEnd());
        }

        [Test]
        public void Vector2Range_SingleValue_IsConstant()
        {
            var range = new BattleEffectVector2Range(new Vector2(0.25f, 0.75f));

            Assert.AreEqual(new Vector2(0.25f, 0.75f), range.GetStart());
            Assert.AreEqual(new Vector2(0.25f, 0.75f), range.GetEnd());
        }

        [Test]
        public void Vector2Range_StartEnd_AreKept()
        {
            var range = new BattleEffectVector2Range(Vector2.zero, Vector2.one);

            Assert.AreEqual(Vector2.zero, range.GetStart());
            Assert.AreEqual(Vector2.one, range.GetEnd());
        }

        [Test]
        public void FieldReferences_MatchTheSerializedFieldNames()
        {
            // The range drawer binds by these names
            string floatJson = JsonUtility.ToJson(new BattleEffectFloatRange(1f, 2f));
            string vectorJson = JsonUtility.ToJson(new BattleEffectVector2Range(Vector2.zero, Vector2.one));

            StringAssert.Contains($"\"{BattleEffectFloatRange.startField}\"", floatJson);
            StringAssert.Contains($"\"{BattleEffectFloatRange.endField}\"", floatJson);
            StringAssert.Contains($"\"{BattleEffectVector2Range.startField}\"", vectorJson);
            StringAssert.Contains($"\"{BattleEffectVector2Range.endField}\"", vectorJson);
        }
    }
}

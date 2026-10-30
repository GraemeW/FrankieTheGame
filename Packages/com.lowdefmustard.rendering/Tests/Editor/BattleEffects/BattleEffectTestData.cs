using System.Globalization;
using NUnit.Framework;
using UnityEngine;

namespace LowDefMustard.Rendering.Tests.Editor
{
    internal static class BattleEffectTestData
    {
        // Notes:
        //  - Items + recipes have no setters for their tunables, so tests overwrite private serialized fields from (partial) JSON
        //  - JsonUtility leaves fields absent from the JSON untouched
        //  - The [SerializeReference] shape is set afterwards via SetShape

        private const float _tolerance = 1e-5f;

        public static BattleEffectItem CreateItem(BattleEffectShape shape, string json = "{}")
        {
            var item = new BattleEffectItem();
            JsonUtility.FromJsonOverwrite(json, item);
            item.SetShape(shape);
            return item;
        }

        public static string ToJson(Color colour) => string.Format(CultureInfo.InvariantCulture, "{{\"r\":{0},\"g\":{1},\"b\":{2},\"a\":{3}}}", colour.r, colour.g, colour.b, colour.a);

        public static void AssertTexel(Color expected, Color actual, string message = null)
        {
            Assert.AreEqual(expected.r, actual.r, _tolerance, $"{message} (r)");
            Assert.AreEqual(expected.g, actual.g, _tolerance, $"{message} (g)");
            Assert.AreEqual(expected.b, actual.b, _tolerance, $"{message} (b)");
            Assert.AreEqual(expected.a, actual.a, _tolerance, $"{message} (a)");
        }
    }
}

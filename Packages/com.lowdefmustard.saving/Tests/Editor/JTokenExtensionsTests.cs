using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace LowDefMustard.Saving.Tests.Editor
{
    public class JTokenExtensionsTests
    {
        private class Vector2Data
        {
            public float x;
            public float y;
        }

        #region TryToObject
        [Test]
        public void TryToObject_NullToken_ReturnsFalse()
        {
            JToken token = null;
            bool result = token.TryToObject(out int value);

            Assert.IsFalse(result);
            Assert.AreEqual(0, value);
        }

        [Test]
        public void TryToObject_NullType_ReturnsFalse()
        {
            JToken token = JValue.CreateNull();
            bool result = token.TryToObject(out int value);

            Assert.IsFalse(result);
        }

        [Test]
        public void TryToObject_UndefinedType_ReturnsFalse()
        {
            JToken token = JValue.CreateUndefined();
            bool result = token.TryToObject(out int value);

            Assert.IsFalse(result);
        }

        [Test]
        public void TryToObject_ExactTypeMatch_ReturnsTrueAndValue()
        {
            // JValue(string) stores its underlying Value as string directly -- hits the exact-match branch
            JToken token = new JValue("hello");
            bool result = token.TryToObject(out string value);

            Assert.IsTrue(result);
            Assert.AreEqual("hello", value);
        }

        [Test]
        public void TryToObject_ScalarWidening_IntToLong_ReturnsTrueAndValue()
        {
            // JValue(int) boxes as long internally - exact match misses, scalar conversion path handles it
            JToken token = new JValue(5);
            bool result = token.TryToObject(out long value);

            Assert.IsTrue(result);
            Assert.AreEqual(5L, value);
        }

        [Test]
        public void TryToObject_InvalidScalarConversion_ReturnsFalse()
        {
            JToken token = new JValue("not a number");
            bool result = token.TryToObject(out int value);

            Assert.IsFalse(result);
            Assert.AreEqual(0, value);
        }

        [Test]
        public void TryToObject_ObjectSlowPath_ReturnsTrueAndValue()
        {
            JToken token = JToken.FromObject(new Vector2Data { x = 1.5f, y = 2.5f });
            bool result = token.TryToObject(out Vector2Data value);

            Assert.IsTrue(result);
            Assert.AreEqual(1.5f, value.x);
            Assert.AreEqual(2.5f, value.y);
        }

        [Test]
        public void TryToObject_ArraySlowPath_ReturnsTrueAndValue()
        {
            JToken token = JToken.FromObject(new List<int> { 1, 2, 3 });
            bool result = token.TryToObject(out List<int> value);

            Assert.IsTrue(result);
            CollectionAssert.AreEqual(new List<int> { 1, 2, 3 }, value);
        }

        [Test]
        public void TryToObject_ArraySlowPath_MalformedElements_ReturnsFalse()
        {
            JToken token = JToken.FromObject(new List<string> { "not", "numbers" });
            bool result = token.TryToObject(out List<int> value);

            Assert.IsFalse(result);
        }
        #endregion

        #region IsNullOrEmpty
        [Test]
        public void IsNullOrEmpty_NullReference_ReturnsTrue()
        {
            JToken token = null;
            Assert.IsTrue(token.IsNullOrEmpty());
        }

        [Test]
        public void IsNullOrEmpty_NullType_ReturnsTrue()
        {
            Assert.IsTrue(JValue.CreateNull().IsNullOrEmpty());
        }

        [Test]
        public void IsNullOrEmpty_UndefinedType_ReturnsTrue()
        {
            Assert.IsTrue(JValue.CreateUndefined().IsNullOrEmpty());
        }

        [Test]
        public void IsNullOrEmpty_EmptyArray_ReturnsTrue()
        {
            Assert.IsTrue(new JArray().IsNullOrEmpty());
        }

        [Test]
        public void IsNullOrEmpty_NonEmptyArray_ReturnsFalse()
        {
            Assert.IsFalse(new JArray(1, 2).IsNullOrEmpty());
        }

        [Test]
        public void IsNullOrEmpty_EmptyObject_ReturnsTrue()
        {
            Assert.IsTrue(new JObject().IsNullOrEmpty());
        }

        [Test]
        public void IsNullOrEmpty_NonEmptyObject_ReturnsFalse()
        {
            var obj = new JObject { ["key"] = "value" };
            Assert.IsFalse(obj.IsNullOrEmpty());
        }

        [Test]
        public void IsNullOrEmpty_EmptyString_ReturnsTrue()
        {
            Assert.IsTrue(new JValue("").IsNullOrEmpty());
        }

        [Test]
        public void IsNullOrEmpty_NonEmptyString_ReturnsFalse()
        {
            Assert.IsFalse(new JValue("text").IsNullOrEmpty());
        }

        [Test]
        public void IsNullOrEmpty_NumericValue_ReturnsFalse()
        {
            // Falls through to default case - not null/undefined/array/object/string
            Assert.IsFalse(new JValue(5).IsNullOrEmpty());
        }
        #endregion
    }
}

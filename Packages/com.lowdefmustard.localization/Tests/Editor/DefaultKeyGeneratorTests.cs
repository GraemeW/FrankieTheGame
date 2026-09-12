using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LowDefMustard.Localization.Tests.Editor
{
    public class DefaultKeyGeneratorTests
    {
        // Tunables
        private const string _hexSuffixPattern = "[0-9a-f]{1,8}";

        // State
        private System.Collections.Generic.List<Object> createdObjects;

        #region DataStructures
        private class TestScriptableObject : ScriptableObject { }
        private class TestMonoBehaviour : MonoBehaviour { }
        private class MarkerDeclaringType { }
        #endregion

        #region Setup
        [SetUp]
        public void SetUp()
        {
            createdObjects = new System.Collections.Generic.List<Object>();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (Object createdObject in createdObjects.Where(createdObject => createdObject != null))
            {
                Object.DestroyImmediate(createdObject);
            }
        }
        #endregion

        #region PrivateMethods
        private GameObject CreateGameObject(string name)
        {
            var gameObject = new GameObject(name);
            createdObjects.Add(gameObject);
            return gameObject;
        }

        private TestScriptableObject CreateScriptableObject(string name)
        {
            var scriptableObject = ScriptableObject.CreateInstance<TestScriptableObject>();
            scriptableObject.name = name;
            createdObjects.Add(scriptableObject);
            return scriptableObject;
        }
        
        private static void AssertMatchesPrefixAndHexSuffix(string key, string expectedPrefix)
        {
            Assert.IsTrue(key.StartsWith(expectedPrefix), $"Key '{key}' did not start with '{expectedPrefix}'");
            string suffix = key[expectedPrefix.Length..];
            Assert.IsTrue(Regex.IsMatch(suffix, $"^{_hexSuffixPattern}$"), $"Suffix '{suffix}' did not match hex pattern");
        }
        #endregion

        #region UniqueKeyTestMethods
        [Test]
        public void GenerateKindaUniqueKey_ScriptableObject_ProducesExpectedStemAndStripsPropertyName()
        {
            TestScriptableObject scriptableObject = CreateScriptableObject("MyAsset");

            string key = DefaultKeyGenerator.GenerateKindaUniqueKey(scriptableObject, "localizedDescription");

            AssertMatchesPrefixAndHexSuffix(key, "TestScriptableObject.SO.MyAsset.Description.");
        }

        [Test]
        public void GenerateKindaUniqueKey_DeclaringTypeProvided_OverridesComponentStem()
        {
            TestScriptableObject scriptableObject = CreateScriptableObject("MyAsset");

            string key = DefaultKeyGenerator.GenerateKindaUniqueKey(scriptableObject, "localizedDescription", typeof(MarkerDeclaringType));

            AssertMatchesPrefixAndHexSuffix(key, "MarkerDeclaringType.SO.MyAsset.Description.");
        }

        [Test]
        public void GenerateKindaUniqueKey_NullPropertyName_ProducesEmptyPropertySegment()
        {
            // sanitized propertyName is "", so the property stem collapses to a bare "."
            TestScriptableObject scriptableObject = CreateScriptableObject("MyAsset");

            string key = DefaultKeyGenerator.GenerateKindaUniqueKey(scriptableObject, null);

            AssertMatchesPrefixAndHexSuffix(key, "TestScriptableObject.SO.MyAsset..");
        }

        [Test]
        public void GenerateKindaUniqueKey_MonoBehaviourWithParent_UsesParentNameAsNameStem()
        {
            GameObject parent = CreateGameObject("Root");
            GameObject child = CreateGameObject("Child");
            child.transform.SetParent(parent.transform);
            child.AddComponent<TestMonoBehaviour>();

            string key = DefaultKeyGenerator.GenerateKindaUniqueKey(child, "localizedName");

            // componentStem is based on the GameObject passed in && scene name segment is environment-dependent (left as a wildcard)
            var pattern = $"^GameObject\\.GO\\.[^.]*\\.Root\\.Name\\.{_hexSuffixPattern}$";
            Assert.IsTrue(Regex.IsMatch(key, pattern), $"Key '{key}' did not match pattern '{pattern}'");
        }

        [Test]
        public void GenerateKindaUniqueKey_MonoBehaviourWithCanvasParent_SkipsParentNameStem()
        {
            GameObject parent = CreateGameObject("Canvas");
            GameObject child = CreateGameObject("Child");
            child.transform.SetParent(parent.transform);
            child.AddComponent<TestMonoBehaviour>();

            string key = DefaultKeyGenerator.GenerateKindaUniqueKey(child, "localizedName");

            var pattern = $"^GameObject\\.GO\\.[^.]*\\.Child\\.Name\\.{_hexSuffixPattern}$";
            Assert.IsTrue(Regex.IsMatch(key, pattern), $"Key '{key}' did not match pattern '{pattern}'");
        }

        [Test]
        public void GenerateKindaUniqueKey_BareGameObject_SameFormatAsMonoBehaviour()
        {
            GameObject bareGameObject = CreateGameObject("Bare");

            string key = DefaultKeyGenerator.GenerateKindaUniqueKey(bareGameObject, "localizedName");

            var pattern = $"^GameObject\\.GO\\.[^.]*\\.Bare\\.Name\\.{_hexSuffixPattern}$";
            Assert.IsTrue(Regex.IsMatch(key, pattern), $"Key '{key}' did not match pattern '{pattern}'");
        }
        #endregion
    }
}

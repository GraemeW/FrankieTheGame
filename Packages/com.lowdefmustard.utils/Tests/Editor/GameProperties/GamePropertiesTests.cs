using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace LowDefMustard.Utils.Tests.Editor
{
    public class GamePropertiesTests
    {
        // State
        private GameProperties gameProperties;

        // Setup
        [SetUp]
        public void SetUp()
        {
            GameProperties.ClearCache();
        }

        [TearDown]
        public void TearDown()
        {
            if (gameProperties != null) { Object.DestroyImmediate(gameProperties); }
            GameProperties.ClearCache();
        }

        // Private Methods
        private static void SetArtPixelsPerUnit(GameProperties target, float artPixelsPerUnit)
        {
            var serializedObject = new SerializedObject(target);
            serializedObject.FindProperty("artPixelsPerUnit").floatValue = artPixelsPerUnit;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
        
        // Tests
        [Test]
        public void GetArtPixelsPerUnit_NullInstance_ReturnsDefault()
        {
            Assert.That(GameProperties.GetArtPixelsPerUnit(null), Is.EqualTo(100f));
        }

        [Test]
        public void GetArtPixelsPerUnit_DestroyedInstance_ReturnsDefault()
        {
            gameProperties = ScriptableObject.CreateInstance<GameProperties>();
            SetArtPixelsPerUnit(gameProperties, 32f);
            Object.DestroyImmediate(gameProperties);

            Assert.That(GameProperties.GetArtPixelsPerUnit(gameProperties), Is.EqualTo(100f));
        }

        [Test]
        public void GetArtPixelsPerUnit_NewInstance_ReturnsDefault()
        {
            gameProperties = ScriptableObject.CreateInstance<GameProperties>();

            Assert.That(GameProperties.GetArtPixelsPerUnit(gameProperties), Is.EqualTo(100f));
        }

        [Test]
        public void GetArtPixelsPerUnit_ConfiguredInstance_ReturnsInstanceValue()
        {
            gameProperties = ScriptableObject.CreateInstance<GameProperties>();
            SetArtPixelsPerUnit(gameProperties, 32f);

            Assert.That(GameProperties.GetArtPixelsPerUnit(gameProperties), Is.EqualTo(32f));
        }
        
        [Test]
        public void GetArtPixelsPerUnit_AfterFirstRead_ReturnsCachedValue()
        {
            gameProperties = ScriptableObject.CreateInstance<GameProperties>();
            SetArtPixelsPerUnit(gameProperties, 32f);
            GameProperties.GetArtPixelsPerUnit(gameProperties);

            Assert.That(GameProperties.GetArtPixelsPerUnit(null), Is.EqualTo(32f));
        }

        [Test]
        public void GetArtPixelsPerUnit_NullInstance_DoesNotPopulateCache()
        {
            GameProperties.GetArtPixelsPerUnit(null);
            gameProperties = ScriptableObject.CreateInstance<GameProperties>();
            SetArtPixelsPerUnit(gameProperties, 32f);

            Assert.That(GameProperties.GetArtPixelsPerUnit(gameProperties), Is.EqualTo(32f));
        }
    }
}

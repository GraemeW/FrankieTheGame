using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace LowDefMustard.RuleTiles.Tests.Editor
{
    public class RuleTileRandomFromSiblingsEditorTests
    {
        // ReSharper disable once InconsistentNaming - match internal unity fields
        private RuleTileRandomFromSiblings m_Tile;
        // ReSharper disable once InconsistentNaming - match internal unity fields
        private UnityEditor.Editor m_Editor;

        [SetUp]
        public void SetUp()
        {
            m_Tile = ScriptableObject.CreateInstance<RuleTileRandomFromSiblings>();
            m_Editor = UnityEditor.Editor.CreateEditor(m_Tile);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_Editor);
            Object.DestroyImmediate(m_Tile);
        }

        [Test]
        public void CreateInspectorGUI_BuildsExpectedFieldTree()
        {
            var root = m_Editor.CreateInspectorGUI();
            var children = root.Children().ToList();

            Assert.AreEqual(4, children.Count);

            var label = children[0] as Label;
            Assert.IsNotNull(label);

            var spriteField = children[1] as PropertyField;
            Assert.IsNotNull(spriteField);

            var perlinField = children[2] as PropertyField;
            Assert.IsNotNull(perlinField);

            var siblingsField = children[3] as PropertyField;
            Assert.IsNotNull(siblingsField);
        }
    }
}

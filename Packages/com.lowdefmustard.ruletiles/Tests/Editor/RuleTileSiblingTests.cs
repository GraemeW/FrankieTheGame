using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace LowDefMustard.RuleTiles.Tests.Editor
{
    public class RuleTileSiblingTests
    {
        // ReSharper disable once InconsistentNaming - match internal unity fields
        private RuleTileSibling m_Tile;
        // ReSharper disable once InconsistentNaming - match internal unity fields
        private RuleTileSibling m_SiblingTile;
        // ReSharper disable once InconsistentNaming - match internal unity fields
        private RuleTileSibling m_NonSiblingTile;
        // ReSharper disable once InconsistentNaming - match internal unity fields
        private Tile m_PlainTile;

        [SetUp]
        public void SetUp()
        {
            m_Tile = ScriptableObject.CreateInstance<RuleTileSibling>();
            m_SiblingTile = ScriptableObject.CreateInstance<RuleTileSibling>();
            m_NonSiblingTile = ScriptableObject.CreateInstance<RuleTileSibling>();
            m_PlainTile = ScriptableObject.CreateInstance<Tile>();

            m_Tile.siblings = new List<TileBase> { m_SiblingTile, m_PlainTile };
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_Tile);
            Object.DestroyImmediate(m_SiblingTile);
            Object.DestroyImmediate(m_NonSiblingTile);
            Object.DestroyImmediate(m_PlainTile);
        }

        [Test]
        public void RuleMatch_This_SelfTile_ReturnsTrue()
        {
            // Not in the siblings list at all - matches via base.RuleMatch's reference equality
            Assert.IsTrue(m_Tile.RuleMatch((int)RuleTile.TilingRuleOutput.Neighbor.This, m_Tile));
        }

        [Test]
        public void RuleMatch_This_SiblingRuleTile_ReturnsTrue()
        {
            Assert.IsTrue(m_Tile.RuleMatch((int)RuleTile.TilingRuleOutput.Neighbor.This, m_SiblingTile));
        }

        [Test]
        public void RuleMatch_This_NonSiblingRuleTile_ReturnsFalse()
        {
            Assert.IsFalse(m_Tile.RuleMatch((int)RuleTile.TilingRuleOutput.Neighbor.This, m_NonSiblingTile));
        }

        [Test]
        public void RuleMatch_NotThis_SiblingRuleTile_ReturnsFalse()
        {
            Assert.IsFalse(m_Tile.RuleMatch((int)RuleTile.TilingRuleOutput.Neighbor.NotThis, m_SiblingTile));
        }

        [Test]
        public void RuleMatch_NotThis_NonSiblingRuleTile_ReturnsTrue()
        {
            Assert.IsTrue(m_Tile.RuleMatch((int)RuleTile.TilingRuleOutput.Neighbor.NotThis, m_NonSiblingTile));
        }

        [Test]
        public void RuleMatch_This_PlainTileInSiblingsList_ReturnsFalse()
        {
            // A plain Tile never matches via the sibling mechanism
            Assert.IsFalse(m_Tile.RuleMatch((int)RuleTile.TilingRuleOutput.Neighbor.This, m_PlainTile));
        }

        [Test]
        public void RuleMatch_NotThis_PlainTileInSiblingsList_ReturnsTrue()
        {
            // Same confirmed-intended gap, NotThis side
            Assert.IsTrue(m_Tile.RuleMatch((int)RuleTile.TilingRuleOutput.Neighbor.NotThis, m_PlainTile));
        }

        [Test]
        public void RuleMatch_This_NullTile_ReturnsFalse()
        {
            Assert.IsFalse(m_Tile.RuleMatch((int)RuleTile.TilingRuleOutput.Neighbor.This, null));
        }

        [Test]
        public void RuleMatch_NotThis_NullTile_ReturnsTrue()
        {
            Assert.IsTrue(m_Tile.RuleMatch((int)RuleTile.TilingRuleOutput.Neighbor.NotThis, null));
        }

        [Test]
        public void RuleMatch_UnknownNeighborCode_FallsThroughToBaseDefaultTrue()
        {
            // Neither This nor NotThis - base.RuleMatch's default case returns true regardless
            const int unknownNeighborCode = 99;
            Assert.IsTrue(m_Tile.RuleMatch(unknownNeighborCode, m_NonSiblingTile));
        }
    }
}

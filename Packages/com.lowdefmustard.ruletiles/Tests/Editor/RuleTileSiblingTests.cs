using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
using LowDefMustard.Utils.Tests;

namespace LowDefMustard.RuleTiles.Tests.Editor
{
    public class RuleTileSiblingTests
    {
        // Tested:
        //  - RuleMatch - This / NotThis against self, sibling, non-sibling, plain Tile in siblings, null, and an unknown neighbour code
        //  - GetTileAnimationData - fixed speed exact across repeated calls
        // Not Tested:
        //  - GetTileAnimationData speed range / non-animated rule paths - straight pass-through to base RuleTile + TileAnimationSpeed (covered in TileAnimationSpeedTests)
        //  - Neighbour matching through a painted Tilemap - RuleMatch is exercised directly instead

        // State
        private readonly TestObjectTracker tracker = new();
        
        // ReSharper disable once InconsistentNaming - match internal unity fields
        private RuleTileSibling m_Tile;
        // ReSharper disable once InconsistentNaming - match internal unity fields
        private RuleTileSibling m_SiblingTile;
        // ReSharper disable once InconsistentNaming - match internal unity fields
        private RuleTileSibling m_NonSiblingTile;
        // ReSharper disable once InconsistentNaming - match internal unity fields
        private Tile m_PlainTile;

        #region Setup
        [SetUp]
        public void SetUp()
        {
            m_Tile = tracker.Create<RuleTileSibling>(nameof(m_Tile));
            m_SiblingTile = tracker.Create<RuleTileSibling>(nameof(m_SiblingTile));
            m_NonSiblingTile = tracker.Create<RuleTileSibling>(nameof(m_NonSiblingTile));
            m_PlainTile = tracker.Create<Tile>(nameof(m_PlainTile));

            m_Tile.siblings = new List<TileBase> { m_SiblingTile, m_PlainTile };
        }

        [TearDown]
        public void TearDown() => tracker.DestroyAll();
        #endregion
        
        #region Tests
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
            // Same, NotThis side
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

        [TestCase(1.5f)]
        [TestCase(0.75f)]
        public void GetTileAnimationData_FixedSpeed_IsExactAcrossCalls(float speed)
        {
            // Random.Range(1.5f, 1.5f) returns 1.50000012 ~8% of the time; 200 calls should guarantee a hit without the override
            const int callCount = 200;
            Sprite sprite = TilemapTestBuilder.CreateSprite(tracker);
            Tilemap tilemap = TilemapTestBuilder.CreateTilemap(tracker);
            m_Tile.m_TilingRules = new List<RuleTile.TilingRule>
            {
                new() { m_Output = RuleTile.TilingRuleOutput.OutputSprite.Animation, m_Sprites = new[] { sprite, sprite }, m_MinAnimationSpeed = speed, m_MaxAnimationSpeed = speed }
            };

            for (int i = 0; i < callCount; i++)
            {
                Assert.IsTrue(TilemapTestBuilder.TryGetAnimationData(m_Tile, tilemap, out TileAnimationData tileAnimationData));
                Assert.AreEqual(speed, tileAnimationData.animationSpeed);
            }
        }
        #endregion
    }
}

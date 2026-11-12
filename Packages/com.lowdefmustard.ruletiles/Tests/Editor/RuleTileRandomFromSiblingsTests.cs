using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
using LowDefMustard.Utils.Tests;

namespace LowDefMustard.RuleTiles.Tests.Editor
{
    public class RuleTileRandomFromSiblingsTests
    {
        // Tested:
        //  - RuleMatch - sibling rule tile matches, plain Tile in siblings doesn't (sibling-specific cases live in RuleTileSiblingTests)
        //  - GetTileData - null / empty / null-entry siblings leave default data, selects the sibling sprite at the Perlin index
        //  - GetTileAnimationData - null / empty / non-animated siblings return false, animated sibling populates data
        // Not Tested:
        //  - Neighbour matching through a painted Tilemap - RuleMatch is exercised directly instead

        // State
        private readonly TestObjectTracker tracker = new();
        
        // ReSharper disable once InconsistentNaming - match internal unity fields
        private RuleTileRandomFromSiblings m_Tile;

        #region Setup
        [SetUp]
        public void SetUp() => m_Tile = tracker.Create<RuleTileRandomFromSiblings>(nameof(m_Tile));

        [TearDown]
        public void TearDown() => tracker.DestroyAll();
        #endregion
        
        #region Helpers
        private Tile CreateTileWithSprite()
        {
            var tile = tracker.Create<Tile>(nameof(Tile));
            tile.sprite = TilemapTestBuilder.CreateSprite(tracker);
            return tile;
        }

        private AnimatedTile CreateAnimatedTile(float minSpeed, float maxSpeed)
        {
            var animatedTile = tracker.Create<AnimatedTile>(nameof(AnimatedTile));
            animatedTile.m_AnimatedSprites = new[] { TilemapTestBuilder.CreateSprite(tracker) };
            animatedTile.m_MinSpeed = minSpeed;
            animatedTile.m_MaxSpeed = maxSpeed;
            return animatedTile;
        }
        
        private static int ExpectedSiblingIndex(Vector3Int position, float perlinScale, int siblingCount)
        {
            var perlin = RuleTile.GetPerlinValue(position, perlinScale, 100000f); // Parallel computation of the production formula using the same public GetPerlinValue call
            return Mathf.Clamp(Mathf.FloorToInt(perlin * siblingCount), 0, siblingCount - 1);
        }
        #endregion

        #region Tests
        [Test]
        public void RuleMatch_This_SiblingRuleTile_ReturnsTrue()
        {
            var siblingTile = tracker.Create<RuleTileSibling>(nameof(RuleTileSibling));
            m_Tile.siblings = new List<TileBase> { siblingTile };

            Assert.IsTrue(m_Tile.RuleMatch((int)RuleTile.TilingRuleOutput.Neighbor.This, siblingTile));
        }

        [Test]
        public void RuleMatch_This_PlainTileInSiblingsList_ReturnsFalse()
        {
            Tile plainTile = CreateTileWithSprite();
            m_Tile.siblings = new List<TileBase> { plainTile };

            Assert.IsFalse(m_Tile.RuleMatch((int)RuleTile.TilingRuleOutput.Neighbor.This, plainTile));
        }

        [Test]
        public void GetTileData_NullSiblings_LeavesDefaultTileData()
        {
            m_Tile.siblings = null;
            var tileData = new TileData();

            m_Tile.GetTileData(Vector3Int.zero, null, ref tileData);

            Assert.AreEqual(m_Tile.m_DefaultSprite, tileData.sprite);
            Assert.AreEqual(TileFlags.LockTransform, tileData.flags);
            Assert.AreEqual(Matrix4x4.identity, tileData.transform);
        }

        [Test]
        public void GetTileData_EmptySiblings_LeavesDefaultTileData()
        {
            m_Tile.siblings = new List<TileBase>();
            var tileData = new TileData();

            m_Tile.GetTileData(Vector3Int.zero, null, ref tileData);

            Assert.AreEqual(m_Tile.m_DefaultSprite, tileData.sprite);
        }

        [Test]
        public void GetTileData_NullEntryAtSelectedIndex_LeavesDefaultTileData()
        {
            // Single-entry list always clamps to index 0
            m_Tile.siblings = new List<TileBase> { null };
            var tileData = new TileData();

            m_Tile.GetTileData(Vector3Int.zero, null, ref tileData);

            Assert.AreEqual(m_Tile.m_DefaultSprite, tileData.sprite);
        }

        [Test]
        public void GetTileData_SelectsSiblingSpriteAtPerlinIndex()
        {
            Tile siblingA = CreateTileWithSprite();
            Tile siblingB = CreateTileWithSprite();
            Tile siblingC = CreateTileWithSprite();
            var siblingTiles = new[] { siblingA, siblingB, siblingC };
            m_Tile.siblings = new List<TileBase> { siblingA, siblingB, siblingC };
            m_Tile.m_PerlinScale = 0.5f;

            var positions = new[]
            {
                new Vector3Int(0, 0, 0),
                new Vector3Int(3, -2, 0),
                new Vector3Int(17, 8, 0),
            };

            foreach (Vector3Int position in positions)
            {
                var expectedIndex = ExpectedSiblingIndex(position, m_Tile.m_PerlinScale, siblingTiles.Length);
                var tileData = new TileData();

                m_Tile.GetTileData(position, null, ref tileData);

                Assert.AreEqual(siblingTiles[expectedIndex].sprite, tileData.sprite,
                    $"position {position}, expected sibling index {expectedIndex}");
            }
        }

        [Test]
        public void GetTileAnimationData_NullSiblings_ReturnsFalse()
        {
            m_Tile.siblings = null;
            var animationData = new TileAnimationData();

            Assert.IsFalse(m_Tile.GetTileAnimationData(Vector3Int.zero, null, ref animationData));
        }

        [Test]
        public void GetTileAnimationData_EmptySiblings_ReturnsFalse()
        {
            m_Tile.siblings = new List<TileBase>();
            var animationData = new TileAnimationData();

            Assert.IsFalse(m_Tile.GetTileAnimationData(Vector3Int.zero, null, ref animationData));
        }

        [Test]
        public void GetTileAnimationData_NonAnimatedSibling_ReturnsFalse()
        {
            Tile plainTile = CreateTileWithSprite();
            m_Tile.siblings = new List<TileBase> { plainTile };
            var animationData = new TileAnimationData();

            Assert.IsFalse(m_Tile.GetTileAnimationData(Vector3Int.zero, null, ref animationData));
        }

        [Test]
        public void GetTileAnimationData_AnimatedSibling_ReturnsTrueAndPopulatesData()
        {
            AnimatedTile animatedSibling = CreateAnimatedTile(2f, 2f);
            m_Tile.siblings = new List<TileBase> { animatedSibling };
            var animationData = new TileAnimationData();

            var result = m_Tile.GetTileAnimationData(Vector3Int.zero, null, ref animationData);

            Assert.IsTrue(result);
            Assert.AreEqual(animatedSibling.m_AnimatedSprites, animationData.animatedSprites);
            Assert.AreEqual(2f, animationData.animationSpeed);
        }
        #endregion
    }
}

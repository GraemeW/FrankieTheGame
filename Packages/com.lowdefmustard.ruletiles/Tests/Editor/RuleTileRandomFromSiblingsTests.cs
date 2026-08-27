using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace LowDefMustard.RuleTiles.Tests.Editor
{
    public class RuleTileRandomFromSiblingsTests
    {
        // ReSharper disable once InconsistentNaming - match internal unity fields
        private RuleTileRandomFromSiblings m_Tile;
        // ReSharper disable once InconsistentNaming - match internal unity fields
        private readonly List<Object> m_CreatedObjects = new();

        [SetUp]
        public void SetUp()
        {
            m_Tile = ScriptableObject.CreateInstance<RuleTileRandomFromSiblings>();
            m_CreatedObjects.Add(m_Tile);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (Object obj in m_CreatedObjects)
            {
                Object.DestroyImmediate(obj);
            }
            m_CreatedObjects.Clear();
        }

        private Tile CreateTileWithSprite()
        {
            var texture = new Texture2D(1, 1);
            var sprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), Vector2.zero);
            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = sprite;
            m_CreatedObjects.Add(tile);
            m_CreatedObjects.Add(sprite);
            m_CreatedObjects.Add(texture);
            return tile;
        }

        private AnimatedTile CreateAnimatedTile(float minSpeed, float maxSpeed)
        {
            var texture = new Texture2D(1, 1);
            var sprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), Vector2.zero);
            var animatedTile = ScriptableObject.CreateInstance<AnimatedTile>();
            animatedTile.m_AnimatedSprites = new[] { sprite };
            animatedTile.m_MinSpeed = minSpeed;
            animatedTile.m_MaxSpeed = maxSpeed;
            m_CreatedObjects.Add(animatedTile);
            m_CreatedObjects.Add(sprite);
            m_CreatedObjects.Add(texture);
            return animatedTile;
        }

        // Parallel computation of the production formula using the same public GetPerlinValue call
        private static int ExpectedSiblingIndex(Vector3Int position, float perlinScale, int siblingCount)
        {
            var perlin = RuleTile.GetPerlinValue(position, perlinScale, 100000f);
            return Mathf.Clamp(Mathf.FloorToInt(perlin * siblingCount), 0, siblingCount - 1);
        }

        [Test]
        public void RuleMatch_This_SiblingRuleTile_ReturnsTrue()
        {
            var siblingTile = ScriptableObject.CreateInstance<RuleTileSibling>();
            m_CreatedObjects.Add(siblingTile);
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
    }
}

using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using LowDefMustard.RuleTiles.Editor;
using LowDefMustard.Utils.Tests;

namespace LowDefMustard.RuleTiles.Tests.Editor
{
    public class RuleTileToSiblingConverterTests
    {
        // Tested:
        //  - Validate - plain RuleTile selected, RuleTileSibling selected, mixed selection, empty selection
        //  - ConvertOne - sibling asset created alongside the source w/ sprite / collider type / tiling rules transplanted, source left untouched, unique path on a name collision
        //  - ConvertOneInPlace - GUID / asset path preserved, same path reloads as RuleTileSibling w/ the same fields
        // Not Tested:
        //  - Convert / ConvertInPlace (MenuItem entry points) - thin dialogue + loop over ConvertOne / ConvertOneInPlace (not worth it)

        // State
        private TempAssetFolder tempFolder;
        private readonly TestObjectTracker tracker = new();

        #region Setup
        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            tempFolder = new TempAssetFolder(nameof(RuleTileToSiblingConverterTests));
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            tempFolder.Dispose();
        }

        [TearDown]
        public void TearDown() => tracker.DestroyAll();
        #endregion

        #region Helpers
        private RuleTile CreateSourceRuleTile(string assetName)
        {
            Sprite sprite = TilemapTestBuilder.CreateSpriteAsset(tempFolder, assetName);

            var tile = ScriptableObject.CreateInstance<RuleTile>();
            tile.m_DefaultSprite = sprite;
            tile.m_DefaultColliderType = Tile.ColliderType.Grid;
            tile.m_TilingRules = new List<RuleTile.TilingRule>
            {
                new()
                {
                    m_Sprites = new[] { sprite },
                    m_PerlinScale = 0.25f,
                    m_MinAnimationSpeed = 2f,
                    m_MaxAnimationSpeed = 3f,
                },
            };

            AssetDatabase.CreateAsset(tile, tempFolder.Path($"{assetName}.asset"));
            return tile;
        }
        #endregion

        #region Tests
        [Test]
        public void Validate_PlainRuleTileSelected_ReturnsTrue()
        {
            RuleTile source = CreateSourceRuleTile(nameof(Validate_PlainRuleTileSelected_ReturnsTrue));
            Selection.objects = new Object[] { source };

            Assert.IsTrue(RuleTileToSiblingConverter.Validate());
        }

        [Test]
        public void Validate_NoSelection_ReturnsFalse()
        {
            Selection.objects = new Object[0];

            Assert.IsFalse(RuleTileToSiblingConverter.Validate());
        }

        [Test]
        public void Validate_RuleTileSiblingSelected_ReturnsFalse()
        {
            var siblingTile = tracker.Create<RuleTileSibling>(nameof(RuleTileSibling));
            Selection.objects = new Object[] { siblingTile };

            Assert.IsFalse(RuleTileToSiblingConverter.Validate());
        }

        [Test]
        public void Validate_MixedSelection_ReturnsFalse()
        {
            var source = CreateSourceRuleTile(nameof(Validate_MixedSelection_ReturnsFalse));
            var siblingTile = tracker.Create<RuleTileSibling>(nameof(RuleTileSibling));
            Selection.objects = new Object[] { source, siblingTile };

            Assert.IsFalse(RuleTileToSiblingConverter.Validate());
        }

        [Test]
        public void ConvertOne_CreatesSiblingAssetWithTransplantedFields()
        {
            const string assetName = nameof(ConvertOne_CreatesSiblingAssetWithTransplantedFields);
            RuleTile source = CreateSourceRuleTile(assetName);

            RuleTileToSiblingConverter.ConvertOne(source);

            // The in-memory `source` reference would be stale after reimport - important to reload directly here
            var clone = AssetDatabase.LoadAssetAtPath<RuleTileSibling>(tempFolder.Path($"{assetName}Sibling.asset"));

            Assert.IsNotNull(clone, "expected a sibling asset to be created alongside the source");
            Assert.AreEqual(source.m_DefaultSprite, clone.m_DefaultSprite);
            Assert.AreEqual(source.m_DefaultColliderType, clone.m_DefaultColliderType);
            Assert.AreEqual(source.m_TilingRules.Count, clone.m_TilingRules.Count);
            Assert.AreEqual(source.m_TilingRules[0].m_PerlinScale, clone.m_TilingRules[0].m_PerlinScale);
            Assert.AreEqual(source.m_TilingRules[0].m_Sprites[0], clone.m_TilingRules[0].m_Sprites[0]);
            Assert.IsTrue(clone.siblings == null || clone.siblings.Count == 0);
        }

        [Test]
        public void ConvertOne_SourceAssetUnaffected()
        {
            const string assetName = nameof(ConvertOne_SourceAssetUnaffected);
            RuleTile source = CreateSourceRuleTile(assetName);
            Tile.ColliderType originalColliderType = source.m_DefaultColliderType;

            RuleTileToSiblingConverter.ConvertOne(source);

            Assert.AreEqual(originalColliderType, source.m_DefaultColliderType);
            Assert.IsFalse(source is RuleTileSibling);
        }

        [Test]
        public void ConvertOne_NameCollision_GeneratesUniquePath()
        {
            const string assetName = nameof(ConvertOne_NameCollision_GeneratesUniquePath);
            RuleTile source = CreateSourceRuleTile(assetName);
            string collidingPath = tempFolder.Path($"{assetName}Sibling.asset");
            var placeholder = ScriptableObject.CreateInstance<RuleTileSibling>();
            AssetDatabase.CreateAsset(placeholder, collidingPath);

            RuleTileToSiblingConverter.ConvertOne(source);

            var placeholderStillThere = AssetDatabase.LoadAssetAtPath<RuleTileSibling>(collidingPath);
            Assert.AreEqual(placeholder, placeholderStillThere);
        }
        [Test]
        public void ConvertOneInPlace_PreservesGuidAndAssetPath()
        {
            const string assetName = nameof(ConvertOneInPlace_PreservesGuidAndAssetPath);
            RuleTile source = CreateSourceRuleTile(assetName);
            string path = AssetDatabase.GetAssetPath(source);
            string guidBefore = AssetDatabase.AssetPathToGUID(path);

            RuleTileToSiblingConverter.ConvertOneInPlace(source);

            Assert.AreEqual(guidBefore, AssetDatabase.AssetPathToGUID(path), "GUID should be unchanged by an in-place conversion");
            var reloadedAtSamePath = AssetDatabase.LoadAssetAtPath<RuleTile>(path);
            Assert.IsInstanceOf<RuleTileSibling>(reloadedAtSamePath, "expected the same path to now deserialize as RuleTileSibling, not plain RuleTile");
        }

        [Test]
        public void ConvertOneInPlace_ReloadsAsRuleTileSiblingWithTransplantedFields()
        {
            const string assetName = nameof(ConvertOneInPlace_ReloadsAsRuleTileSiblingWithTransplantedFields);
            RuleTile source = CreateSourceRuleTile(assetName);
            string path = AssetDatabase.GetAssetPath(source);
            Sprite defaultSprite = source.m_DefaultSprite;
            Tile.ColliderType colliderType = source.m_DefaultColliderType;
            int tilingRuleCount = source.m_TilingRules.Count;
            float perlinScale = source.m_TilingRules[0].m_PerlinScale;
            Sprite ruleSprite = source.m_TilingRules[0].m_Sprites[0];

            RuleTileToSiblingConverter.ConvertOneInPlace(source);

            // The in-memory `source` reference is stale after the reimport - reload from the same path directly
            var reloaded = AssetDatabase.LoadAssetAtPath<RuleTileSibling>(path);

            Assert.IsNotNull(reloaded, "expected the same-path asset to now load as RuleTileSibling");
            Assert.AreEqual(defaultSprite, reloaded.m_DefaultSprite);
            Assert.AreEqual(colliderType, reloaded.m_DefaultColliderType);
            Assert.AreEqual(tilingRuleCount, reloaded.m_TilingRules.Count);
            Assert.AreEqual(perlinScale, reloaded.m_TilingRules[0].m_PerlinScale);
            Assert.AreEqual(ruleSprite, reloaded.m_TilingRules[0].m_Sprites[0]);
            Assert.IsTrue(reloaded.siblings == null || reloaded.siblings.Count == 0);
        }
        #endregion
    }
}

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using LowDefMustard.RuleTiles.Editor;

namespace LowDefMustard.RuleTiles.Tests.Editor
{
    // Tested:
    //  - Validate - Plain RuleTile selected, RuleTileSibling selected, mixed selection, empty selection
    //  - ConvertOne - New sibling asset created alongside source with default
    // ConvertOneInPlace - GUID/asset path preserved across the swap, reloading the same path now yields a RuleTileSibling with the same parameters
    //  - Sprite/collider type and tiling rules transplanted, source left untouched, unique path generated on a name collision
    //  Not Tested:
    //  - Convert/ConvertInPlace ( MenuItem entry point) - not directly tested, thin wrapper looping
    //  - Selection.objects into ConvertOne, already covered by Validate + ConvertOne
    // Note:  this uses real assets under a disposable temp folder - deleted in TearDown
    public class RuleTileToSiblingConverterTests
    {
        // Static/Const Tunables
        private const string _tempFolderName = "_TEMP_RuleTileToSiblingConverterTests_SafeToDelete";
        private const string _tempFolder = "Assets/" + _tempFolderName;

        // State
        private readonly List<Object> createdObjects = new();

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            if (AssetDatabase.IsValidFolder(_tempFolder)) { AssetDatabase.DeleteAsset(_tempFolder); }
        }

        [TearDown]
        public void TearDown()
        {
            foreach (Object tempObject in createdObjects.Where(obj => obj != null && string.IsNullOrEmpty(AssetDatabase.GetAssetPath(obj))))
            {
                Object.DestroyImmediate(tempObject);
            }
            createdObjects.Clear();
        }

        private RuleTile CreateSourceRuleTile(string assetName)
        {
            if (!AssetDatabase.IsValidFolder(_tempFolder)) { AssetDatabase.CreateFolder("Assets", _tempFolderName); }

            var texture = new Texture2D(1, 1);
            var sprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), Vector2.zero);
            AssetDatabase.CreateAsset(sprite, $"{_tempFolder}/{assetName}Sprite.asset");

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

            AssetDatabase.CreateAsset(tile, $"{_tempFolder}/{assetName}.asset");
            return tile;
        }

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
            var siblingTile = ScriptableObject.CreateInstance<RuleTileSibling>();
            createdObjects.Add(siblingTile);
            Selection.objects = new Object[] { siblingTile };

            Assert.IsFalse(RuleTileToSiblingConverter.Validate());
        }

        [Test]
        public void Validate_MixedSelection_ReturnsFalse()
        {
            var source = CreateSourceRuleTile(nameof(Validate_MixedSelection_ReturnsFalse));
            var siblingTile = ScriptableObject.CreateInstance<RuleTileSibling>();
            createdObjects.Add(siblingTile);
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
            var clone = AssetDatabase.LoadAssetAtPath<RuleTileSibling>($"{_tempFolder}/{assetName}Sibling.asset");

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
            string collidingPath = $"{_tempFolder}/{assetName}Sibling.asset";
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
    }
}

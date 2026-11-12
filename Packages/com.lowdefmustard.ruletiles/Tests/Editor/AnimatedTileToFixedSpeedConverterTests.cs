using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using LowDefMustard.RuleTiles.Editor;
using LowDefMustard.Utils.Tests;

namespace LowDefMustard.RuleTiles.Tests.Editor
{
    public class AnimatedTileToFixedSpeedConverterTests
    {
        // Tested:
        //  - Validate - plain AnimatedTile selected, fixed speed tile selected, mixed selection, empty selection
        //  - ConvertOneInPlace - GUID preserved, same path reloads as AnimatedTileFixedSpeed w/ every serialized field carried over
        // Not Tested:
        //  - ConvertInPlace (MenuItem entry point) - thin dialogue + loop over ConvertOneInPlace (not worth testing)

        // State
        private TempAssetFolder tempFolder;
        private readonly TestObjectTracker tracker = new();

        #region Setup
        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            tempFolder = new TempAssetFolder(nameof(AnimatedTileToFixedSpeedConverterTests));
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
        private AnimatedTile CreateSourceAnimatedTile(string assetName)
        {
            Sprite sprite = TilemapTestBuilder.CreateSpriteAsset(tempFolder, assetName);

            var tile = ScriptableObject.CreateInstance<AnimatedTile>();
            tile.m_AnimatedSprites = new[] { sprite, sprite };
            tile.m_MinSpeed = 1.5f;
            tile.m_MaxSpeed = 1.5f;
            tile.m_AnimationStartTime = 0.25f;
            tile.m_AnimationStartFrame = 2;
            tile.m_TileColliderType = Tile.ColliderType.Grid;

            AssetDatabase.CreateAsset(tile, tempFolder.Path($"{assetName}.asset"));
            return tile;
        }
        #endregion

        #region Tests
        [Test]
        public void Validate_PlainAnimatedTileSelected_ReturnsTrue()
        {
            Selection.objects = new Object[] { CreateSourceAnimatedTile(nameof(Validate_PlainAnimatedTileSelected_ReturnsTrue)) };

            Assert.IsTrue(AnimatedTileToFixedSpeedConverter.Validate());
        }

        [Test]
        public void Validate_NoSelection_ReturnsFalse()
        {
            Selection.objects = new Object[0];

            Assert.IsFalse(AnimatedTileToFixedSpeedConverter.Validate());
        }

        [Test]
        public void Validate_FixedSpeedTileSelected_ReturnsFalse()
        {
            var fixedSpeedTile = tracker.Create<AnimatedTileFixedSpeed>(nameof(AnimatedTileFixedSpeed));
            Selection.objects = new Object[] { fixedSpeedTile };

            Assert.IsFalse(AnimatedTileToFixedSpeedConverter.Validate());
        }

        [Test]
        public void Validate_MixedSelection_ReturnsFalse()
        {
            AnimatedTile source = CreateSourceAnimatedTile(nameof(Validate_MixedSelection_ReturnsFalse));
            var fixedSpeedTile = tracker.Create<AnimatedTileFixedSpeed>(nameof(AnimatedTileFixedSpeed));
            Selection.objects = new Object[] { source, fixedSpeedTile };

            Assert.IsFalse(AnimatedTileToFixedSpeedConverter.Validate());
        }

        [Test]
        public void ConvertOneInPlace_PreservesGuidAndReloadsAsFixedSpeedWithFields()
        {
            const string assetName = nameof(ConvertOneInPlace_PreservesGuidAndReloadsAsFixedSpeedWithFields);
            AnimatedTile source = CreateSourceAnimatedTile(assetName);
            string path = AssetDatabase.GetAssetPath(source);
            string guidBefore = AssetDatabase.AssetPathToGUID(path);
            Sprite[] sprites = source.m_AnimatedSprites;

            AnimatedTileToFixedSpeedConverter.ConvertOneInPlace(source);

            // The in-memory `source` reference is stale after the reimport - reload from the same path directly
            Assert.AreEqual(guidBefore, AssetDatabase.AssetPathToGUID(path), "GUID should be unchanged by an in-place conversion");
            var reloaded = AssetDatabase.LoadAssetAtPath<AnimatedTileFixedSpeed>(path);
            Assert.IsNotNull(reloaded, "expected the same-path asset to now load as AnimatedTileFixedSpeed");
            CollectionAssert.AreEqual(sprites, reloaded.m_AnimatedSprites);
            Assert.AreEqual(1.5f, reloaded.m_MinSpeed);
            Assert.AreEqual(1.5f, reloaded.m_MaxSpeed);
            Assert.AreEqual(0.25f, reloaded.m_AnimationStartTime);
            Assert.AreEqual(2, reloaded.m_AnimationStartFrame);
            Assert.AreEqual(Tile.ColliderType.Grid, reloaded.m_TileColliderType);
        }
        #endregion
    }
}

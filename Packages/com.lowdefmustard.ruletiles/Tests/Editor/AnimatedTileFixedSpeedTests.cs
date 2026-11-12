using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
using LowDefMustard.RuleTiles.Editor;
using LowDefMustard.Utils.Tests;

namespace LowDefMustard.RuleTiles.Tests.Editor
{
    public class AnimatedTileFixedSpeedTests
    {
        // Tested:
        //  - GetTileAnimationData - fixed speed exact across repeated calls, speed range stays in range w/ base data kept, no sprites returns false
        //  - AnimatedTileFixedSpeedEditor - CreateEditor resolves to it for the subclass
        // Not Tested:
        //  - Inspector drawing itself - inherited unchanged from Unity's AnimatedTileEditor

        // State
        private readonly TestObjectTracker tracker = new();
        private AnimatedTileFixedSpeed tile;
        private Sprite sprite;
        private Tilemap tilemap;

        #region Setup
        [SetUp]
        public void SetUp()
        {
            tile = tracker.Create<AnimatedTileFixedSpeed>(nameof(AnimatedTileFixedSpeedTests));
            sprite = TilemapTestBuilder.CreateSprite(tracker);
            tilemap = TilemapTestBuilder.CreateTilemap(tracker);
        }

        [TearDown]
        public void TearDown() => tracker.DestroyAll();
        #endregion

        #region Tests
        [TestCase(1.5f)]
        [TestCase(0.75f)]
        public void GetTileAnimationData_FixedSpeed_IsExactAcrossCalls(float speed)
        {
            tile.m_AnimatedSprites = new[] { sprite, sprite };
            tile.m_MinSpeed = speed;
            tile.m_MaxSpeed = speed;

            for (int i = 0; i < 200; i++)
            {
                Assert.IsTrue(TilemapTestBuilder.TryGetAnimationData(tile, tilemap, out TileAnimationData tileAnimationData));
                Assert.AreEqual(speed, tileAnimationData.animationSpeed);
            }
        }

        [Test]
        public void GetTileAnimationData_SpeedRange_StaysWithinRangeAndKeepsBaseData()
        {
            tile.m_AnimatedSprites = new[] { sprite, sprite };
            tile.m_MinSpeed = 1f;
            tile.m_MaxSpeed = 3f;
            tile.m_AnimationStartTime = 0.5f;

            Assert.IsTrue(TilemapTestBuilder.TryGetAnimationData(tile, tilemap, out TileAnimationData tileAnimationData));
            Assert.GreaterOrEqual(tileAnimationData.animationSpeed, 1f);
            Assert.LessOrEqual(tileAnimationData.animationSpeed, 3f);
            Assert.AreSame(tile.m_AnimatedSprites, tileAnimationData.animatedSprites);
            Assert.AreEqual(0.5f, tileAnimationData.animationStartTime);
        }

        [Test]
        public void GetTileAnimationData_NoSprites_ReturnsFalse()
        {
            tile.m_AnimatedSprites = new Sprite[0];

            Assert.IsFalse(TilemapTestBuilder.TryGetAnimationData(tile, tilemap, out _));
        }

        [Test]
        public void CreateEditor_UsesAnimatedTileInspector()
        {
            UnityEditor.Editor editor = tracker.Track(UnityEditor.Editor.CreateEditor(tile));
            Assert.IsInstanceOf<AnimatedTileFixedSpeedEditor>(editor);
        }
        #endregion
    }
}

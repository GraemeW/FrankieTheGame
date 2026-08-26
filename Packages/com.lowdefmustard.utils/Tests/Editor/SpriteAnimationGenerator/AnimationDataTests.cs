using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using LowDefMustard.Utils.Editor;

namespace LowDefMustard.Utils.Tests.Editor
{
    public class AnimationDataTests
    {
        private readonly List<Object> createdObjects = new();

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in createdObjects)
            {
                if (obj != null) Object.DestroyImmediate(obj);
            }
            createdObjects.Clear();
        }

        private Sprite CreateDummySprite()
        {
            var texture = new Texture2D(1, 1);
            var sprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), Vector2.zero);
            createdObjects.Add(texture);
            createdObjects.Add(sprite);
            return sprite;
        }

        // --- IdleAnimationData ---

        [Test]
        public void IdleAnimationData_GetClipName_FormatsAsPrefixCharacterIdleTokenAction()
        {
            var data = new IdleAnimationData("Pre_", "Hero", "Down", System.Array.Empty<Sprite>(), new Dictionary<(string, string), Sprite[]>(), "Assets/Out");

            Assert.AreEqual($"Pre_Hero{SpriteAnimationGenerator.idleOverrideToken}Down", data.GetClipName());
        }

        [Test]
        public void IdleAnimationData_GetClipAssetPath_AppendsAnimExtension()
        {
            var data = new IdleAnimationData("", "Hero", "Down", System.Array.Empty<Sprite>(), new Dictionary<(string, string), Sprite[]>(), "Assets/Out");

            Assert.AreEqual($"Assets/Out/{data.GetClipName()}.anim", data.GetClipAssetPath());
        }

        [Test]
        public void IdleAnimationData_GetIdleSprites_DedicatedSourceExists_ReturnsDedicatedSprites()
        {
            var dedicated = new[] { CreateDummySprite(), CreateDummySprite() };
            var movement = new[] { CreateDummySprite() };
            var idleSources = new Dictionary<(string, string), Sprite[]> { { ("Hero", "Down"), dedicated } };
            var data = new IdleAnimationData("", "Hero", "Down", movement, idleSources, "Assets/Out");

            CollectionAssert.AreEqual(dedicated, data.GetIdleSprites());
        }

        [Test]
        public void IdleAnimationData_NoDedicatedSource_DownAction_FallsBackToMovementSprites()
        {
            var movement = new[] { CreateDummySprite(), CreateDummySprite() };
            var data = new IdleAnimationData("", "Hero", SpriteAnimationGenerator.standardDownToken, movement, new Dictionary<(string, string), Sprite[]>(), "Assets/Out");

            CollectionAssert.AreEqual(movement, data.GetIdleSprites());
        }

        [Test]
        public void IdleAnimationData_NoDedicatedSource_NonDownAction_FallsBackToSingleFirstMovementFrame()
        {
            var firstFrame = CreateDummySprite();
            var movement = new[] { firstFrame, CreateDummySprite(), CreateDummySprite() };
            var data = new IdleAnimationData("", "Hero", "Left", movement, new Dictionary<(string, string), Sprite[]>(), "Assets/Out");

            var result = data.GetIdleSprites();
            Assert.AreEqual(1, result.Length);
            Assert.AreSame(firstFrame, result[0]);
        }

        [Test]
        public void IdleAnimationData_DedicatedSourceExistsButEmpty_FallsBackRatherThanReturningEmpty()
        {
            // TryGetValue succeeds but the array is empty - should fall through to the movement-sprites path
            var movement = new[] { CreateDummySprite() };
            var idleSources = new Dictionary<(string, string), Sprite[]> { { ("Hero", SpriteAnimationGenerator.standardDownToken), System.Array.Empty<Sprite>() } };
            var data = new IdleAnimationData("", "Hero", SpriteAnimationGenerator.standardDownToken, movement, idleSources, "Assets/Out");

            CollectionAssert.AreEqual(movement, data.GetIdleSprites());
        }

        // --- StandStillAnimationData ---

        [Test]
        public void StandStillAnimationData_GetClipName_FormatsAsPrefixCharacterStandStillToken()
        {
            var data = new StandStillAnimationData("Pre_", "Hero", new Dictionary<string, Sprite>(), null, "Assets/Out");

            Assert.AreEqual($"Pre_Hero{SpriteAnimationGenerator.standStillOverrideToken}", data.GetClipName());
        }

        [Test]
        public void StandStillAnimationData_DedicatedSourceExists_ReturnsDedicatedSprite()
        {
            var dedicated = CreateDummySprite();
            var downFrame = CreateDummySprite();
            var sources = new Dictionary<string, Sprite> { { "Hero", dedicated } };
            var data = new StandStillAnimationData("", "Hero", sources, downFrame, "Assets/Out");

            var result = data.GetStandStillSprites();
            Assert.AreEqual(1, result.Length);
            Assert.AreSame(dedicated, result[0]);
        }

        [Test]
        public void StandStillAnimationData_NoDedicatedSource_FallsBackToDownFirstFrame()
        {
            var downFrame = CreateDummySprite();
            var data = new StandStillAnimationData("", "Hero", new Dictionary<string, Sprite>(), downFrame, "Assets/Out");

            var result = data.GetStandStillSprites();
            Assert.AreEqual(1, result.Length);
            Assert.AreSame(downFrame, result[0]);
        }

        [Test]
        public void StandStillAnimationData_NoDedicatedSourceAndNoDownFrame_ReturnsEmptyArray()
        {
            var data = new StandStillAnimationData("", "Hero", new Dictionary<string, Sprite>(), null, "Assets/Out");

            Assert.AreEqual(0, data.GetStandStillSprites().Length);
        }

        // --- StandardAnimationData ---

        [Test]
        public void StandardAnimationData_Constructor_StoresGivenValues()
        {
            var sprites = new[] { CreateDummySprite() };

            var data = new StandardAnimationData("Assets/Out/Clip.anim", "Clip", sprites);

            Assert.AreEqual("Assets/Out/Clip.anim", data.clipAssetPath);
            Assert.AreEqual("Clip", data.clipName);
            CollectionAssert.AreEqual(sprites, data.sprites);
        }
    }
}

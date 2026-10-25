using NUnit.Framework;
using UnityEngine;
using static LowDefMustard.Rendering.Tests.Editor.BattleEffectTestData;

namespace LowDefMustard.Rendering.Tests.Editor
{
    public class BattleEffectInstanceTests
    {
        // State
        private BattleEffectRecipe parent;
        private BattleEffectRecipe child;

        #region Setup
        [SetUp]
        public void SetUp()
        {
            parent = ScriptableObject.CreateInstance<BattleEffectRecipe>();
            child = ScriptableObject.CreateInstance<BattleEffectRecipe>();
            child.SetItems(new[] { CreateItem(new BattleEffectSegment(),
                $"{{\"startTimeSeconds\":0.5,\"durationSeconds\":1,\"positionEU\":{{\"start\":{{\"x\":0.2,\"y\":0.3}},\"end\":{{\"x\":0.4,\"y\":0.6}}}},\"itemRotationDegrees\":{{\"start\":30,\"end\":30}},\"style\":{{\"colour\":{ToJson(Color.red)}}},\"stripes\":{{\"stripesPerEU\":10,\"angleDegrees\":90}}}}") });
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(parent);
            Object.DestroyImmediate(child);
        }
        #endregion

        #region Tests
        [Test]
        public void Bake_Instance_AddsItsItemsAfterTheParentsOwn()
        {
            parent.SetItems(new[] { CreateItem(new BattleEffectCircle()) });
            parent.SetInstances(new[] { new BattleEffectInstance(child, 0f, false, false, new BattleEffectColourSwap[0]) });

            Texture2D dataTexture = parent.BakeDataTexture(out int itemCount);

            Assert.AreEqual(2, itemCount);
            Assert.AreEqual(0f, dataTexture.GetPixel(BattleEffectDataLayout.shape, 0).r, "row 0:  the parent's circle");
            Assert.AreEqual(3f, dataTexture.GetPixel(BattleEffectDataLayout.shape, 1).r, "row 1:  the instanced segment");
        }

        [Test]
        public void Bake_StartOffset_DelaysTheInstancedItems()
        {
            parent.SetInstances(new[] { new BattleEffectInstance(child, 2f, false, false, new BattleEffectColourSwap[0]) });

            Texture2D dataTexture = parent.BakeDataTexture(out _);

            Assert.AreEqual(2.5f, dataTexture.GetPixel(BattleEffectDataLayout.timing, 0).r, 1e-5f);
            Assert.AreEqual(3.5f, parent.GetTotalEndTime(), 1e-5f, "end time includes the offset instance");
        }

        [Test]
        public void Bake_MirrorX_FlipsPositionRotationLocalYAndStripes()
        {
            parent.SetInstances(new[] { new BattleEffectInstance(child, 0f, true, false, new BattleEffectColourSwap[0]) });

            Texture2D dataTexture = parent.BakeDataTexture(out _);

            AssertTexel(new Color(0.8f, 0.3f, 0.6f, 0.6f), dataTexture.GetPixel(BattleEffectDataLayout.position, 0), "x about the centre");
            Assert.AreEqual(Mathf.PI - 30f * Mathf.Deg2Rad, dataTexture.GetPixel(BattleEffectDataLayout.rotation, 0).r, 1e-5f, "rotation");
            Color scale = dataTexture.GetPixel(BattleEffectDataLayout.scale, 0);
            Assert.Greater(scale.r, 0f, "local x kept");
            Assert.Less(scale.g, 0f, "local y flipped");
            Assert.AreEqual(-90f * Mathf.Deg2Rad, dataTexture.GetPixel(BattleEffectDataLayout.stripes, 0).g, 1e-5f, "stripe angle");
        }

        [Test]
        public void Bake_MirrorX_FlipsBeatOffsets()
        {
            child.SetItems(new[] { CreateItem(new BattleEffectCircle(), "{\"beats\":{\"count\":1,\"beatA\":{\"offsetEU\":{\"x\":0.1,\"y\":0.2}}}}") });
            parent.SetInstances(new[] { new BattleEffectInstance(child, 0f, true, false, new BattleEffectColourSwap[0]) });

            Texture2D dataTexture = parent.BakeDataTexture(out _);

            Color offsets = dataTexture.GetPixel(BattleEffectDataLayout.beatOffsets01, 0);
            Assert.AreEqual(-0.1f, offsets.r, 1e-5f);
            Assert.AreEqual(0.2f, offsets.g, 1e-5f);
        }

        [Test]
        public void Bake_MirrorXAndY_IsAHalfTurnWithoutAFlip()
        {
            parent.SetInstances(new[] { new BattleEffectInstance(child, 0f, true, true, new BattleEffectColourSwap[0]) });

            Texture2D dataTexture = parent.BakeDataTexture(out _);

            AssertTexel(new Color(0.8f, 0.7f, 0.6f, 0.4f), dataTexture.GetPixel(BattleEffectDataLayout.position, 0));
            Assert.AreEqual(Mathf.PI + 30f * Mathf.Deg2Rad, dataTexture.GetPixel(BattleEffectDataLayout.rotation, 0).r, 1e-5f);
            Assert.Greater(dataTexture.GetPixel(BattleEffectDataLayout.scale, 0).g, 0f);
        }

        [Test]
        public void Bake_ColourSwap_ReplacesMatchingColoursOnly()
        {
            parent.SetInstances(new[] { new BattleEffectInstance(child, 0f, false, false, new[] { new BattleEffectColourSwap(Color.blue, Color.white), new BattleEffectColourSwap(Color.red, Color.green) }) });

            Texture2D dataTexture = parent.BakeDataTexture(out _);

            AssertTexel(Color.green.linear, dataTexture.GetPixel(BattleEffectDataLayout.colour, 0));
        }

        [Test]
        public void Bake_ColourSwap_AlsoSwapsTheOutlineAndAltColours()
        {
            child.SetItems(new[] { CreateItem(new BattleEffectCircle(), $"{{\"style\":{{\"mode\":2,\"outlineColour\":{ToJson(Color.blue)}}},\"colourCycle\":{{\"rate\":1}},\"altColours\":{{\"count\":1,\"colourB\":{ToJson(Color.blue)}}}}}") });
            parent.SetInstances(new[] { new BattleEffectInstance(child, 0f, false, false, new[] { new BattleEffectColourSwap(Color.blue, Color.yellow) }) });

            Texture2D dataTexture = parent.BakeDataTexture(out _);

            AssertTexel(Color.yellow.linear, dataTexture.GetPixel(BattleEffectDataLayout.outlineColour, 0), "outline colour");
            AssertTexel(Color.yellow.linear, dataTexture.GetPixel(BattleEffectDataLayout.altColourB, 0), "alt colour B");
        }

        [Test]
        public void Bake_SelfInstance_StopsAtTheDepthLimit()
        {
            child.SetInstances(new[] { new BattleEffectInstance(child, 0f, false, false, new BattleEffectColourSwap[0]) });

            child.BakeDataTexture(out int itemCount);

            Assert.AreEqual(5, itemCount, "its own item + four nested levels");
        }

        [Test]
        public void Bake_InstancedRecipeEdit_RebakesTheParent()
        {
            parent.SetInstances(new[] { new BattleEffectInstance(child, 0f, false, false, new BattleEffectColourSwap[0]) });
            parent.BakeDataTexture(out _);

            child.SetItems(new[] { CreateItem(new BattleEffectCircle()), CreateItem(new BattleEffectCircle()) });
            parent.BakeDataTexture(out int itemCount);

            Assert.AreEqual(2, itemCount);
        }
        #endregion
    }
}

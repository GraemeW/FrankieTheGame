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
            parent.SetInstances(new[] { new BattleEffectInstance(child, 0f, false, false, Vector2.one, Vector2.zero, new BattleEffectColourSwap[0]) });

            Texture2D dataTexture = parent.BakeDataTexture(out int itemCount);

            Assert.AreEqual(2, itemCount);
            Assert.AreEqual(0f, dataTexture.GetPixel(BattleEffectDataLayout.shape, 0).r, "row 0:  the parent's circle");
            Assert.AreEqual(3f, dataTexture.GetPixel(BattleEffectDataLayout.shape, 1).r, "row 1:  the instanced segment");
        }

        [Test]
        public void Bake_StartOffset_DelaysTheInstancedItems()
        {
            parent.SetInstances(new[] { new BattleEffectInstance(child, 2f, false, false, Vector2.one, Vector2.zero, new BattleEffectColourSwap[0]) });

            Texture2D dataTexture = parent.BakeDataTexture(out _);

            Assert.AreEqual(2.5f, dataTexture.GetPixel(BattleEffectDataLayout.timing, 0).r, 1e-5f);
            Assert.AreEqual(3.5f, parent.GetTotalEndTime(), 1e-5f, "end time includes the offset instance");
        }

        [Test]
        public void Bake_MirrorX_FlipsPositionRotationLocalYAndStripes()
        {
            parent.SetInstances(new[] { new BattleEffectInstance(child, 0f, true, false, Vector2.one, Vector2.zero, new BattleEffectColourSwap[0]) });

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
            parent.SetInstances(new[] { new BattleEffectInstance(child, 0f, true, false, Vector2.one, Vector2.zero, new BattleEffectColourSwap[0]) });

            Texture2D dataTexture = parent.BakeDataTexture(out _);

            Color offsets = dataTexture.GetPixel(BattleEffectDataLayout.beatOffsets01, 0);
            Assert.AreEqual(-0.1f, offsets.r, 1e-5f);
            Assert.AreEqual(0.2f, offsets.g, 1e-5f);
        }

        [Test]
        public void Bake_MirrorXAndY_IsAHalfTurnWithoutAFlip()
        {
            parent.SetInstances(new[] { new BattleEffectInstance(child, 0f, true, true, Vector2.one, Vector2.zero, new BattleEffectColourSwap[0]) });

            Texture2D dataTexture = parent.BakeDataTexture(out _);

            AssertTexel(new Color(0.8f, 0.7f, 0.6f, 0.4f), dataTexture.GetPixel(BattleEffectDataLayout.position, 0));
            Assert.AreEqual(Mathf.PI + 30f * Mathf.Deg2Rad, dataTexture.GetPixel(BattleEffectDataLayout.rotation, 0).r, 1e-5f);
            Assert.Greater(dataTexture.GetPixel(BattleEffectDataLayout.scale, 0).g, 0f);
        }

        [Test]
        public void Bake_ColourSwap_ReplacesMatchingColoursOnly()
        {
            parent.SetInstances(new[] { new BattleEffectInstance(child, 0f, false, false, Vector2.one, Vector2.zero, new[] { new BattleEffectColourSwap(Color.blue, Color.white), new BattleEffectColourSwap(Color.red, Color.green) }) });

            Texture2D dataTexture = parent.BakeDataTexture(out _);

            AssertTexel(Color.green.linear, dataTexture.GetPixel(BattleEffectDataLayout.colour, 0));
        }

        [Test]
        public void Bake_ColourSwap_AlsoSwapsTheOutlineAndAltColours()
        {
            child.SetItems(new[] { CreateItem(new BattleEffectCircle(), $"{{\"style\":{{\"mode\":2,\"outlineColour\":{ToJson(Color.blue)}}},\"colourCycle\":{{\"rate\":1}},\"altColours\":{{\"count\":1,\"colourB\":{ToJson(Color.blue)}}}}}") });
            parent.SetInstances(new[] { new BattleEffectInstance(child, 0f, false, false, Vector2.one, Vector2.zero, new[] { new BattleEffectColourSwap(Color.blue, Color.yellow) }) });

            Texture2D dataTexture = parent.BakeDataTexture(out _);

            AssertTexel(Color.yellow.linear, dataTexture.GetPixel(BattleEffectDataLayout.outlineColour, 0), "outline colour");
            AssertTexel(Color.yellow.linear, dataTexture.GetPixel(BattleEffectDataLayout.altColourB, 0), "alt colour B");
        }

        [TestCase(-0.2f, 0.3f, 1f)]
        [TestCase(-1f, 0f, 0.5f)]
        public void Bake_NegativeStartOffset_StartsNoEarlierThanZeroKeepingTheEnd(float offset, float expectedStart, float expectedDuration)
        {
            parent.SetInstances(new[] { new BattleEffectInstance(child, offset, false, false, Vector2.one, Vector2.zero, new BattleEffectColourSwap[0]) });

            Texture2D dataTexture = parent.BakeDataTexture(out _);

            Color timing = dataTexture.GetPixel(BattleEffectDataLayout.timing, 0);
            Assert.AreEqual(expectedStart, timing.r, 1e-5f, "start");
            Assert.AreEqual(expectedDuration, timing.g, 1e-5f, "duration (the child's item:  0.5 -> 1.5)");
        }

        [Test]
        public void Bake_Tiling_ReplacesTheItemsTilingAfterThePlacement()
        {
            child.SetItems(new[] { CreateItem(new BattleEffectCircle(), "{\"tiling\":{\"mode\":1,\"count\":{\"x\":2,\"y\":2},\"spacingEU\":{\"x\":0.1,\"y\":0.1}}}") });
            var tiling = new BattleEffectTiling(BattleEffectTilingMode.StaggeredGrid, new Vector2Int(6, 3), new Vector2(0.25f, 0.5f));
            parent.SetInstances(new[] { new BattleEffectInstance(child, 0f, false, false, new Vector2(0.5f, 0.5f), Vector2.zero, new BattleEffectColourSwap[0], tiling) });

            Texture2D dataTexture = parent.BakeDataTexture(out _);

            AssertTexel(new Color((int)BattleEffectTilingMode.StaggeredGrid, 6f, 3f, 0f), dataTexture.GetPixel(BattleEffectDataLayout.tiling, 0));
            AssertTexel(new Color(0.25f, 0.5f, 0f, 0f), dataTexture.GetPixel(BattleEffectDataLayout.tilingSpacing, 0), "the instance's own units (unscaled)");
        }

        [Test]
        public void Bake_ScaleRatio_ScalesTheItemsOwnTilingSpacing()
        {
            child.SetItems(new[] { CreateItem(new BattleEffectCircle(), "{\"tiling\":{\"mode\":1,\"count\":{\"x\":2,\"y\":2},\"spacingEU\":{\"x\":0.2,\"y\":0.4}}}") });
            parent.SetInstances(new[] { new BattleEffectInstance(child, 0f, false, false, new Vector2(0.5f, 2f), Vector2.zero, new BattleEffectColourSwap[0]) });

            Texture2D dataTexture = parent.BakeDataTexture(out _);

            Color spacing = dataTexture.GetPixel(BattleEffectDataLayout.tilingSpacing, 0);
            Assert.AreEqual(0.1f, spacing.r, 1e-5f);
            Assert.AreEqual(0.8f, spacing.g, 1e-5f);
        }

        [Test]
        public void Bake_MirrorAndPlacement_MoveAnAnchoredLatticesCentre()
        {
            child.SetItems(new[] { CreateItem(new BattleEffectCircle(), "{\"tiling\":{\"mode\":1,\"count\":{\"x\":2,\"y\":2},\"spacingEU\":{\"x\":0.2,\"y\":0.4},\"isAnchored\":true,\"anchorEU\":{\"x\":0.3,\"y\":0.7}}}") });
            parent.SetInstances(new[] { new BattleEffectInstance(child, 0f, true, false, new Vector2(0.5f, 2f), new Vector2(0.1f, -0.1f), new BattleEffectColourSwap[0]) });

            Texture2D dataTexture = parent.BakeDataTexture(out _);

            AssertTexel(new Color(0.1f, 0.8f, 0.7f, 0.8f), dataTexture.GetPixel(BattleEffectDataLayout.tilingSpacing, 0), "spacing scaled;  anchor mirrored, then placed");
        }

        [Test]
        public void Bake_SelfInstance_StopsAtTheDepthLimit()
        {
            child.SetInstances(new[] { new BattleEffectInstance(child, 0f, false, false, Vector2.one, Vector2.zero, new BattleEffectColourSwap[0]) });

            child.BakeDataTexture(out int itemCount);

            Assert.AreEqual(5, itemCount, "its own item + four nested levels");
        }

        [Test]
        public void Bake_ScaleAndOffset_PlaceThePositionAboutTheCentre()
        {
            parent.SetInstances(new[] { new BattleEffectInstance(child, 0f, false, false, new Vector2(0.5f, 0.25f), new Vector2(0.1f, -0.2f), new BattleEffectColourSwap[0]) });

            Texture2D dataTexture = parent.BakeDataTexture(out _);

            AssertTexel(new Color(0.45f, 0.25f, 0.55f, 0.325f), dataTexture.GetPixel(BattleEffectDataLayout.position, 0), "0.5 + (p - 0.5) x ratio + offset");
        }

        [Test]
        public void Bake_Scale_SizesUnrotatedItemsPerAxisAndOutlinesByTheSmallerAxis()
        {
            child.SetItems(new[] { CreateItem(new BattleEffectCircle(), "{\"scaleEU\":{\"start\":{\"x\":0.2,\"y\":0.4},\"end\":{\"x\":0.2,\"y\":0.4}},\"style\":{\"outlineThicknessEU\":{\"start\":0.1,\"end\":0.2}},\"jitter\":{\"amountEU\":{\"x\":0.1,\"y\":0.1}}}") });
            parent.SetInstances(new[] { new BattleEffectInstance(child, 0f, false, false, new Vector2(0.5f, 0.25f), Vector2.zero, new BattleEffectColourSwap[0]) });

            Texture2D dataTexture = parent.BakeDataTexture(out _);

            AssertTexel(new Color(0.1f, 0.1f, 0.1f, 0.1f), dataTexture.GetPixel(BattleEffectDataLayout.scale, 0), "scale");
            Color style = dataTexture.GetPixel(BattleEffectDataLayout.style, 0);
            Assert.AreEqual(0.025f, style.b, 1e-5f, "thickness start");
            Assert.AreEqual(0.05f, style.a, 1e-5f, "thickness end");
            Color jitter = dataTexture.GetPixel(BattleEffectDataLayout.jitter, 0);
            Assert.AreEqual(0.05f, jitter.g, 1e-5f, "jitter x");
            Assert.AreEqual(0.025f, jitter.b, 1e-5f, "jitter y");
        }

        [Test]
        public void Bake_Scale_SwapsTheAxesOfQuarterTurnedItems()
        {
            child.SetItems(new[] { CreateItem(new BattleEffectCircle(), "{\"scaleEU\":{\"start\":{\"x\":0.2,\"y\":0.4},\"end\":{\"x\":0.2,\"y\":0.4}},\"itemRotationDegrees\":{\"start\":90,\"end\":90}}") });
            parent.SetInstances(new[] { new BattleEffectInstance(child, 0f, false, false, new Vector2(0.5f, 0.25f), Vector2.zero, new BattleEffectColourSwap[0]) });

            Texture2D dataTexture = parent.BakeDataTexture(out _);

            AssertTexel(new Color(0.05f, 0.2f, 0.05f, 0.2f), dataTexture.GetPixel(BattleEffectDataLayout.scale, 0), "local x lies along y");
        }

        [Test]
        public void Bake_ScaleAndOffset_KeepTheStripesOnTheItems()
        {
            var ratio = new Vector2(0.5f, 0.25f);
            var offset = new Vector2(0.1f, -0.2f);
            parent.SetInstances(new[] { new BattleEffectInstance(child, 0f, false, false, ratio, offset, new BattleEffectColourSwap[0]) });
            Color before = child.BakeDataTexture(out _).GetPixel(BattleEffectDataLayout.stripes, 0);

            Color after = parent.BakeDataTexture(out _).GetPixel(BattleEffectDataLayout.stripes, 0);

            foreach (Vector2 point in new[] { new Vector2(0.3f, 0.7f), new Vector2(0.9f, 0.1f), new Vector2(0.5f, 0.5f) })
            {
                Vector2 placed = new Vector2(0.5f, 0.5f) + Vector2.Scale(point - new Vector2(0.5f, 0.5f), ratio) + offset;
                float stripeBefore = StripeCoordinate(before, point), stripeAfter = StripeCoordinate(after, placed);
                Assert.AreEqual(0f, Mathf.Abs(Mathf.DeltaAngle(stripeBefore * 360f, stripeAfter * 360f)), 1e-2f, $"stripe at {point}");
            }
        }

        [Test]
        public void ValidateInstances_ResetsAZeroFilledScale()
        {
            parent.SetInstances(new[] { new BattleEffectInstance(child, 0f, false, false, Vector2.zero, Vector2.zero, new BattleEffectColourSwap[0]) });

            parent.ValidateInstances();

            Assert.IsFalse(parent.GetInstances()[0].IsScaleUnset());
        }

        [Test]
        public void Bake_InstancedRecipeEdit_RebakesTheParent()
        {
            parent.SetInstances(new[] { new BattleEffectInstance(child, 0f, false, false, Vector2.one, Vector2.zero, new BattleEffectColourSwap[0]) });
            parent.BakeDataTexture(out _);

            child.SetItems(new[] { CreateItem(new BattleEffectCircle()), CreateItem(new BattleEffectCircle()) });
            parent.BakeDataTexture(out int itemCount);

            Assert.AreEqual(2, itemCount);
        }
        #endregion

        #region PrivateMethods
        private static float StripeCoordinate(Color stripes, Vector2 uv)
        {
            // SDFStripeMask's band coordinate (before frac)
            var across = new Vector2(-Mathf.Sin(stripes.g), Mathf.Cos(stripes.g));
            return Vector2.Dot(uv - new Vector2(0.5f, 0.5f), across) * stripes.r - stripes.a;
        }
        #endregion
    }
}

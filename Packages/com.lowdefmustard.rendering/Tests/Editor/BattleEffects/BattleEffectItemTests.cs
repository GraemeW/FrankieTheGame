using NUnit.Framework;
using UnityEngine;
using static LowDefMustard.Rendering.Tests.Editor.BattleEffectTestData;

namespace LowDefMustard.Rendering.Tests.Editor
{
    public class BattleEffectItemTests
    {
        // Scope here is primarily to check TryWriteData's texel layout (one row of BattleEffectDataLayout.texelsPerItem texels) + effect-scale handling

        // State
        private Color[] texels;
        private BattleEffectFloatRange[] parameterBuffer;

        #region Setup
        [SetUp]
        public void SetUp()
        {
            texels = new Color[BattleEffectDataLayout.texelsPerItem * 2];
            parameterBuffer = new BattleEffectFloatRange[BattleEffectDataLayout.shapeParameterCapacity];
        }
        #endregion

        #region Guards
        [Test]
        public void TryWriteData_NoShape_ReturnsFalseAndLeavesTexelsUntouched()
        {
            BattleEffectItem item = CreateItem(null);

            Assert.IsFalse(item.TryWriteData(texels, 0, parameterBuffer, 1f));
            foreach (Color texel in texels) { Assert.AreEqual(default(Color), texel); }
        }

        [Test]
        public void TryWriteData_StaleParameterBuffer_IsClearedBeforeWrite()
        {
            for (int i = 0; i < parameterBuffer.Length; i++) { parameterBuffer[i] = new BattleEffectFloatRange(9f); }

            CreateItem(new BattleEffectCircle()).TryWriteData(texels, 0, parameterBuffer, 1f);

            AssertTexel(Color.clear, texels[BattleEffectDataLayout.parametersAStart]);
            AssertTexel(Color.clear, texels[BattleEffectDataLayout.parametersBEnd]);
        }
        #endregion

        #region Layout
        [Test]
        public void TryWriteData_Header_WritesShapeIdTimingAndEasing()
        {
            BattleEffectItem item = CreateItem(new BattleEffectDiamond(), "{\"startTime\":0.5,\"duration\":2,\"easing\":2}");

            Assert.IsTrue(item.TryWriteData(texels, 0, parameterBuffer, 1f));

            AssertTexel(new Color(2f, 0.5f, 2f, (int)BattleEffectEasing.EaseOut), texels[BattleEffectDataLayout.header]);
        }

        [Test]
        public void TryWriteData_RowStart_OffsetsEveryTexel()
        {
            int rowStart = BattleEffectDataLayout.texelsPerItem;

            CreateItem(new BattleEffectSegment(), "{\"duration\":3}").TryWriteData(texels, rowStart, parameterBuffer, 1f);

            for (int i = 0; i < rowStart; i++) { Assert.AreEqual(default(Color), texels[i], $"texel {i} of the previous row"); }
            AssertTexel(new Color(3f, 0f, 3f, 0f), texels[rowStart + BattleEffectDataLayout.header]);
        }

        [Test]
        public void TryWriteData_Rotation_ConvertsDegreesToRadians()
        {
            BattleEffectItem item = CreateItem(new BattleEffectCircle(), "{\"rotation\":{\"start\":90,\"end\":180},\"outlineThickness\":{\"start\":0.1,\"end\":-0.2}}");

            item.TryWriteData(texels, 0, parameterBuffer, 1f);

            AssertTexel(new Color(Mathf.PI / 2f, Mathf.PI, 0.1f, -0.2f), texels[BattleEffectDataLayout.rotationOutline]);
        }

        [Test]
        public void TryWriteData_Colour_IsWrittenInLinearSpace()
        {
            var colour = new Color(0.5f, 0.25f, 0.75f, 0.5f);
            BattleEffectItem item = CreateItem(new BattleEffectCircle(), $"{{\"colour\":{ToJson(colour)}}}");

            item.TryWriteData(texels, 0, parameterBuffer, 1f);

            AssertTexel(colour.linear, texels[BattleEffectDataLayout.colour]);
        }

        [Test]
        public void TryWriteData_Repetition_ClampsCountAndPacksMirrorFlags()
        {
            BattleEffectItem item = CreateItem(new BattleEffectCircle(), "{\"repetition\":3,\"count\":{\"x\":0,\"y\":-2},\"mirrorX\":true,\"mirrorY\":true}");

            item.TryWriteData(texels, 0, parameterBuffer, 1f);

            AssertTexel(new Color((int)BattleEffectRepetition.Grid, 1f, 1f, 3f), texels[BattleEffectDataLayout.repetition]);
        }

        [Test]
        public void TryWriteData_ShapeParameters_SplitAcrossStartAndEndTexels()
        {
            // Squiggle defaults:  frequency 30, wave base -0.25, wave peak 0.25, phase speed 1, width 0.9
            CreateItem(new BattleEffectSquiggle()).TryWriteData(texels, 0, parameterBuffer, 1f);

            AssertTexel(new Color(30f, -0.25f, 0.25f, 1f), texels[BattleEffectDataLayout.parametersAStart]);
            AssertTexel(new Color(30f, -0.25f, 0.25f, 1f), texels[BattleEffectDataLayout.parametersAEnd]);
            AssertTexel(new Color(0.9f, 0f, 0f, 0f), texels[BattleEffectDataLayout.parametersBStart]);
            AssertTexel(new Color(0.9f, 0f, 0f, 0f), texels[BattleEffectDataLayout.parametersBEnd]);
        }
        #endregion

        #region EffectScale
        [Test]
        public void TryWriteData_EffectScale_ScalesPositionAboutCentre()
        {
            BattleEffectItem item = CreateItem(new BattleEffectCircle(), "{\"position\":{\"start\":{\"x\":0.75,\"y\":0.5},\"end\":{\"x\":0.25,\"y\":0.25}}}");

            item.TryWriteData(texels, 0, parameterBuffer, 2f);

            AssertTexel(new Color(1f, 0.5f, 0f, 0f), texels[BattleEffectDataLayout.position]);
        }

        [Test]
        public void TryWriteData_EffectScale_ScalesSizeOutlineAndJitterButNotSpacing()
        {
            // Defaults:  scale 0.25, outline 0.05, jitter amount 0.02, spacing 2.5 (shape units ~ follow the shape's own scale)
            CreateItem(new BattleEffectCircle()).TryWriteData(texels, 0, parameterBuffer, 2f);

            AssertTexel(new Color(0.5f, 0.5f, 0.5f, 0.5f), texels[BattleEffectDataLayout.scale]);
            Assert.AreEqual(0.1f, texels[BattleEffectDataLayout.rotationOutline].b, 1e-5f);
            Assert.AreEqual(0.04f, texels[BattleEffectDataLayout.jitter].g, 1e-5f);
            AssertTexel(new Color(2.5f, 2.5f, 2.5f, 2.5f), texels[BattleEffectDataLayout.spacing]);
        }
        #endregion

        #region Accessors
        [Test]
        public void GetEndTime_IsStartPlusDuration()
        {
            Assert.AreEqual(2.5f, CreateItem(new BattleEffectCircle(), "{\"startTime\":0.5,\"duration\":2}").GetEndTime(), 1e-5f);
        }

        [Test]
        public void GetLabel_ReturnsTheInspectorLabel()
        {
            Assert.AreEqual("Pop", CreateItem(new BattleEffectCircle(), "{\"label\":\"Pop\"}").GetLabel());
        }

        [Test]
        public void IsUnset_FreshItem_IsFalse()
        {
            Assert.IsFalse(new BattleEffectItem().IsUnset());
        }

        [Test]
        public void IsUnset_ZeroFilledItem_IsTrue()
        {
            // i.e. what the inspector adds as the first entry of an empty list (field initializers skipped)
            BattleEffectItem item = CreateItem(null, $"{{\"duration\":0,\"colour\":{ToJson(Color.clear)}}}");

            Assert.IsTrue(item.IsUnset());
        }
        #endregion
    }
}

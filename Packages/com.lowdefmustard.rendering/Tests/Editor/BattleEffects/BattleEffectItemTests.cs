using NUnit.Framework;
using UnityEngine;
using static LowDefMustard.Rendering.Tests.Editor.BattleEffectTestData;

namespace LowDefMustard.Rendering.Tests.Editor
{
    public class BattleEffectItemTests
    {
        // Test scope:  primarily checking TryWriteData's texel layout (one row of BattleEffectDataLayout.texelsPerItem texels) + effect-scale handling

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
        public void TryWriteData_ShapeAndTiming_WritesShapeIdStartDurationEasingAndSteps()
        {
            BattleEffectItem item = CreateItem(new BattleEffectDiamond(), "{\"startTimeSeconds\":0.5,\"durationSeconds\":2,\"easing\":2,\"progressSteps\":3}");

            Assert.IsTrue(item.TryWriteData(texels, 0, parameterBuffer, 1f));

            AssertTexel(new Color(4f, 0f, 0f, 0f), texels[BattleEffectDataLayout.shape], "diamond");
            AssertTexel(new Color(0.5f, 2f, (int)BattleEffectEasing.EaseOut, 3f), texels[BattleEffectDataLayout.timing]);
        }

        [Test]
        public void TryWriteData_Approach_WritesEasingAndRatio()
        {
            CreateItem(new BattleEffectCircle(), "{\"easing\":4,\"approachRatio\":10.5}").TryWriteData(texels, 0, parameterBuffer, 1f);

            Assert.AreEqual((int)BattleEffectEasing.Approach, texels[BattleEffectDataLayout.timing].b);
            AssertTexel(new Color(10.5f, 0f, 0f, 0f), texels[BattleEffectDataLayout.easingParameters]);
        }

        [Test]
        public void TryWriteData_Approach_RatioBelowOneIsLinear()
        {
            CreateItem(new BattleEffectCircle(), "{\"approachRatio\":0}").TryWriteData(texels, 0, parameterBuffer, 1f);

            Assert.AreEqual(1f, texels[BattleEffectDataLayout.easingParameters].r);
        }

        [Test]
        public void TryWriteData_RowStart_OffsetsEveryTexel()
        {
            int rowStart = BattleEffectDataLayout.texelsPerItem;

            CreateItem(new BattleEffectSegment(), "{\"durationSeconds\":3}").TryWriteData(texels, rowStart, parameterBuffer, 1f);

            for (int i = 0; i < rowStart; i++) { Assert.AreEqual(default(Color), texels[i], $"texel {i} of the previous row"); }
            AssertTexel(new Color(0f, 3f, 0f, 0f), texels[rowStart + BattleEffectDataLayout.timing]);
        }

        [Test]
        public void TryWriteData_Rotation_PacksItemThenShapeRotationInRadians()
        {
            BattleEffectItem item = CreateItem(new BattleEffectCircle(), "{\"itemRotationDegrees\":{\"start\":90,\"end\":180},\"shapeRotationDegrees\":{\"start\":-45,\"end\":90}}");

            item.TryWriteData(texels, 0, parameterBuffer, 1f);

            AssertTexel(new Color(Mathf.PI / 2f, Mathf.PI, -Mathf.PI / 4f, Mathf.PI / 2f), texels[BattleEffectDataLayout.rotation]);
        }

        [Test]
        public void TryWriteData_Style_PacksBlendAndOutlineThickness()
        {
            BattleEffectItem item = CreateItem(new BattleEffectCircle(), "{\"style\":{\"mode\":2,\"blend\":1,\"outlineThicknessEU\":{\"start\":0.1,\"end\":-0.2}}}");

            item.TryWriteData(texels, 0, parameterBuffer, 1f);

            AssertTexel(new Color((int)BattleEffectStyleMode.FillAndOutline, (int)BattleEffectBlend.Erase, 0.1f, -0.2f), texels[BattleEffectDataLayout.style]);
        }

        [Test]
        public void TryWriteData_Colours_AreWrittenInLinearSpace()
        {
            var colour = new Color(0.5f, 0.25f, 0.75f, 0.5f);
            var outlineColour = new Color(0.1f, 0.9f, 0.3f, 1f);
            BattleEffectItem item = CreateItem(new BattleEffectCircle(), $"{{\"style\":{{\"colour\":{ToJson(colour)},\"outlineColour\":{ToJson(outlineColour)}}}}}");

            item.TryWriteData(texels, 0, parameterBuffer, 1f);

            AssertTexel(colour.linear, texels[BattleEffectDataLayout.colour]);
            AssertTexel(outlineColour.linear, texels[BattleEffectDataLayout.outlineColour]);
        }

        [Test]
        public void TryWriteData_ColourCycle_WritesSourceRateOffsetPaletteCountAndLinearAltColours()
        {
            BattleEffectItem item = CreateItem(new BattleEffectCircle(),
                $"{{\"colourCycle\":{{\"rate\":20,\"phaseOffset\":1.5}},\"altColours\":{{\"count\":2,\"colourB\":{ToJson(Color.red)},\"colourC\":{ToJson(Color.gray)}}}}}");

            item.TryWriteData(texels, 0, parameterBuffer, 1f);

            AssertTexel(new Color((int)BattleEffectColourSource.Time, 20f, 1.5f, 3f), texels[BattleEffectDataLayout.colourCycle]);
            AssertTexel(Color.red.linear, texels[BattleEffectDataLayout.altColourB]);
            AssertTexel(Color.gray.linear, texels[BattleEffectDataLayout.altColourC]);
            AssertTexel(Color.clear, texels[BattleEffectDataLayout.altColourD], "beyond the alt colour count");
        }

        [Test]
        public void TryWriteData_ColourCycle_PatternSource_PacksBase4DigitsAndStepCount()
        {
            BattleEffectItem item = CreateItem(new BattleEffectCircle(), $"{{\"colourCycle\":{{\"source\":2,\"rate\":30,\"pattern\":\"012 3\"}},\"altColours\":{{\"count\":3}}}}");

            item.TryWriteData(texels, 0, parameterBuffer, 1f);

            AssertTexel(new Color((int)BattleEffectColourSource.Pattern, 30f, 0f, 4f), texels[BattleEffectDataLayout.colourCycle]);
            AssertTexel(new Color(0 + 1 * 4 + 2 * 16 + 3 * 64, 0f, 0f, 4f), texels[BattleEffectDataLayout.colourPattern], "step 0 lowest (spaces ignored)");
        }

        [Test]
        public void TryWriteData_ColourCycle_TimeSource_LeavesThePatternClear()
        {
            CreateItem(new BattleEffectCircle(), "{\"colourCycle\":{\"rate\":10,\"pattern\":\"0123\"}}").TryWriteData(texels, 0, parameterBuffer, 1f);

            AssertTexel(Color.clear, texels[BattleEffectDataLayout.colourPattern]);
        }

        [Test]
        public void EncodeColourPattern_SpillsIntoLaterFloatsAndClampsToMaxSteps()
        {
            Color encoded = BattleEffectColourCycle.EncodePattern(new string('3', BattleEffectColourCycle.maxPatternSteps + 5));

            float fullFloat = Mathf.Pow(4f, BattleEffectColourCycle.patternStepsPerFloat) - 1f;
            AssertTexel(new Color(fullFloat, fullFloat, fullFloat, BattleEffectColourCycle.maxPatternSteps), encoded, "every digit 3, exact in a float");
        }

        [Test]
        public void TryWriteData_Style_OutlineFromPalette_WritesItsStepAndKeepsThePalette()
        {
            BattleEffectItem item = CreateItem(new BattleEffectCircle(), "{\"style\":{\"mode\":2,\"isOutlineFromPalette\":true,\"outlinePaletteStep\":-1},\"altColours\":{\"count\":2}}");

            item.TryWriteData(texels, 0, parameterBuffer, 1f);

            AssertTexel(new Color(1f, -1f, 0f, 0f), texels[BattleEffectDataLayout.outlineSource]);
            Assert.AreEqual(3f, texels[BattleEffectDataLayout.colourCycle].a, "the stroke's palette entries count as palette use");
        }

        [Test]
        public void TryWriteData_Style_OutlineFromPalette_OffUnlessFillAndOutline()
        {
            CreateItem(new BattleEffectCircle(), "{\"style\":{\"mode\":1,\"isOutlineFromPalette\":true}}").TryWriteData(texels, 0, parameterBuffer, 1f);

            Assert.AreEqual(0f, texels[BattleEffectDataLayout.outlineSource].r);
        }

        [Test]
        public void TryWriteData_AltColours_ClampsTheCountToTheLayout()
        {
            // i.e. a count set past the inspector's range (e.g. in the asset file)
            BattleEffectItem item = CreateItem(new BattleEffectCircle(), $"{{\"colourCycle\":{{\"rate\":1}},\"altColours\":{{\"count\":9,\"colourD\":{ToJson(Color.blue)}}}}}");

            item.TryWriteData(texels, 0, parameterBuffer, 1f);

            Assert.AreEqual(1 + BattleEffectDataLayout.maxAltColours, texels[BattleEffectDataLayout.colourCycle].a, "colour + every alt colour");
            AssertTexel(Color.blue.linear, texels[BattleEffectDataLayout.altColourD]);
        }

        [Test]
        public void TryWriteData_AltColours_Unused_KeepsTheColourOnly()
        {
            // i.e. nothing steps through the palette (no rate, copy source or beat colour)
            BattleEffectItem item = CreateItem(new BattleEffectCircle(), $"{{\"altColours\":{{\"count\":1,\"colourB\":{ToJson(Color.red)}}}}}");

            item.TryWriteData(texels, 0, parameterBuffer, 1f);

            Assert.AreEqual(1f, texels[BattleEffectDataLayout.colourCycle].a, "palette of one");
            AssertTexel(Color.clear, texels[BattleEffectDataLayout.altColourB]);
        }

        [Test]
        public void TryWriteData_ColourCycle_CopySource_KeepsThePaletteAtRateZero()
        {
            BattleEffectItem item = CreateItem(new BattleEffectCircle(), $"{{\"colourCycle\":{{\"source\":1}},\"altColours\":{{\"count\":1,\"colourB\":{ToJson(Color.red)}}}}}");

            item.TryWriteData(texels, 0, parameterBuffer, 1f);

            AssertTexel(new Color((int)BattleEffectColourSource.Copy, 0f, 0f, 2f), texels[BattleEffectDataLayout.colourCycle]);
            AssertTexel(Color.red.linear, texels[BattleEffectDataLayout.altColourB]);
        }

        [Test]
        public void TryWriteData_OutlineEdgeGradient_WritesFlagStepsAndLinearColour()
        {
            BattleEffectItem item = CreateItem(new BattleEffectCircle(), $"{{\"outlineEdgeGradient\":{{\"isEnabled\":true,\"colour\":{ToJson(Color.red)},\"steps\":2}}}}");

            item.TryWriteData(texels, 0, parameterBuffer, 1f);

            AssertTexel(new Color(1f, 2f, 0f, 0f), texels[BattleEffectDataLayout.edgeGradient]);
            AssertTexel(Color.red.linear, texels[BattleEffectDataLayout.edgeColour]);
        }

        [Test]
        public void TryWriteData_OutlineEdgeGradient_DefaultIsOff()
        {
            CreateItem(new BattleEffectCircle()).TryWriteData(texels, 0, parameterBuffer, 1f);

            AssertTexel(Color.clear, texels[BattleEffectDataLayout.edgeGradient]);
            AssertTexel(Color.clear, texels[BattleEffectDataLayout.edgeColour]);
        }

        [Test]
        public void TryWriteData_Stripes_WritesCountRadiansDutyAndPhase()
        {
            BattleEffectItem item = CreateItem(new BattleEffectCircle(), "{\"stripes\":{\"stripesPerEU\":112,\"angleDegrees\":90,\"dutyFraction\":0.25,\"phaseFraction\":0.5}}");

            item.TryWriteData(texels, 0, parameterBuffer, 1f);

            AssertTexel(new Color(112f, Mathf.PI / 2f, 0.25f, 0.5f), texels[BattleEffectDataLayout.stripes]);
        }

        [Test]
        public void TryWriteData_Stripes_DefaultIsOff()
        {
            CreateItem(new BattleEffectCircle()).TryWriteData(texels, 0, parameterBuffer, 1f);

            Assert.AreEqual(0f, texels[BattleEffectDataLayout.stripes].r);
        }

        [Test]
        public void TryWriteData_Clip_WritesShapeAngleSizesAndOffsets()
        {
            BattleEffectItem item = CreateItem(new BattleEffectCircle(), "{\"clip\":{\"shape\":2,\"halfSizeSU\":{\"start\":{\"x\":3,\"y\":4},\"end\":{\"x\":5,\"y\":6}}," +
                "\"offsetSU\":{\"start\":{\"x\":-1,\"y\":2},\"end\":{\"x\":0.5,\"y\":-3}},\"angleDegrees\":90}}");

            item.TryWriteData(texels, 0, parameterBuffer, 1f);

            AssertTexel(new Color((int)BattleEffectClipShape.Diamond, Mathf.PI / 2f, 0f, 0f), texels[BattleEffectDataLayout.clip]);
            AssertTexel(new Color(3f, 4f, 5f, 6f), texels[BattleEffectDataLayout.clipSize]);
            AssertTexel(new Color(-1f, 2f, 0.5f, -3f), texels[BattleEffectDataLayout.clipOffset]);
        }

        [Test]
        public void TryWriteData_Clip_OffsetDefaultsToTheItemsPosition()
        {
            CreateItem(new BattleEffectCircle(), "{\"clip\":{\"shape\":1}}").TryWriteData(texels, 0, parameterBuffer, 1f);

            AssertTexel(Color.clear, texels[BattleEffectDataLayout.clipOffset]);
        }

        [Test]
        public void TryWriteData_Repetition_ClampsCountAndPacksMirrorFlags()
        {
            BattleEffectItem item = CreateItem(new BattleEffectCircle(), "{\"repetition\":{\"mode\":3,\"count\":{\"x\":0,\"y\":-2},\"mirrorX\":true,\"mirrorY\":true}}");

            item.TryWriteData(texels, 0, parameterBuffer, 1f);

            AssertTexel(new Color((int)BattleEffectRepetitionMode.Grid, 1f, 1f, 3f), texels[BattleEffectDataLayout.repetition]);
        }

        [TestCase(true, false, 5f)]
        [TestCase(false, false, 0f)]
        public void TryWriteData_Repetition_PacksBeatOffsetsSpreadOnlyWhenMirrored(bool mirrorX, bool mirrorY, float expected)
        {
            BattleEffectItem item = CreateItem(new BattleEffectCircle(), $"{{\"repetition\":{{\"mirrorX\":{(mirrorX ? "true" : "false")},\"mirrorY\":{(mirrorY ? "true" : "false")},\"beatOffsetsSpread\":true}}}}");

            item.TryWriteData(texels, 0, parameterBuffer, 1f);

            Assert.AreEqual(expected, texels[BattleEffectDataLayout.repetition].a, "x = 1 + spread = 4;  unmirrored:  nothing to spread");
        }

        [Test]
        public void TryWriteData_Repetition_WritesCopyScaleRatioRange()
        {
            BattleEffectItem item = CreateItem(new BattleEffectCircle(), "{\"repetition\":{\"mode\":1,\"copyScaleRatio\":{\"start\":0.5,\"end\":0.75}}}");

            item.TryWriteData(texels, 0, parameterBuffer, 1f);

            AssertTexel(new Color(0.5f, 0.75f, 0f, 0f), texels[BattleEffectDataLayout.copyScale]);
        }

        [Test]
        public void TryWriteData_Repetition_WritesCopyWave()
        {
            BattleEffectItem item = CreateItem(new BattleEffectCircle(), "{\"repetition\":{\"mode\":1,\"waveform\":4,\"waveCopiesPerCycle\":14,\"waveAmplitudeSU\":{\"start\":6,\"end\":-2}," +
                "\"wavePhaseFraction\":{\"start\":0.25,\"end\":0.5},\"wavePlateauFraction\":{\"start\":0.5,\"end\":0.75}}}");

            item.TryWriteData(texels, 0, parameterBuffer, 1f);

            AssertTexel(new Color((int)BattleEffectWaveform.Trapezoid, 14f, 6f, -2f), texels[BattleEffectDataLayout.copyWave]);
            AssertTexel(new Color(0.25f, 0.5f, 0.5f, 0.75f), texels[BattleEffectDataLayout.copyWavePhase]);
        }

        [Test]
        public void TryWriteData_Repetition_CopyWaveDefaultIsOff()
        {
            CreateItem(new BattleEffectCircle()).TryWriteData(texels, 0, parameterBuffer, 1f);

            Assert.AreEqual(0f, texels[BattleEffectDataLayout.copyWave].b, "amplitude start");
            Assert.AreEqual(0f, texels[BattleEffectDataLayout.copyWave].a, "amplitude end");
        }

        [Test]
        public void TryWriteData_Repetition_CopyScaleRatioDefaultsAndZeroAreOff()
        {
            CreateItem(new BattleEffectCircle()).TryWriteData(texels, 0, parameterBuffer, 1f);
            CreateItem(new BattleEffectCircle(), "{\"repetition\":{\"copyScaleRatio\":{\"start\":0,\"end\":-1}}}").TryWriteData(texels, BattleEffectDataLayout.texelsPerItem, parameterBuffer, 1f);

            AssertTexel(new Color(1f, 1f, 0f, 0f), texels[BattleEffectDataLayout.copyScale], "default");
            AssertTexel(new Color(1f, 1f, 0f, 0f), texels[BattleEffectDataLayout.texelsPerItem + BattleEffectDataLayout.copyScale], "zero-filled / negative");
        }

        [Test]
        public void TryWriteData_Flicker_PacksPatternBitsUnderALeadingOne()
        {
            BattleEffectItem item = CreateItem(new BattleEffectCircle(), "{\"flicker\":{\"rate\":30,\"depthFraction\":1,\"pattern\":\"110 1\"}}");

            item.TryWriteData(texels, 0, parameterBuffer, 1f);

            AssertTexel(new Color(30f, 1f, 0f, 0b1_1011), texels[BattleEffectDataLayout.flicker], "steps 0, 1, 3 bright (spaces ignored)");
        }

        [Test]
        public void TryWriteData_Flicker_NoPatternIsTheSquareWave()
        {
            CreateItem(new BattleEffectCircle(), "{\"flicker\":{\"rate\":10}}").TryWriteData(texels, 0, parameterBuffer, 1f);

            Assert.AreEqual(0f, texels[BattleEffectDataLayout.flicker].a);
        }

        [TestCase("", 0f)]
        [TestCase("x y", 0f)]
        [TestCase("0", 2f)]
        [TestCase("1", 3f)]
        [TestCase("01", 0b110)]
        public void EncodeFlickerPattern_LeadingOneMarksLength(string pattern, float expected)
        {
            Assert.AreEqual(expected, BattleEffectFlicker.EncodePattern(pattern));
        }

        [Test]
        public void EncodeFlickerPattern_ClampsToMaxStepsAndStaysExact()
        {
            float encoded = BattleEffectFlicker.EncodePattern(new string('1', BattleEffectFlicker.maxPatternSteps + 5));

            Assert.AreEqual((1 << (BattleEffectFlicker.maxPatternSteps + 1)) - 1, (int)encoded, "23 bright steps under the leading 1");
            Assert.AreEqual(encoded, (float)(int)encoded, "exact in a float");
        }

        [Test]
        public void TryWriteData_Beats_WritesStartsColoursScaledOffsetsAndElongations()
        {
            BattleEffectItem item = CreateItem(new BattleEffectCircle(),
                "{\"beats\":{\"count\":2,\"beatA\":{\"startSeconds\":0,\"offsetEU\":{\"x\":0.1,\"y\":0},\"elongationSU\":{\"x\":3,\"y\":0},\"colourIndex\":0}," +
                "\"beatB\":{\"startSeconds\":0.5,\"offsetEU\":{\"x\":0,\"y\":-0.2},\"elongationSU\":{\"x\":0,\"y\":4},\"colourIndex\":1}},\"altColours\":{\"count\":1}}");

            item.TryWriteData(texels, 0, parameterBuffer, 2f);

            Color starts = texels[BattleEffectDataLayout.beatStarts];
            Assert.AreEqual(0f, starts.r);
            Assert.AreEqual(0.5f, starts.g);
            Assert.Greater(starts.b, 1e6f, "unused beats never start");
            AssertTexel(new Color(0f, 1f, 0f, 0f), texels[BattleEffectDataLayout.beatColours]);
            AssertTexel(new Color(0.2f, 0f, 0f, -0.4f), texels[BattleEffectDataLayout.beatOffsets01], "x the effect scale");
            AssertTexel(new Color(3f, 0f, 0f, 4f), texels[BattleEffectDataLayout.beatElongations01]);
            Assert.AreEqual(2f, texels[BattleEffectDataLayout.colourCycle].a, "a beat's colour index keeps the palette (w/o a rate)");
        }

        [Test]
        public void TryWriteData_Beats_PatternPacksBase5DigitsRateAndStepCount()
        {
            BattleEffectItem item = CreateItem(new BattleEffectCircle(), "{\"beats\":{\"count\":4,\"pattern\":\"014 23\",\"patternRate\":20}}");

            item.TryWriteData(texels, 0, parameterBuffer, 1f);

            AssertTexel(new Color(0 + 1 * 5 + 4 * 25 + 2 * 125 + 3 * 625, 0f, 20f, 5f), texels[BattleEffectDataLayout.beatPattern], "step 0 lowest (spaces ignored)");
        }

        [Test]
        public void TryWriteData_Beats_PatternNeedsBeatsAndARate()
        {
            CreateItem(new BattleEffectCircle(), "{\"beats\":{\"count\":0,\"pattern\":\"0123\",\"patternRate\":20}}").TryWriteData(texels, 0, parameterBuffer, 1f);
            AssertTexel(Color.clear, texels[BattleEffectDataLayout.beatPattern], "no beats");

            CreateItem(new BattleEffectCircle(), "{\"beats\":{\"count\":2,\"pattern\":\"0123\"}}").TryWriteData(texels, 0, parameterBuffer, 1f);
            AssertTexel(Color.clear, texels[BattleEffectDataLayout.beatPattern], "no rate");
        }

        [Test]
        public void EncodeBeatPattern_SpillsIntoTheSecondFloatAndClampsToMaxSteps()
        {
            Color encoded = BattleEffectBeats.EncodePattern(new string('4', BattleEffectBeats.maxPatternSteps + 5), 10f);

            float fullFloat = Mathf.Pow(5f, BattleEffectBeats.patternStepsPerFloat) - 1f;
            AssertTexel(new Color(fullFloat, fullFloat, 10f, BattleEffectBeats.maxPatternSteps), encoded, "every digit 4, exact in a float");
            Assert.AreEqual(Color.clear, BattleEffectBeats.EncodePattern("x9 ", 10f), "no digits = off");
        }

        [Test]
        public void TryWriteData_Beats_DefaultIsOff()
        {
            CreateItem(new BattleEffectCircle()).TryWriteData(texels, 0, parameterBuffer, 1f);

            Assert.Greater(texels[BattleEffectDataLayout.beatStarts].r, 1e6f);
            AssertTexel(Color.clear, texels[BattleEffectDataLayout.beatPattern]);
        }

        [Test]
        public void TryWriteData_Jitter_PacksModeAndPhaseApart()
        {
            BattleEffectItem item = CreateItem(new BattleEffectCircle(), "{\"jitter\":{\"rate\":12,\"mode\":1,\"phaseFraction\":0.25,\"seed\":7}}");

            item.TryWriteData(texels, 0, parameterBuffer, 1f);

            Assert.AreEqual(12f, texels[BattleEffectDataLayout.jitter].r);
            Assert.AreEqual(7f, texels[BattleEffectDataLayout.jitter].a);
            AssertTexel(new Color((int)BattleEffectJitterMode.Orbit, 0.25f, 0f, 0f), texels[BattleEffectDataLayout.jitterMode]);
        }

        [Test]
        public void TryWriteData_ShapeParameters_SplitAcrossStartAndEndTexels()
        {
            // Jagged line defaults:  half length 1, cell width 0.25, amplitude 0.1, calm 0.3 | seed 0, bump half width 1, variation 0, scroll 0
            CreateItem(new BattleEffectJaggedLine()).TryWriteData(texels, 0, parameterBuffer, 1f);

            AssertTexel(new Color(1f, 0.25f, 0.1f, 0.3f), texels[BattleEffectDataLayout.parametersAStart]);
            AssertTexel(new Color(1f, 0.25f, 0.1f, 0.3f), texels[BattleEffectDataLayout.parametersAEnd]);
            AssertTexel(new Color(0f, 1f, 0f, 0f), texels[BattleEffectDataLayout.parametersBStart]);
            AssertTexel(new Color(0f, 1f, 0f, 0f), texels[BattleEffectDataLayout.parametersBEnd]);
        }
        #endregion

        #region EffectScale
        [Test]
        public void TryWriteData_EffectScale_ScalesPositionAboutCentre()
        {
            BattleEffectItem item = CreateItem(new BattleEffectCircle(), "{\"positionEU\":{\"start\":{\"x\":0.75,\"y\":0.5},\"end\":{\"x\":0.25,\"y\":0.25}}}");

            item.TryWriteData(texels, 0, parameterBuffer, 2f);

            AssertTexel(new Color(1f, 0.5f, 0f, 0f), texels[BattleEffectDataLayout.position]);
        }

        [Test]
        public void TryWriteData_EffectScale_ScalesSizeOutlineAndJitterButNotSpacing()
        {
            // Defaults:  scale 0.25, outline 0.05, jitter amount 0.02, spacing 2.5 (shape units ~ follow the shape's own scale)
            CreateItem(new BattleEffectCircle()).TryWriteData(texels, 0, parameterBuffer, 2f);

            AssertTexel(new Color(0.5f, 0.5f, 0.5f, 0.5f), texels[BattleEffectDataLayout.scale]);
            Assert.AreEqual(0.1f, texels[BattleEffectDataLayout.style].b, 1e-5f);
            Assert.AreEqual(0.04f, texels[BattleEffectDataLayout.jitter].g, 1e-5f);
            AssertTexel(new Color(2.5f, 2.5f, 2.5f, 2.5f), texels[BattleEffectDataLayout.spacing]);
        }

        [Test]
        public void TryWriteData_EffectScale_WidensStripes()
        {
            // i.e. a bigger effect has fewer stripes per effect unit (the bands scale w/ it)
            CreateItem(new BattleEffectCircle(), "{\"stripes\":{\"stripesPerEU\":100}}").TryWriteData(texels, 0, parameterBuffer, 2f);

            Assert.AreEqual(50f, texels[BattleEffectDataLayout.stripes].r, 1e-5f);
        }
        #endregion

        #region Accessors
        [Test]
        public void GetEndTime_IsStartPlusDuration()
        {
            Assert.AreEqual(2.5f, CreateItem(new BattleEffectCircle(), "{\"startTimeSeconds\":0.5,\"durationSeconds\":2}").GetEndTime(), 1e-5f);
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
            BattleEffectItem item = CreateItem(null, $"{{\"durationSeconds\":0,\"style\":{{\"colour\":{ToJson(Color.clear)}}}}}");

            Assert.IsTrue(item.IsUnset());
        }
        #endregion
    }
}

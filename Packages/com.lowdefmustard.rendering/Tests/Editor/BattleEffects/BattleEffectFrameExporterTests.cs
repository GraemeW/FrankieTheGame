using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using LowDefMustard.Rendering.Editor;
using static LowDefMustard.Rendering.Tests.Editor.BattleEffectTestData;

namespace LowDefMustard.Rendering.Tests.Editor
{
    public class BattleEffectFrameExporterTests
    {
        // Notes:
        //  - Real offscreen exports (hidden PanelRenderer -> RenderTexture -> PNG) are made over several editor updates
        //  - These tests are project-dependent && need a real battle effect material (a UI Toolkit graph)
        //  - Shader compilation is forced synchronous (this session only) to ensure the first frames aren't drawn w/ the placeholder shader
        //  - Background red is exact in both gamma + linear (channels of 0 / 1) to ensure pixels compare exactly

        // Const Tunables
        private const string _folder = "Temp/_TEMP_BattleEffectFrameExporter_SafeToDelete";
        private const int _size = 32;

        // State
        private BattleEffectRecipe recipe;
        private BattleEffectFrameExporter exporter;
        private bool wasAsyncCompilationAllowed;
        private bool isFinished;

        #region Setup
        [SetUp]
        public void SetUp()
        {
            wasAsyncCompilationAllowed = ShaderUtil.allowAsyncCompilation;
            ShaderUtil.allowAsyncCompilation = false;
            recipe = ScriptableObject.CreateInstance<BattleEffectRecipe>();
            isFinished = false;
        }

        [TearDown]
        public void TearDown()
        {
            exporter?.Cancel();
            exporter = null;
            ShaderUtil.allowAsyncCompilation = wasAsyncCompilationAllowed;
            Object.DestroyImmediate(recipe);
            if (Directory.Exists(_folder)) { Directory.Delete(_folder, true); }
        }
        #endregion

        #region PrivateMethods
        private IEnumerator Export(int frameCount, bool isTransparent, Vector2Int? size = null, BattleEffectFit fit = BattleEffectFit.AspectFit)
        {
            var renderSettings = new BattleEffectRenderSettings { recipe = recipe, material = RenderingTestMaterials.FindBattleEffectMaterialOrIgnore(), backgroundColour = Color.red, fit = fit };
            exporter = new BattleEffectFrameExporter(renderSettings, new BattleEffectRetroToggles(), new BattleEffectExportRequest(size ?? new Vector2Int(_size, _size), 10f, frameCount, _folder, isTransparent));
            exporter.finished += () => isFinished = true;
            exporter.Start();
            yield return HeadlessEditorWindow.WaitUntil(() => isFinished, 20f);
            Assert.IsTrue(isFinished, "export finished");
            exporter = null;
        }

        private IEnumerator ExportWideCircle(BattleEffectFit fit, bool isSideBackground)
        {
            // White filled circle (radius 0.45) on a 2:1 element ~ aspect fit keeps it round in the centred square, stretch spreads it across the full width
            recipe.SetItems(new[] { CreateItem(new BattleEffectCircle(), $"{{\"durationSeconds\":10,\"style\":{{\"mode\":0}},\"scaleEU\":{{\"start\":{{\"x\":0.45,\"y\":0.45}},\"end\":{{\"x\":0.45,\"y\":0.45}}}},\"colour\":{ToJson(Color.white)}}}") });

            yield return Export(1, false, new Vector2Int(2 * _size, _size), fit);

            Texture2D frame = LoadFrame(1);
            Assert.AreNotEqual((Color32)Color.red, (Color32)frame.GetPixel(_size, _size / 2), "centre is drawn over");
            Assert.AreEqual(isSideBackground, ((Color32)frame.GetPixel(_size / 4, _size / 2)).Equals((Color32)Color.red), "an eighth of the width in from the left edge");
            Object.DestroyImmediate(frame);
        }

        private static Texture2D LoadFrame(int frameNumber)
        {
            var frame = new Texture2D(2, 2);
            Assert.IsTrue(frame.LoadImage(File.ReadAllBytes(Path.Combine(_folder, $"frame_{frameNumber:0000}.png"))));
            return frame;
        }
        #endregion

        #region Export
        [UnityTest]
        public IEnumerator Export_WritesNumberedFramesAtTheRequestedSize()
        {
            yield return Export(3, false);

            Assert.AreEqual(3, Directory.GetFiles(_folder, "frame_*.png").Length);
            Texture2D lastFrame = LoadFrame(3);
            Assert.AreEqual(_size, lastFrame.width);
            Assert.AreEqual(_size, lastFrame.height);
            Object.DestroyImmediate(lastFrame);
        }

        [UnityTest]
        public IEnumerator Export_StaleFrames_AreReplaced()
        {
            Directory.CreateDirectory(_folder);
            File.WriteAllBytes(Path.Combine(_folder, "frame_0009.png"), new byte[] { 0 });

            yield return Export(1, false);

            Assert.AreEqual(1, Directory.GetFiles(_folder, "frame_*.png").Length);
        }

        [UnityTest]
        public IEnumerator Export_EmptyRecipe_IsTheBackgroundColour()
        {
            yield return Export(1, false);

            Texture2D frame = LoadFrame(1);
            Assert.AreEqual((Color32)Color.red, (Color32)frame.GetPixel(_size / 2, _size / 2));
            Object.DestroyImmediate(frame);
        }

        [UnityTest]
        public IEnumerator Export_Transparent_LeavesTheBackgroundClear()
        {
            yield return Export(1, true);

            Texture2D frame = LoadFrame(1);
            Assert.AreEqual(0, ((Color32)frame.GetPixel(0, 0)).a);
            Object.DestroyImmediate(frame);
        }

        [UnityTest]
        public IEnumerator Export_FilledCircle_DrawsOverTheCentreOnly()
        {
            // White filled circle (radius 0.3 effect units) for the whole export
            recipe.SetItems(new[] { CreateItem(new BattleEffectCircle(), $"{{\"durationSeconds\":10,\"style\":{{\"mode\":0}},\"scaleEU\":{{\"start\":{{\"x\":0.3,\"y\":0.3}},\"end\":{{\"x\":0.3,\"y\":0.3}}}},\"colour\":{ToJson(Color.white)}}}") });

            yield return Export(1, false);

            Texture2D frame = LoadFrame(1);
            Assert.AreNotEqual((Color32)Color.red, (Color32)frame.GetPixel(_size / 2, _size / 2), "centre is drawn over");
            Assert.AreEqual((Color32)Color.red, (Color32)frame.GetPixel(0, 0), "corner is background");
            Object.DestroyImmediate(frame);
        }

        [UnityTest]
        public IEnumerator Export_WideElement_AspectFit_LeavesTheSidesEmpty()
        {
            yield return ExportWideCircle(BattleEffectFit.AspectFit, true);
        }

        [UnityTest]
        public IEnumerator Export_WideElement_Stretch_FillsTheSides()
        {
            yield return ExportWideCircle(BattleEffectFit.Stretch, false);
        }

        [UnityTest]
        public IEnumerator Cancel_StopsBeforeAnyFrameIsWritten()
        {
            var renderSettings = new BattleEffectRenderSettings { recipe = recipe, material = RenderingTestMaterials.FindBattleEffectMaterialOrIgnore() };
            exporter = new BattleEffectFrameExporter(renderSettings, new BattleEffectRetroToggles(), new BattleEffectExportRequest(new Vector2Int(_size, _size), 10f, 5, _folder, false));
            exporter.finished += () => isFinished = true;
            exporter.Start();

            exporter.Cancel();
            exporter = null;
            for (int i = 0; i < 30; i++) { yield return null; }

            Assert.IsFalse(isFinished);
            Assert.AreEqual(0, Directory.GetFiles(_folder, "frame_*.png").Length);
        }
        #endregion

        #region Unpremultiply
        [Test]
        public void Unpremultiply_HalfAlpha_DoublesTheLinearColour()
        {
            float premultiplied = Mathf.LinearToGammaSpace(0.25f);

            Assert.AreEqual(Mathf.LinearToGammaSpace(0.5f), BattleEffectFrameExporter.Unpremultiply(premultiplied, 0.5f), 1e-4f);
        }

        [Test]
        public void Unpremultiply_ClampsToOne()
        {
            Assert.AreEqual(1f, BattleEffectFrameExporter.Unpremultiply(1f, 0.25f), 1e-4f);
        }
        #endregion
    }
}

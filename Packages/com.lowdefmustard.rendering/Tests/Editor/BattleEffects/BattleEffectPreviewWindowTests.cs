using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using LowDefMustard.Rendering.Editor;
using static LowDefMustard.Rendering.Tests.Editor.BattleEffectTestData;

namespace LowDefMustard.Rendering.Tests.Editor
{
    public class BattleEffectPreviewWindowTests
    {
        // Notes:
        //  - Opens the real window off-screen (CreateGUI runs on show) + drives it through its internal transport methods
        //  - Playback ticks run on the window's scheduler, so every test pauses first (time only moves when a test moves it)
        //  - Export writes to Temp/BattleEffectPreview/<recipe name>

        // Const Tunables
        private const string _recipeName = "_TEMP_BattleEffectPreviewWindowTests_SafeToDelete";
        private const string _referenceFolder = "Temp/_TEMP_BattleEffectPreviewWindowReference_SafeToDelete";
        private const float _endTailSeconds = 0.25f;

        // State
        private HeadlessEditorWindow host;
        private BattleEffectPreviewWindow window;
        private BattleEffectRecipe recipe;

        #region Setup
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            recipe = ScriptableObject.CreateInstance<BattleEffectRecipe>();
            recipe.name = _recipeName;
            JsonUtility.FromJsonOverwrite("{\"frameRate\":10}", recipe);
            recipe.SetItems(new[] { CreateItem(new BattleEffectCircle(), "{\"startTimeSeconds\":0.5,\"durationSeconds\":1}") });

            window = ScriptableObject.CreateInstance<BattleEffectPreviewWindow>();
            host = new HeadlessEditorWindow(window);
            yield return HeadlessEditorWindow.WaitUntil(() => window.rootVisualElement.Q<ObjectField>() != null);
            if (window.IsPlaying()) { window.TogglePlaying(); }
        }

        [TearDown]
        public void TearDown()
        {
            host.Close();
            Object.DestroyImmediate(recipe);
            foreach (string folder in new[] { _referenceFolder, Path.Combine("Temp/BattleEffectPreview", _recipeName) })
            {
                if (Directory.Exists(folder)) { Directory.Delete(folder, true); }
            }
            host = null;
        }
        #endregion

        #region PrivateMethods
        private Button FindButton(string text) => window.rootVisualElement.Query<Button>().Where(button => button.text == text).First();

        private static void Click(Button button)
        {
            using NavigationSubmitEvent submitEvent = NavigationSubmitEvent.GetPooled();
            submitEvent.target = button;
            button.SendEvent(submitEvent);
        }
        #endregion

        #region GUI
        [Test]
        public void CreateGUI_BuildsTheRecipeMaterialAndSizeControls()
        {
            Assert.IsNotNull(window.rootVisualElement.Query<ObjectField>().Where(field => field.label == "Recipe").First());
            Assert.IsNotNull(window.rootVisualElement.Query<ObjectField>().Where(field => field.label == "Material").First());
            DropdownField sizeField = window.rootVisualElement.Query<DropdownField>().Where(field => field.label == "Size").First();
            Assert.IsNotNull(sizeField);
            var referenceSizes = new[] { "Reference frames x0.25", "Reference frames x0.5", "Reference frames x1", "Reference frames x2" };
            CollectionAssert.AreEqual(BattleEffectPreviewSizes.GetPresets().Select(preset => preset.name).Concat(referenceSizes), sizeField.choices, "presets, then the reference sizes");
            Assert.IsNotNull(window.rootVisualElement.Q<BattleEffectTimelineElement>());
        }

        [Test]
        public void TimeLabel_NoRecipe_AsksForOne()
        {
            window.SetRecipe(null);

            StringAssert.Contains("Assign a recipe", window.rootVisualElement.Query<Label>().ToList().Select(label => label.text).Aggregate("", string.Concat));
        }
        #endregion

        #region Transport
        [Test]
        public void SetRecipe_RewindsAndShowsTheRecipe()
        {
            window.Seek(1f);

            window.SetRecipe(recipe);

            Assert.AreEqual(0f, window.GetTime());
            Assert.AreSame(recipe, window.rootVisualElement.Query<ObjectField>().Where(field => field.label == "Recipe").First().value);
            Assert.AreEqual(1.5f + _endTailSeconds, window.GetTotalTime(), 1e-5f, "recipe end + tail");
        }

        [TestCase(-1f, 0f)]
        [TestCase(0.75f, 0.75f)]
        [TestCase(100f, 1.75f)]
        public void Seek_ClampsToTheTotalTime(float seekTime, float expectedTime)
        {
            window.SetRecipe(recipe);

            window.Seek(seekTime);

            Assert.AreEqual(expectedTime, window.GetTime(), 1e-5f);
        }

        [Test]
        public void Step_MovesOneFrameAtTheRecipeRateAndPauses()
        {
            window.SetRecipe(recipe);
            window.TogglePlaying();

            window.Step(1);
            window.Step(1);

            Assert.IsFalse(window.IsPlaying());
            Assert.AreEqual(10f, window.GetStepRate());
            Assert.AreEqual(0.2f, window.GetTime(), 1e-5f);
        }

        [Test]
        public void GetStepRate_SmoothRecipe_FallsBackTo30()
        {
            JsonUtility.FromJsonOverwrite("{\"frameRate\":0}", recipe);
            window.SetRecipe(recipe);

            Assert.AreEqual(30f, window.GetStepRate());
        }

        [Test]
        public void TogglePlaying_AtTheEnd_RestartsFromZero()
        {
            window.SetRecipe(recipe);
            window.Seek(window.GetTotalTime());

            window.TogglePlaying();

            Assert.IsTrue(window.IsPlaying());
            Assert.AreEqual(0f, window.GetTime());
        }

        [Test]
        public void TransportButtons_DriveTheTransport()
        {
            window.SetRecipe(recipe);

            Click(FindButton("Frame >"));
            Assert.AreEqual(0.1f, window.GetTime(), 1e-5f, "frame >");

            Click(FindButton("< Frame"));
            Assert.AreEqual(0f, window.GetTime(), 1e-5f, "< frame");

            window.Seek(1f);
            Click(FindButton("Restart"));
            Assert.AreEqual(0f, window.GetTime(), "restart");

            Click(FindButton("Play"));
            Assert.IsTrue(window.IsPlaying(), "play");
        }
        #endregion

        #region Sizes
        [Test]
        public void Preview_ShowsAnOffscreenTextureAtThePreviewSize()
        {
            DropdownField sizeField = window.rootVisualElement.Query<DropdownField>().Where(field => field.label == "Size").First();
            sizeField.index = 0;
            RenderTexture firstTexture = window.GetPreviewTexture();
            Vector2 firstSize = BattleEffectPreviewSizes.GetPresets()[0].size;

            Assert.IsNotNull(firstTexture, "drawn via a render texture (the game's colour path), not the editor panel");
            Assert.AreEqual(new Vector2Int(Mathf.RoundToInt(firstSize.x), Mathf.RoundToInt(firstSize.y)), new Vector2Int(firstTexture.width, firstTexture.height));
        }

        [Test]
        public void Preview_SizeChange_SwapsTheTextureForTheNewSize()
        {
            DropdownField sizeField = window.rootVisualElement.Query<DropdownField>().Where(field => field.label == "Size").First();
            sizeField.index = 0;
            RenderTexture firstTexture = window.GetPreviewTexture();
            var presets = BattleEffectPreviewSizes.GetPresets().ToList();
            int otherIndex = presets.FindIndex(preset => preset.size != presets[0].size);
            if (otherIndex < 0) { Assert.Ignore("Needs two size presets of different sizes"); }

            sizeField.index = otherIndex;

            Vector2 otherSize = presets[otherIndex].size;
            Assert.AreNotSame(firstTexture, window.GetPreviewTexture());
            Assert.AreEqual(Mathf.RoundToInt(otherSize.x), window.GetPreviewTexture().width);
        }

        [Test]
        public void SizeDropdown_SelectsThePresetSize()
        {
            DropdownField sizeField = window.rootVisualElement.Query<DropdownField>().Where(field => field.label == "Size").First();

            sizeField.index = 0;

            Assert.AreEqual(BattleEffectPreviewSizes.GetPresets()[0].size, window.GetPreviewSize());
        }

        [Test]
        public void ReferenceSize_NoReferenceFrames_FallsBackToTheLastPreset()
        {
            DropdownField sizeField = window.rootVisualElement.Query<DropdownField>().Where(field => field.label == "Size").First();

            sizeField.index = sizeField.choices.Count - 1;

            Assert.AreEqual(BattleEffectPreviewSizes.GetPresets().Last().size, window.GetPreviewSize());
        }

        [Test]
        public void ReferenceFrames_SetTheReferenceSizeAndExtendTheTotalTime()
        {
            Directory.CreateDirectory(_referenceFolder);
            var frame = new Texture2D(8, 6);
            for (int i = 0; i < 90; i++) { File.WriteAllBytes(Path.Combine(_referenceFolder, $"frame_{i:0000}.png"), frame.EncodeToPNG()); }
            Object.DestroyImmediate(frame);
            window.SetRecipe(recipe);
            DropdownField sizeField = window.rootVisualElement.Query<DropdownField>().Where(field => field.label == "Size").First();

            window.SetReferenceFolder(_referenceFolder);
            sizeField.index = sizeField.choices.Count - 1;

            Assert.AreEqual(new Vector2(16f, 12f), window.GetPreviewSize(), "reference frames x2");
            Assert.AreEqual(3f + _endTailSeconds, window.GetTotalTime(), 1e-5f, "90 frames @ 30 fps outlast the recipe");
        }

        [TestCase("Reference frames x0.25", 2f, 1.5f)]
        [TestCase("Reference frames x0.5", 4f, 3f)]
        [TestCase("Reference frames x1", 8f, 6f)]
        [TestCase("Reference frames x2", 16f, 12f)]
        public void ReferenceSizes_ScaleTheReferenceFrameSize(string choice, float expectedWidth, float expectedHeight)
        {
            Directory.CreateDirectory(_referenceFolder);
            var frame = new Texture2D(8, 6);
            File.WriteAllBytes(Path.Combine(_referenceFolder, "frame_0000.png"), frame.EncodeToPNG());
            Object.DestroyImmediate(frame);
            window.SetRecipe(recipe);
            DropdownField sizeField = window.rootVisualElement.Query<DropdownField>().Where(field => field.label == "Size").First();

            window.SetReferenceFolder(_referenceFolder);
            sizeField.index = sizeField.choices.IndexOf(choice);

            Assert.AreEqual(new Vector2(expectedWidth, expectedHeight), window.GetPreviewSize());
        }
        #endregion

        #region Export
        [Test]
        public void StartExport_NotRenderable_DoesNothing()
        {
            window.SetRecipe(null);

            window.StartExport();

            Assert.IsFalse(window.IsExporting());
        }

        [UnityTest]
        public IEnumerator StartExport_Renderable_ExportsEveryFrame()
        {
            // Project-dependent:  renders w/ a real battle effect material
            window.GetRenderSettings().material = RenderingTestMaterials.FindBattleEffectMaterialOrIgnore();
            window.SetRecipe(recipe);

            window.StartExport();
            Assert.IsTrue(window.IsExporting());
            yield return HeadlessEditorWindow.WaitUntil(() => !window.IsExporting(), 20f);

            Assert.IsFalse(window.IsExporting(), "export finished");
            string folder = Path.Combine("Temp/BattleEffectPreview", _recipeName);
            Assert.AreEqual(15, Directory.GetFiles(folder, "frame_*.png").Length, "1.5s @ the recipe's 10 fps");
        }
        #endregion
    }
}

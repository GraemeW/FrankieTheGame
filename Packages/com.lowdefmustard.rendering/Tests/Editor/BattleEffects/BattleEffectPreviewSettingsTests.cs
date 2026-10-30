using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using LowDefMustard.Rendering.Editor;

namespace LowDefMustard.Rendering.Tests.Editor
{
    public class BattleEffectPreviewSettingsTests
    {
        // Note:  Size presets are global editor state (a project registers its own on load) ~ thus:  snapshot + restore around every test

        // Const Tunables
        private const string _testPresetName = "_TEMP_TestPreset_SafeToDelete";

        // State
        private List<BattleEffectPreviewSize> registeredPresets;
        private BattleEffectRecipe recipe;

        #region Setup
        [SetUp]
        public void SetUp()
        {
            registeredPresets = BattleEffectPreviewSizes.GetPresets().ToList();
            recipe = ScriptableObject.CreateInstance<BattleEffectRecipe>();
            JsonUtility.FromJsonOverwrite("{\"frameRate\":12}", recipe);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(recipe);
            foreach (BattleEffectPreviewSize preset in BattleEffectPreviewSizes.GetPresets().ToList()) { BattleEffectPreviewSizes.Unregister(preset.name); }
            if (registeredPresets.SequenceEqual(BattleEffectPreviewSizes.GetPresets())) { return; } // Nothing was registered (only the default was listed)
            foreach (BattleEffectPreviewSize preset in registeredPresets) { BattleEffectPreviewSizes.Register(preset.name, preset.size); }
        }
        #endregion

        #region PreviewSizes
        [Test]
        public void PreviewSize_EqualsByNameOnly()
        {
            var preset = new BattleEffectPreviewSize(_testPresetName, new Vector2(10f, 20f));

            Assert.AreEqual(preset, new BattleEffectPreviewSize(_testPresetName, new Vector2(30f, 40f)));
            Assert.AreEqual(preset.GetHashCode(), new BattleEffectPreviewSize(_testPresetName, Vector2.zero).GetHashCode());
            Assert.AreNotEqual(preset, new BattleEffectPreviewSize("Other", new Vector2(10f, 20f)));
        }

        [Test]
        public void Register_AddsPreset()
        {
            BattleEffectPreviewSizes.Register(_testPresetName, new Vector2(10f, 20f));

            BattleEffectPreviewSize preset = BattleEffectPreviewSizes.GetPresets().Single(listed => listed.name == _testPresetName);
            Assert.AreEqual(new Vector2(10f, 20f), preset.size);
        }

        [Test]
        public void Register_ListsInRegistrationOrder()
        {
            BattleEffectPreviewSizes.Register(_testPresetName + "B", new Vector2(10f, 20f));
            BattleEffectPreviewSizes.Register(_testPresetName + "A", new Vector2(10f, 20f));

            List<string> names = BattleEffectPreviewSizes.GetPresets().Select(preset => preset.name).ToList();
            Assert.AreEqual(names.Count - 2, names.IndexOf(_testPresetName + "B"), "appended after any existing presets");
            Assert.AreEqual(names.Count - 1, names.IndexOf(_testPresetName + "A"));
        }

        [Test]
        public void Register_ExistingName_ReplacesTheSizeInPlace()
        {
            BattleEffectPreviewSizes.Register(_testPresetName, new Vector2(10f, 20f));
            BattleEffectPreviewSizes.Register(_testPresetName + "Later", new Vector2(10f, 20f));
            int index = BattleEffectPreviewSizes.GetPresets().ToList().FindIndex(preset => preset.name == _testPresetName);

            BattleEffectPreviewSizes.Register(_testPresetName, new Vector2(30f, 40f));

            List<BattleEffectPreviewSize> presets = BattleEffectPreviewSizes.GetPresets().ToList();
            Assert.AreEqual(1, presets.Count(preset => preset.name == _testPresetName));
            Assert.AreEqual(index, presets.FindIndex(preset => preset.name == _testPresetName), "keeps its place");
            Assert.AreEqual(new Vector2(30f, 40f), presets[index].size);
        }

        [Test]
        public void Unregister_RemovesPreset()
        {
            BattleEffectPreviewSizes.Register(_testPresetName, new Vector2(10f, 20f));

            Assert.IsTrue(BattleEffectPreviewSizes.Unregister(_testPresetName));
            Assert.IsFalse(BattleEffectPreviewSizes.GetPresets().Any(preset => preset.name == _testPresetName));
            Assert.IsFalse(BattleEffectPreviewSizes.Unregister(_testPresetName));
        }

        [Test]
        public void GetPresets_NoneRegistered_ListsOnlyTheDefault()
        {
            BattleEffectPreviewSizes.Register(_testPresetName, new Vector2(10f, 20f));
            foreach (BattleEffectPreviewSize preset in BattleEffectPreviewSizes.GetPresets().ToList()) { BattleEffectPreviewSizes.Unregister(preset.name); }

            IReadOnlyList<BattleEffectPreviewSize> presets = BattleEffectPreviewSizes.GetPresets();

            Assert.AreEqual(1, presets.Count);
            Assert.Greater(presets[0].size.x, 0f);
            Assert.Greater(presets[0].size.y, 0f);
        }
        #endregion

        #region RetroToggles
        [Test]
        public void GetFrameRate_SteppedTime_UsesTheRecipeFrameRate()
        {
            Assert.AreEqual(12f, new BattleEffectRetroToggles { isSteppedTime = true }.GetFrameRate(recipe));
        }

        [Test]
        public void GetFrameRate_SmoothTime_IsZero()
        {
            Assert.AreEqual(0f, new BattleEffectRetroToggles { isSteppedTime = false }.GetFrameRate(recipe));
        }

        [Test]
        public void GetFrameRate_NoRecipe_IsZero()
        {
            Assert.AreEqual(0f, new BattleEffectRetroToggles().GetFrameRate(null));
        }
        #endregion

        #region RenderSettings
        [Test]
        public void IsRenderable_NeedsRecipeAndMaterial()
        {
            var renderSettings = new BattleEffectRenderSettings { recipe = recipe };

            Assert.IsFalse(renderSettings.isRenderable);
        }

        [Test]
        public void IsBattleEffectMaterial_NullOrOtherShader_IsFalse()
        {
            var otherMaterial = new Material(Shader.Find("Hidden/Internal-Colored"));

            Assert.IsFalse(BattleEffectPropertyRefs.IsBattleEffectMaterial(null));
            Assert.IsFalse(BattleEffectPropertyRefs.IsBattleEffectMaterial(otherMaterial));
            Object.DestroyImmediate(otherMaterial);
        }

        [Test]
        public void FindMaterialIfUnset_FindsABattleEffectMaterialInTheProject()
        {
            // Project-dependent:  needs a material using a graph w/ the BattleEffect recipe properties somewhere under Assets
            var renderSettings = new BattleEffectRenderSettings { tint = null };

            renderSettings.FindMaterialIfUnset();

            Assert.IsNotNull(renderSettings.tint, "tint is restored");
            if (renderSettings.material == null) { Assert.Ignore("No BattleEffect material in this project"); }
            Assert.IsTrue(BattleEffectPropertyRefs.IsBattleEffectMaterial(renderSettings.material));
        }
        #endregion
    }
}

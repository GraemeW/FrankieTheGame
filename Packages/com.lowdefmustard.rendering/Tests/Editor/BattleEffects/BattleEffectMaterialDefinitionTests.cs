using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using LowDefMustard.Rendering.Editor;
using static LowDefMustard.Rendering.Tests.Editor.BattleEffectTestData;

namespace LowDefMustard.Rendering.Tests.Editor
{
    public class BattleEffectMaterialDefinitionTests
    {
        // State
        private Material material;
        private BattleEffectRecipe recipe;

        #region Setup
        [SetUp]
        public void SetUp()
        {
            material = RenderingTestMaterials.CreatePropertiesMaterial();
            recipe = ScriptableObject.CreateInstance<BattleEffectRecipe>();
            JsonUtility.FromJsonOverwrite("{\"frameRate\":12,\"scale\":2,\"edgeSoftness\":1.5,\"glowWidth\":0.1}", recipe);
            recipe.SetItems(new[] { CreateItem(new BattleEffectCircle()), CreateItem(new BattleEffectDiamond()) });
        }

        [TearDown]
        public void TearDown()
        {
            if (recipe != null) { Object.DestroyImmediate(recipe); }
            Object.DestroyImmediate(material);
        }
        #endregion

        #region Recipe
        [Test]
        public void RecipeApplyTo_BindsTheBakedTextureAndSettings()
        {
            var materialDefinition = new MaterialDefinition(material);

            recipe.ApplyTo(ref materialDefinition);

            Assert.AreSame(recipe.BakeDataTexture(out int itemCount), materialDefinition.GetTexture("_ItemData"));
            Assert.AreEqual(itemCount, materialDefinition.GetFloat("_ItemCount"));
            Assert.AreEqual(12f, materialDefinition.GetFloat("_FrameRate"));
            Assert.AreEqual(1.5f, materialDefinition.GetFloat("_EdgeSoftness"));
            Assert.AreEqual(0.2f, materialDefinition.GetFloat("_GlowWidth"), 1e-5f, "glow width x recipe scale");
            Assert.AreEqual((int)BattleEffectFit.AspectFit, materialDefinition.GetFloat("_FitMode"), "aspect fit by default");
        }

        [Test]
        public void RecipeApplyTo_Stretch_SetsTheFitMode()
        {
            var materialDefinition = new MaterialDefinition(material);

            recipe.ApplyTo(ref materialDefinition, BattleEffectFit.Stretch);

            Assert.AreEqual((int)BattleEffectFit.Stretch, materialDefinition.GetFloat("_FitMode"));
        }

        [Test]
        public void RecipeOnDisable_DestroysTheDataTexture()
        {
            Texture2D dataTexture = recipe.BakeDataTexture(out _);

            Object.DestroyImmediate(recipe);

            Assert.IsTrue(dataTexture == null);
        }
        #endregion

        #region Tint
        [Test]
        public void TintApplyTo_WritesColoursAndSettingsInRadians()
        {
            var tint = new BattleEffectTint();
            JsonUtility.FromJsonOverwrite($"{{\"mode\":2,\"colourA\":{ToJson(Color.red)},\"colourB\":{ToJson(Color.blue)},\"angle\":90,\"frequency\":3,\"speed\":0.5}}", tint);
            var materialDefinition = new MaterialDefinition(material);

            tint.ApplyTo(ref materialDefinition);

            Assert.AreEqual(Color.red, materialDefinition.GetColor("_TintColourA"));
            Assert.AreEqual(Color.blue, materialDefinition.GetColor("_TintColourB"));
            Vector4 settings = materialDefinition.GetVector("_TintSettings");
            Assert.AreEqual((int)BattleEffectTintMode.Wave, settings.x);
            Assert.AreEqual(Mathf.PI / 2f, settings.y, 1e-5f);
            Assert.AreEqual(3f, settings.z);
            Assert.AreEqual(0.5f, settings.w);
        }
        #endregion

        #region PropertyRefs
        [Test]
        public void IsBattleEffectMaterial_ShaderWithRecipeProperties_IsTrue()
        {
            Assert.IsTrue(BattleEffectPropertyRefs.IsBattleEffectMaterial(material));
        }

        [TestCase(true, true, 0f, 0f)]
        [TestCase(false, false, 1f, 1f)]
        public void SetPreview_WritesTimeFrameRateAndRetroFlags(bool isFlickerEnabled, bool isJitterEnabled, float expectedDisableFlicker, float expectedDisableJitter)
        {
            var materialDefinition = new MaterialDefinition(material);

            BattleEffectPropertyRefs.SetPreview(ref materialDefinition, 0.75f, 24f, isFlickerEnabled, isJitterEnabled);

            Assert.AreEqual(1f, materialDefinition.GetFloat("_IsPreview"));
            Assert.AreEqual(0.75f, materialDefinition.GetFloat("_PreviewTime"));
            Assert.AreEqual(24f, materialDefinition.GetFloat("_FrameRate"));
            Assert.AreEqual(expectedDisableFlicker, materialDefinition.GetFloat("_DisableFlicker"));
            Assert.AreEqual(expectedDisableJitter, materialDefinition.GetFloat("_DisableJitter"));
        }
        #endregion

        #region RenderSettings
        [Test]
        public void CreateMaterialDefinition_AppliesRecipeTintAndPreview()
        {
            var renderSettings = new BattleEffectRenderSettings { recipe = recipe, material = material, tint = new BattleEffectTint(Color.yellow) };

            MaterialDefinition materialDefinition = renderSettings.CreateMaterialDefinition(1.25f, new BattleEffectRetroToggles());

            Assert.AreSame(material, materialDefinition.material);
            Assert.AreEqual(2f, materialDefinition.GetFloat("_ItemCount"));
            Assert.AreEqual(Color.yellow, materialDefinition.GetColor("_TintColourA"));
            Assert.AreEqual(1.25f, materialDefinition.GetFloat("_PreviewTime"));
            Assert.AreEqual(12f, materialDefinition.GetFloat("_FrameRate"), "stepped time keeps the recipe's frame rate");
        }

        [Test]
        public void CreateMaterialDefinition_SmoothTime_OverridesTheRecipeFrameRate()
        {
            var renderSettings = new BattleEffectRenderSettings { recipe = recipe, material = material };

            MaterialDefinition materialDefinition = renderSettings.CreateMaterialDefinition(0f, new BattleEffectRetroToggles { isSteppedTime = false });

            Assert.AreEqual(0f, materialDefinition.GetFloat("_FrameRate"));
        }

        [Test]
        public void CreateMaterialDefinition_PassesTheFitMode()
        {
            var renderSettings = new BattleEffectRenderSettings { recipe = recipe, material = material, fit = BattleEffectFit.Stretch };

            MaterialDefinition materialDefinition = renderSettings.CreateMaterialDefinition(0f, new BattleEffectRetroToggles());

            Assert.AreEqual((int)BattleEffectFit.Stretch, materialDefinition.GetFloat("_FitMode"));
        }

        [Test]
        public void IsRenderable_RecipeAndMaterial_IsTrue()
        {
            Assert.IsTrue(new BattleEffectRenderSettings { recipe = recipe, material = material }.isRenderable);
        }
        #endregion
    }
}

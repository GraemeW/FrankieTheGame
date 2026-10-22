using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace LowDefMustard.Rendering.Tests.Editor
{
    public class ShaderPropertyRefsTests
    {
        // State
        private GameObject rendererObject;
        private SpriteRenderer spriteRenderer;
        private Material material;
        private Texture2D texture;

        #region Setup
        [SetUp]
        public void SetUp()
        {
            rendererObject = new GameObject("_TEMP_ShaderPropertyRefsTests");
            spriteRenderer = rendererObject.AddComponent<SpriteRenderer>();
            material = RenderingTestMaterials.CreatePropertiesMaterial();
            texture = new Texture2D(2, 2);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(rendererObject);
            Object.DestroyImmediate(material);
            Object.DestroyImmediate(texture);
        }
        #endregion

        #region PrivateMethods
        private MaterialPropertyBlock GetPropertyBlock()
        {
            var propertyBlock = new MaterialPropertyBlock();
            spriteRenderer.GetPropertyBlock(propertyBlock);
            return propertyBlock;
        }
        #endregion

        #region Renderer
        [Test]
        public void SetMainTexture_Renderer_WritesThePropertyBlock()
        {
            ShaderPropertyRefs.SetMainTexture(spriteRenderer, texture);

            Assert.AreSame(texture, GetPropertyBlock().GetTexture("_MainTex"));
        }

        [Test]
        public void SetTintAndPhase_Renderer_KeepEarlierPropertyBlockValues()
        {
            ShaderPropertyRefs.SetTint(spriteRenderer, Color.red);
            ShaderPropertyRefs.SetShaderPhase(spriteRenderer, 2.5f);

            MaterialPropertyBlock propertyBlock = GetPropertyBlock();
            Assert.AreEqual(Color.red, propertyBlock.GetColor("_Tint"));
            Assert.AreEqual(2.5f, propertyBlock.GetFloat("_Phase"));
        }

        [Test]
        public void RendererSetters_NullRenderer_DoNothing()
        {
            Assert.DoesNotThrow(() =>
            {
                ShaderPropertyRefs.SetMainTexture((Renderer)null, texture);
                ShaderPropertyRefs.SetTint((Renderer)null, Color.red);
                ShaderPropertyRefs.SetShaderPhase((Renderer)null, 1f);
            });
        }
        #endregion

        #region Material
        [Test]
        public void MaterialSetters_WriteEachProperty()
        {
            ShaderPropertyRefs.SetMainTexture(material, texture);
            ShaderPropertyRefs.SetTint(material, Color.green);
            ShaderPropertyRefs.SetShaderPhase(material, 1.5f);
            ShaderPropertyRefs.SetFadeTime(material, 3f);
            ShaderPropertyRefs.SetFadeOutTime(material, 0.75f);

            Assert.AreSame(texture, material.GetTexture("_MainTex"));
            Assert.AreEqual(Color.green, material.GetColor("_Tint"));
            Assert.AreEqual(1.5f, material.GetFloat("_Phase"));
            Assert.AreEqual(3f, material.GetFloat("_FadeTime"));
            Assert.AreEqual(0.75f, material.GetFloat("_FadeOutTime"));
        }

        [Test]
        public void SetMainTexture_NullTexture_KeepsTheCurrentTexture()
        {
            ShaderPropertyRefs.SetMainTexture(material, texture);

            ShaderPropertyRefs.SetMainTexture(material, null);

            Assert.AreSame(texture, material.GetTexture("_MainTex"));
        }

        [Test]
        public void MaterialSetters_NullMaterial_DoNothing()
        {
            Assert.DoesNotThrow(() =>
            {
                ShaderPropertyRefs.SetMainTexture((Material)null, texture);
                ShaderPropertyRefs.SetTint((Material)null, Color.red);
                ShaderPropertyRefs.SetShaderPhase((Material)null, 1f);
                ShaderPropertyRefs.SetFadeTime(null, 1f);
                ShaderPropertyRefs.SetFadeOutTime(null, 1f);
            });
        }
        #endregion

        #region MaterialDefinition
        [Test]
        public void SetShaderPhase_MaterialDefinition_OverridesOnlyTheDefinition()
        {
            var materialDefinition = new MaterialDefinition(material);

            ShaderPropertyRefs.SetShaderPhase(ref materialDefinition, 4f);

            Assert.AreEqual(4f, materialDefinition.GetFloat("_Phase"));
            Assert.AreEqual(0f, material.GetFloat("_Phase"), "the shared material is untouched");
        }
        #endregion
    }
}

using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace LowDefMustard.Rendering.Tests.Editor
{
    public class GraphicsWarmupStatesTests
    {
        // State
        private Material material;
        private PanelSettings panelSettings;
        private GraphicsStateCollection collection;

        #region Setup
        [SetUp]
        public void SetUp()
        {
            material = RenderingTestMaterials.CreatePropertiesMaterial();
            panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
        }

        [TearDown]
        public void TearDown()
        {
            if (collection != null) { Object.DestroyImmediate(collection); }
            Object.DestroyImmediate(panelSettings);
            Object.DestroyImmediate(material);
        }
        #endregion
        
        #region PrivateMethods
        private List<GraphicsStateCollection.ShaderVariant> GetVariants()
        {
            var variants = new List<GraphicsStateCollection.ShaderVariant>();
            collection.GetVariants(variants);
            return variants;
        }

        private List<GraphicsStateCollection.GraphicsState> GetStates()
        {
            var states = new List<GraphicsStateCollection.GraphicsState>();
            collection.GetGraphicsStatesForVariant(GetVariants()[0], states);
            return states;
        }

        private static bool IsSameLayout(VertexAttributeDescriptor[] expected, VertexAttributeDescriptor[] actual)
        {
            if (actual == null || actual.Length != expected.Length) { return false; }
            for (int i = 0; i < expected.Length; i++)
            {
                if (actual[i].attribute != expected[i].attribute || actual[i].format != expected[i].format || actual[i].dimension != expected[i].dimension || actual[i].stream != expected[i].stream) { return false; }
            }
            return true;
        }
        #endregion

        #region Tests
        [TestCase(GraphicsWarmupPath.UIToolkitMaterial, 1)]
        [TestCase(GraphicsWarmupPath.Sprite, 2)]
        [TestCase(GraphicsWarmupPath.SpriteShape, 1)]
        public void Build_AddsAStatePerLayoutForTheDrawnPassOnly(GraphicsWarmupPath path, int expectedStates)
        {
            collection = GraphicsWarmupStates.Build(new[] { new GraphicsWarmupEntry(path, material, null, panelSettings) });

            List<GraphicsStateCollection.ShaderVariant> variants = GetVariants();
            Assert.AreEqual(1, variants.Count);
            Assert.AreSame(material.shader, variants[0].shader);
            Assert.AreEqual(0, variants[0].passId.PassIndex);
            Assert.AreEqual(expectedStates, collection.GetGraphicsStateCountForVariant(variants[0]));
        }

        [TestCase(GraphicsWarmupPath.UIToolkitMaterial)]
        [TestCase(GraphicsWarmupPath.Sprite)]
        [TestCase(GraphicsWarmupPath.SpriteShape)]
        public void Build_StatesUseThePathsLayouts(GraphicsWarmupPath path)
        {
            collection = GraphicsWarmupStates.Build(new[] { new GraphicsWarmupEntry(path, material, null, panelSettings) });

            List<GraphicsStateCollection.GraphicsState> states = GetStates();
            IReadOnlyList<VertexAttributeDescriptor[]> layouts = GraphicsWarmupStates.GetVertexLayouts(path);
            Assert.AreEqual(layouts.Count, states.Count);
            foreach (VertexAttributeDescriptor[] layout in layouts)
            {
                Assert.IsTrue(states.Exists(state => IsSameLayout(layout, state.vertexAttributes)), $"no state w/ the {layout.Length}-attribute layout");
            }
        }

        [Test]
        public void Build_UIToolkitMaterial_DrawsToColourPlusDepthStencilWithUIToolkitsStencil()
        {
            collection = GraphicsWarmupStates.Build(new[] { new GraphicsWarmupEntry(GraphicsWarmupPath.UIToolkitMaterial, material, null, panelSettings) });

            GraphicsStateCollection.GraphicsState state = GetStates()[0];
            Assert.AreEqual(2, state.attachments.Length);
            Assert.AreEqual(SystemInfo.GetGraphicsFormat(DefaultFormat.LDR), state.attachments[0].graphicsFormat);
            Assert.AreEqual(SystemInfo.GetGraphicsFormat(DefaultFormat.DepthStencil), state.attachments[1].graphicsFormat);
            Assert.AreEqual(1, state.depthAttachmentIndex);
            StencilState stencil = state.renderState.stencilState;
            Assert.IsTrue(stencil.enabled);
            Assert.AreEqual(CompareFunction.Equal, stencil.compareFunctionFront);
            Assert.AreEqual(StencilOp.IncrementSaturate, stencil.zFailOperationFront);
            Assert.AreEqual(StencilOp.DecrementSaturate, stencil.zFailOperationBack);
        }

        [Test]
        public void Build_Sprite_WithTargetTexture_UsesItsFormats()
        {
            var targetTexture = new RenderTexture(new RenderTextureDescriptor(4, 4, GraphicsFormat.R8G8B8A8_UNorm, GraphicsFormat.D32_SFloat_S8_UInt));
            try
            {
                collection = GraphicsWarmupStates.Build(new[] { new GraphicsWarmupEntry(GraphicsWarmupPath.Sprite, material, null, null, targetTexture) });

                GraphicsStateCollection.GraphicsState state = GetStates()[0];
                Assert.AreEqual(0, state.depthAttachmentIndex, "sprites draw in URP's native render pass:  depth first");
                Assert.AreEqual(GraphicsFormat.D32_SFloat_S8_UInt, state.attachments[0].graphicsFormat);
                Assert.AreEqual(GraphicsFormat.R8G8B8A8_UNorm, state.attachments[1].graphicsFormat);
                Assert.IsFalse(state.renderState.stencilState.enabled, "UI Toolkit's stencil is UI only");
            }
            finally { Object.DestroyImmediate(targetTexture); }
        }

        [Test]
        public void Build_TakesTheBlendStateFromTheMaterialsPass()
        {
            collection = GraphicsWarmupStates.Build(new[] { new GraphicsWarmupEntry(GraphicsWarmupPath.UIToolkitMaterial, material, null, panelSettings) });

            RenderTargetBlendState blend = GetStates()[0].renderState.blendState.blendState0;
            Assert.AreEqual(BlendMode.SrcAlpha, blend.sourceColorBlendMode, "RenderingTestProperties blends SrcAlpha OneMinusSrcAlpha");
            Assert.AreEqual(BlendMode.OneMinusSrcAlpha, blend.destinationColorBlendMode);
        }

        [Test]
        public void Build_SkipsInvalidEntries()
        {
            collection = GraphicsWarmupStates.Build(new[]
            {
                null,
                new GraphicsWarmupEntry(GraphicsWarmupPath.UIToolkitMaterial, material, null, null),       // UI Toolkit w/o panel settings
                new GraphicsWarmupEntry(GraphicsWarmupPath.UIToolkitBackdropFilter, null, null, panelSettings), // filter path w/o a filter
                new GraphicsWarmupEntry(GraphicsWarmupPath.Sprite, null, null, null)
            });

            Assert.AreEqual(0, collection.variantCount);
        }

        [Test]
        public void Build_NullEntries_ReturnsAnEmptyCollection()
        {
            collection = GraphicsWarmupStates.Build(null);

            Assert.IsNotNull(collection);
            Assert.AreEqual(0, collection.variantCount);
        }
        #endregion
    }
}

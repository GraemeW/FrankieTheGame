using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace LowDefMustard.Rendering
{
    public static class GraphicsWarmupStates
    {
        // Notes:
        //  - Unity derives each variant's render state (blend, depth, cull) from the material's own pass
        //  - The draw path supplies vertex layout(s), render target(s) + any state the renderer forces (UI Toolkit's stencil)
        //  - Attachment load / store actions are left generic ~ they aren't part of an API-level pipeline state (Metal / D3D12 / Vulkan render pass compatibility)

        // Const Tunables
        private const int _spritePass = 0;
        private const int _uiToolkitMaterialPass = 0;
        private const byte _stencilMask = 255;

        // Vertex layouts
        private static readonly VertexAttributeDescriptor[] _uiToolkitLayout =
        {
            new(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
            new(VertexAttribute.Color, VertexAttributeFormat.UNorm8, 4),
            new(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 4),
            new(VertexAttribute.TexCoord4, VertexAttributeFormat.UInt32, 4),
            new(VertexAttribute.TexCoord5, VertexAttributeFormat.Float32, 4)
        };
        private static readonly VertexAttributeDescriptor[] _filterBlitLayout = // Unity's full-screen blit quad
        {
            new(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
            new(VertexAttribute.Normal, VertexAttributeFormat.Float32, 3),
            new(VertexAttribute.Color, VertexAttributeFormat.Float32, 4),
            new(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 3),
            new(VertexAttribute.TexCoord1, VertexAttributeFormat.Float32, 3),
            new(VertexAttribute.TexCoord2, VertexAttributeFormat.Float32, 3),
            new(VertexAttribute.TexCoord3, VertexAttributeFormat.Float32, 3)
        };
        private static readonly VertexAttributeDescriptor[] _spriteLayout = // SpriteRenderer:  one stream per attribute
        {
            new(VertexAttribute.Position, VertexAttributeFormat.Float32, 3, 0),
            new(VertexAttribute.Color, VertexAttributeFormat.UNorm8, 4, 2),
            new(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2, 1)
        };
        private static readonly VertexAttributeDescriptor[] _spriteBatchedLayout = // Batched sprites:  interleaved
        {
            new(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
            new(VertexAttribute.Color, VertexAttributeFormat.UNorm8, 4),
            new(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2)
        };
        private static readonly VertexAttributeDescriptor[] _spriteShapeLayout = // SpriteShapeRenderer:  one stream per attribute w/ a normal
        {
            new(VertexAttribute.Position, VertexAttributeFormat.Float32, 3, 0),
            new(VertexAttribute.Normal, VertexAttributeFormat.Float32, 3, 2),
            new(VertexAttribute.Color, VertexAttributeFormat.UNorm8, 4, 3),
            new(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2, 1)
        };

        // UI Toolkit forces its clipping stencil test on every draw w/ a depth-stencil target
        private static readonly StencilState _uiToolkitStencil = new(true, _stencilMask, _stencilMask,
            CompareFunction.Equal, StencilOp.Keep, StencilOp.Keep, StencilOp.IncrementSaturate,
            CompareFunction.Less, StencilOp.Keep, StencilOp.Keep, StencilOp.DecrementSaturate);

        #region PublicMethods
        public static GraphicsStateCollection Build(IEnumerable<GraphicsWarmupEntry> entries)
        {
            var collection = new GraphicsStateCollection();
            if (entries == null) { return collection; }

            foreach (GraphicsWarmupEntry entry in entries)
            {
                if (entry == null || !entry.IsValid()) { continue; }
                DrawTarget target = GetDrawTarget(entry);
                foreach ((Material material, int passIndex) in GetDrawnPasses(entry))
                {
                    foreach (VertexAttributeDescriptor[] layout in GetVertexLayouts(entry.GetPath())) { AddPass(collection, material, passIndex, layout, target); }
                }
            }
            return collection;
        }

        public static IReadOnlyList<VertexAttributeDescriptor[]> GetVertexLayouts(GraphicsWarmupPath path) => path switch
        {
            GraphicsWarmupPath.UIToolkitMaterial => new[] { _uiToolkitLayout },
            GraphicsWarmupPath.UIToolkitBackdropFilter => new[] { _filterBlitLayout },
            GraphicsWarmupPath.Sprite => new[] { _spriteLayout, _spriteBatchedLayout },
            GraphicsWarmupPath.SpriteShape => new[] { _spriteShapeLayout },
            _ => new[] { _uiToolkitLayout }
        };

        public static GraphicsFormat GetColorFormat(GraphicsWarmupEntry entry) => entry.IsSprite() && entry.GetTargetTexture() != null ? entry.GetTargetTexture().graphicsFormat : SystemInfo.GetGraphicsFormat(DefaultFormat.LDR);
        #endregion

        #region PrivateMethods
        private static DrawTarget GetDrawTarget(GraphicsWarmupEntry entry)
        {
            GraphicsFormat color = GetColorFormat(entry);
            GraphicsFormat depthStencil = SystemInfo.GetGraphicsFormat(DefaultFormat.DepthStencil);
            return entry.GetPath() switch
            {
                // Backdrop filters draw into a colour-only intermediate target
                GraphicsWarmupPath.UIToolkitBackdropFilter => new DrawTarget(color, GraphicsFormat.None, false, -1, false),
                // Sprites draw in URP's native render pass:  depth first, colour as its output
                GraphicsWarmupPath.Sprite or GraphicsWarmupPath.SpriteShape => new DrawTarget(color, GetSpriteDepthFormat(entry, depthStencil), true, 0, false),
                _ => new DrawTarget(color, depthStencil, false, -1, true)
            };
        }

        private static GraphicsFormat GetSpriteDepthFormat(GraphicsWarmupEntry entry, GraphicsFormat fallback)
        {
            RenderTexture targetTexture = entry.GetTargetTexture();
            return targetTexture != null && targetTexture.depthStencilFormat != GraphicsFormat.None ? targetTexture.depthStencilFormat : fallback;
        }

        private static IEnumerable<(Material, int)> GetDrawnPasses(GraphicsWarmupEntry entry)
        {
            switch (entry.GetPath())
            {
                case GraphicsWarmupPath.UIToolkitBackdropFilter:
                    foreach (var pass in entry.GetFilter().passes) { if (pass.material != null) { yield return (pass.material, pass.passIndex); } }
                    break;
                case GraphicsWarmupPath.Sprite:
                case GraphicsWarmupPath.SpriteShape:
                    yield return (entry.GetMaterial(), _spritePass);
                    break;
                case GraphicsWarmupPath.UIToolkitMaterial:
                default:
                    yield return (entry.GetMaterial(), _uiToolkitMaterialPass);
                    break;
            }
        }

        private static void AddPass(GraphicsStateCollection collection, Material material, int passIndex, VertexAttributeDescriptor[] layout, DrawTarget target)
        {
            // AddGraphicsStates covers every pass of the material -> stage in a scratch collection + only copy over the drawn pass
            Mesh layoutMesh = CreateLayoutMesh(layout);
            var scratch = new GraphicsStateCollection();
            bool hasDepth = target.depthStencil != GraphicsFormat.None;
            var attachments = new NativeArray<AttachmentDescriptor>(hasDepth ? 2 : 1, Allocator.Temp);
            var subPasses = new NativeArray<SubPassDescriptor>(1, Allocator.Temp);
            try
            {
                int colorIndex = hasDepth && target.isDepthFirst ? 1 : 0;
                int depthIndex = hasDepth ? 1 - colorIndex : -1;
                attachments[colorIndex] = CreateAttachment(target.color);
                if (hasDepth) { attachments[depthIndex] = CreateAttachment(target.depthStencil); }
                var subPass = new SubPassDescriptor { colorOutputs = new AttachmentIndexArray(1) { [0] = colorIndex } };
                subPasses[0] = subPass;
                if (!scratch.AddGraphicsStates(new[] { layoutMesh }, new[] { material }, 1, attachments, subPasses, target.subPassIndex, depthIndex, -1)) { return; }

                var variants = new List<GraphicsStateCollection.ShaderVariant>();
                var states = new List<GraphicsStateCollection.GraphicsState>();
                scratch.GetVariants(variants);
                foreach (GraphicsStateCollection.ShaderVariant variant in variants.Where(variant => variant.passId.PassIndex == passIndex))
                {
                    collection.AddVariant(variant.shader, variant.passId, variant.keywords);
                    scratch.GetGraphicsStatesForVariant(variant, states);
                    foreach (GraphicsStateCollection.GraphicsState state in states) { collection.AddGraphicsStateForVariant(variant, WithDrawStencil(state, target.hasUIToolkitStencil)); }
                }
            }
            finally
            {
                attachments.Dispose();
                subPasses.Dispose();
                DestroyObject(scratch);
                DestroyObject(layoutMesh);
            }
        }

        private static AttachmentDescriptor CreateAttachment(GraphicsFormat format) => new(format) { loadAction = RenderBufferLoadAction.Load, storeAction = RenderBufferStoreAction.Store };

        private static GraphicsStateCollection.GraphicsState WithDrawStencil(GraphicsStateCollection.GraphicsState state, bool hasUIToolkitStencil)
        {
            // Note:  the reference is set per draw, where AddGraphicsStates defaults it to 1
            RenderStateBlock renderState = state.renderState;
            renderState.stencilReference = 0;
            if (hasUIToolkitStencil)
            {
                renderState.stencilState = _uiToolkitStencil;
                renderState.mask |= RenderStateMask.Stencil;
            }
            state.renderState = renderState;
            return state;
        }

        private static Mesh CreateLayoutMesh(VertexAttributeDescriptor[] layout)
        {
            var mesh = new Mesh { hideFlags = HideFlags.HideAndDontSave };
            mesh.SetVertexBufferParams(4, layout);
            mesh.SetIndexBufferParams(6, IndexFormat.UInt16);
            mesh.SetIndexBufferData(new ushort[] { 0, 1, 2, 2, 3, 0 }, 0, 0, 6, MeshUpdateFlags.DontValidateIndices);
            mesh.SetSubMesh(0, new SubMeshDescriptor(0, 6), MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontRecalculateBounds);
            return mesh;
        }

        private static void DestroyObject(Object target)
        {
            if (target == null) { return; }
            if (Application.isPlaying) { Object.Destroy(target); }
            else { Object.DestroyImmediate(target); }
        }
        #endregion

        // Nested Types
        private readonly struct DrawTarget
        {
            public readonly GraphicsFormat color;
            public readonly GraphicsFormat depthStencil; // None = colour only
            public readonly bool isDepthFirst;
            public readonly int subPassIndex; // -1 = not in a native render pass
            public readonly bool hasUIToolkitStencil;

            public DrawTarget(GraphicsFormat color, GraphicsFormat depthStencil, bool isDepthFirst, int subPassIndex, bool hasUIToolkitStencil)
            {
                this.color = color;
                this.depthStencil = depthStencil;
                this.isDepthFirst = isDepthFirst;
                this.subPassIndex = subPassIndex;
                this.hasUIToolkitStencil = hasUIToolkitStencil;
            }
        }
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace LowDefMustard.Rendering
{
    public sealed class GraphicsWarmupProbe : VisualElement
    {
        // Notes:
        //  - Not hidden via display / visibility / opacity since UI Toolkit may skip those draws entirely
        //  - Drawn w/ a clear tint, so most materials draw nothing visible

        // Const Tunables
        private const float _sizePixels = 1f;

        public GraphicsWarmupProbe(GraphicsWarmupEntry entry)
        {
            pickingMode = PickingMode.Ignore;
            style.position = Position.Absolute;
            style.left = 0f;
            style.top = 0f;
            style.width = _sizePixels;
            style.height = _sizePixels;

            if (entry.GetPath() == GraphicsWarmupPath.UIToolkitBackdropFilter)
            {
                style.backdropFilter = new List<FilterFunction> { new(entry.GetFilter()) };
                return;
            }
            style.unityMaterial = new StyleMaterialDefinition(new MaterialDefinition(entry.GetMaterial()));
            generateVisualContent += DrawProbe;
        }

        private static void DrawProbe(MeshGenerationContext meshGenerationContext) => ShaderQuad.Draw(meshGenerationContext, new Rect(0f, 0f, _sizePixels, _sizePixels), Color.clear);
    }
}

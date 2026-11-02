using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace LowDefMustard.Rendering
{
    [Serializable]
    public sealed class GraphicsWarmupEntry
    {
        // Notes:
        //  - The UI Toolkit paths also need the panel settings that the in-game element is drawn on
        //  - Sprites drawn by a camera into a render texture name it as their target texture

        // Tunables
        [SerializeField] private GraphicsWarmupPath path;
        [SerializeField] private Material material;
        [SerializeField] private FilterFunctionDefinition filter;
        [SerializeField] private PanelSettings panelSettings;
        [SerializeField] private RenderTexture targetTexture;

        // Constructor
        public GraphicsWarmupEntry(GraphicsWarmupPath path, Material material, FilterFunctionDefinition filter, PanelSettings panelSettings, RenderTexture targetTexture = null)
        {
            this.path = path;
            this.material = material;
            this.filter = filter;
            this.panelSettings = panelSettings;
            this.targetTexture = targetTexture;
        }

        #region PublicMethods
        public GraphicsWarmupPath GetPath() => path;
        public Material GetMaterial() => material;
        public FilterFunctionDefinition GetFilter() => filter;
        public PanelSettings GetPanelSettings() => panelSettings;
        public RenderTexture GetTargetTexture() => targetTexture;
        public bool IsUIToolkit() => path is GraphicsWarmupPath.UIToolkitMaterial or GraphicsWarmupPath.UIToolkitBackdropFilter;

        public bool IsValid() => path switch
        {
            GraphicsWarmupPath.UIToolkitMaterial => material != null && panelSettings != null,
            GraphicsWarmupPath.UIToolkitBackdropFilter => filter != null && filter.passes is { Length: > 0 } && panelSettings != null,
            GraphicsWarmupPath.Sprite => material != null,
            _ => false
        };
        #endregion
    }
}

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

        // Editor Field References
        internal const string pathField = nameof(path);
        internal const string materialField = nameof(material);
        internal const string filterField = nameof(filter);
        internal const string panelSettingsField = nameof(panelSettings);
        internal const string targetTextureField = nameof(targetTexture);

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
        public bool IsUIToolkit() => IsUIToolkit(path);
        public bool IsSprite() => IsSprite(path);

        public bool IsValid() => Enum.IsDefined(typeof(GraphicsWarmupPath), path)
                                 && (!UsesMaterial(path) || material != null)
                                 && (!UsesFilter(path) || (filter != null && filter.passes is { Length: > 0 }))
                                 && (!UsesPanelSettings(path) || panelSettings != null);
        #endregion

        #region PathRules
        internal static bool IsUIToolkit(GraphicsWarmupPath path) => path is GraphicsWarmupPath.UIToolkitMaterial or GraphicsWarmupPath.UIToolkitBackdropFilter;
        internal static bool IsSprite(GraphicsWarmupPath path) => path is GraphicsWarmupPath.Sprite or GraphicsWarmupPath.SpriteShape;
        internal static bool UsesMaterial(GraphicsWarmupPath path) => path is GraphicsWarmupPath.UIToolkitMaterial || IsSprite(path);
        internal static bool UsesFilter(GraphicsWarmupPath path) => path is GraphicsWarmupPath.UIToolkitBackdropFilter;
        internal static bool UsesPanelSettings(GraphicsWarmupPath path) => IsUIToolkit(path);
        internal static bool UsesTargetTexture(GraphicsWarmupPath path) => IsSprite(path); // Optional:  only when the camera draws into a render texture
        #endregion
    }
}

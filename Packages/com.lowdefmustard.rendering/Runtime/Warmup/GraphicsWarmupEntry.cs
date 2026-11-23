using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UIElements;
using LowDefMustard.Core;

namespace LowDefMustard.Rendering
{
    [Serializable]
    public sealed class GraphicsWarmupEntry
    {
        // Notes:
        //  - The UI Toolkit paths also need the panel settings that the in-game element is drawn on
        //  - Sprites drawn by a camera into a render texture name it as their target texture
        //  - Materials drawn by Addressables content go in addressableMaterial (not material):  a direct reference warms a separate built-in copy

        // Tunables
        [SerializeField] private GraphicsWarmupPath path;
        [SerializeField] private Material material;
        [SerializeField][Tooltip("Use instead of material when Addressables content draws it")] private AssetReferenceT<Material> addressableMaterial;
        [SerializeField] private FilterFunctionDefinition filter;
        [SerializeField] private PanelSettings panelSettings;
        [SerializeField] private RenderTexture targetTexture;

        // Editor Field References
        internal const string pathField = nameof(path);
        internal const string materialField = nameof(material);
        internal const string addressableMaterialField = nameof(addressableMaterial);
        internal const string filterField = nameof(filter);
        internal const string panelSettingsField = nameof(panelSettings);
        internal const string targetTextureField = nameof(targetTexture);

        // State
        [NonSerialized] private AsyncOperationHandle<Material> addressableMaterialHandle;

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
        public Material GetMaterial() => material != null ? material : addressableMaterialHandle.IsValid() ? addressableMaterialHandle.Result : null;
        public FilterFunctionDefinition GetFilter() => filter;
        public PanelSettings GetPanelSettings() => panelSettings;
        public RenderTexture GetTargetTexture() => targetTexture;
        public bool IsUIToolkit() => path is GraphicsWarmupPath.UIToolkitMaterial or GraphicsWarmupPath.UIToolkitBackdropFilter;
        public bool IsSprite() => path is GraphicsWarmupPath.Sprite or GraphicsWarmupPath.SpriteShape;

        public bool IsValid() => path switch
        {
            GraphicsWarmupPath.UIToolkitMaterial => GetMaterial() != null && panelSettings != null,
            GraphicsWarmupPath.UIToolkitBackdropFilter => filter != null && filter.passes is { Length: > 0 } && panelSettings != null,
            GraphicsWarmupPath.Sprite or GraphicsWarmupPath.SpriteShape => GetMaterial() != null,
            _ => false
        };

        public void LoadAddressableMaterial()
        {
            // Held until ReleaseAddressableMaterial ~ keeps the warmed copy loaded for the content that draws it
            if (material != null || addressableMaterial == null || !addressableMaterial.RuntimeKeyIsValid() || addressableMaterialHandle.IsValid()) { return; }
            addressableMaterialHandle = Addressables.LoadAssetAsync<Material>(addressableMaterial.RuntimeKey);
            addressableMaterialHandle.WaitForCompletion();
            if (addressableMaterialHandle.Status != AsyncOperationStatus.Succeeded) { LDMLog.Warning(LDMLogCategory.Rendering, $"Failed to load the Addressables warmup material '{addressableMaterial.RuntimeKey}'. Skipping its warmup."); }
        }

        public void ReleaseAddressableMaterial()
        {
            if (addressableMaterialHandle.IsValid()) { Addressables.Release(addressableMaterialHandle); }
            addressableMaterialHandle = default;
        }

#if UNITY_EDITOR
        internal Material GetEditorMaterial() => material != null ? material : addressableMaterial?.editorAsset;
#endif
        #endregion
    }
}

#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace LowDefMustard.Rendering.Editor
{
    public sealed class BattleEffectOffscreenRenderer : IDisposable
    {
        // Notes:
        //  - Draws a battle effect into a RenderTexture through a hidden runtime UI Toolkit panel to ensure accurate colour representation
        //      - N.B. this is required as Unity's in-editor UI renders in gamma space only, so rendering onto a simple editor panel results in linear/gamma mismatch (everything dim)
        //  - The panel renders on later editor updates:  wait a few after a change before reading the texture back

        // State
        private readonly Vector2Int size;
        private RenderTexture texture;
        private PanelSettings panelSettings;
        private GameObject host;
        private VisualElement effect;
        private StyleMaterialDefinition material = StyleKeyword.Null;
        private int builtVersion = -1;

        public BattleEffectOffscreenRenderer(Vector2Int size, Color clearColour)
        {
            this.size = new Vector2Int(Mathf.Max(size.x, 1), Mathf.Max(size.y, 1));
            texture = new RenderTexture(this.size.x, this.size.y, 0, RenderTextureFormat.ARGB32) { hideFlags = HideFlags.HideAndDontSave };
            texture.Create();

            panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
            panelSettings.hideFlags = HideFlags.HideAndDontSave;
            panelSettings.targetTexture = texture;
            panelSettings.scaleMode = PanelScaleMode.ConstantPixelSize;
            panelSettings.scale = 1f;
            panelSettings.clearColor = true;
            panelSettings.colorClearValue = clearColour;

            host = new GameObject(nameof(BattleEffectOffscreenRenderer)) { hideFlags = HideFlags.HideAndDontSave };
            var panelRenderer = host.AddComponent<PanelRenderer>();
            panelRenderer.panelSettings = panelSettings;
            panelRenderer.RegisterUIReloadCallback(BuildEffect);
        }

        public RenderTexture GetTexture() => texture;
        public Vector2Int GetSize() => size;
        public bool IsBuilt() => effect != null;

        public void SetMaterial(StyleMaterialDefinition setMaterial)
        {
            // Held until the panel has built the effect (the first build comes a few editor updates after construction)
            material = setMaterial;
            if (effect == null) { return; }
            effect.style.unityMaterial = material;
            effect.MarkDirtyRepaint();
        }

        public void SetClearColour(Color clearColour)
        {
            if (panelSettings != null) { panelSettings.colorClearValue = clearColour; }
            effect?.MarkDirtyRepaint();
        }

        public void Dispose()
        {
            if (host != null) { Object.DestroyImmediate(host); }
            if (panelSettings != null) { Object.DestroyImmediate(panelSettings); }
            if (texture != null)
            {
                texture.Release();
                Object.DestroyImmediate(texture);
            }
            host = null;
            panelSettings = null;
            texture = null;
            effect = null;
        }

        #region PrivateMethods
        private void BuildEffect(PanelRenderer panelRenderer, VisualElement root, int version)
        {
            if (version == builtVersion) { return; }
            builtVersion = version;
            effect = new VisualElement { style = { position = Position.Absolute, left = 0f, top = 0f, width = size.x, height = size.y } };
            effect.generateVisualContent += meshGenerationContext => ShaderQuad.Draw(meshGenerationContext, new Rect(0f, 0f, 1f, 1f), Color.white); // Tinted in the shader
            root.Add(effect);
            SetMaterial(material);
        }
        #endregion
    }
}
#endif

#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Frankie.Combat.UI;
using Object = UnityEngine.Object;

namespace Frankie.Combat.Editor
{
    public sealed class BattleEffectFrameExporter
    {
        // Notes:
        //  - Renders a recipe frame by frame into a RenderTexture via a hidden UI Toolkit panel (edit mode), writing frame_####.png
        //  - Read back from the render texture, never from the screen (OS screen reads crash the editor on macOS)
        //  - Frames are sampled mid-frame, so stepped time + reference frames land on the same frame index

        // Const Tunables
        private const int _initialSettleUpdates = 10; // First render of a new panel takes longer
        private const int _settleUpdates = 3;

        // State
        private readonly BattleEffectRenderSettings renderSettings;
        private readonly BattleEffectRetroToggles retroToggles;
        private readonly BattleEffectExportRequest exportRequest;
        
        private RenderTexture target;
        private PanelSettings panelSettings;
        private GameObject host;
        private VisualElement effect;
        private Texture2D readback;
        private int frame;
        private int settleUpdates;
        private int builtVersion = -1;

        // Events
        public event Action finished;

        public BattleEffectFrameExporter(BattleEffectRenderSettings renderSettings, BattleEffectRetroToggles retroToggles, BattleEffectExportRequest exportRequest)
        {
            this.renderSettings = renderSettings;
            this.retroToggles = retroToggles;
            this.exportRequest = exportRequest;
            Vector2Int size = exportRequest.size;

            target = new RenderTexture(size.x, size.y, 0, RenderTextureFormat.ARGB32) { hideFlags = HideFlags.HideAndDontSave };
            target.Create();
            
            panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
            panelSettings.hideFlags = HideFlags.HideAndDontSave;
            panelSettings.targetTexture = target;
            panelSettings.scaleMode = PanelScaleMode.ConstantPixelSize;
            panelSettings.scale = 1f;
            panelSettings.clearColor = true;
            panelSettings.colorClearValue = exportRequest.isTransparent ? Color.clear : renderSettings.backgroundColour;
            
            readback = new Texture2D(size.x, size.y, exportRequest.isTransparent ? TextureFormat.RGBA32 : TextureFormat.RGB24, false) { hideFlags = HideFlags.HideAndDontSave };
        }

        public void Start()
        {
            Directory.CreateDirectory(exportRequest.folder);
            foreach (string staleFrame in Directory.GetFiles(exportRequest.folder, "frame_*.png")) { File.Delete(staleFrame); }

            host = new GameObject(nameof(BattleEffectFrameExporter)) { hideFlags = HideFlags.HideAndDontSave };
            var panelRenderer = host.AddComponent<PanelRenderer>();
            panelRenderer.panelSettings = panelSettings;
            panelRenderer.RegisterUIReloadCallback(BuildEffect);

            frame = 0;
            settleUpdates = _initialSettleUpdates;
            EditorApplication.update += Update;
        }

        public void Cancel()
        {
            EditorApplication.update -= Update;
            Cleanup();
        }

        #region PrivateMethods
        private void BuildEffect(PanelRenderer panelRenderer, VisualElement root, int version)
        {
            if (version == builtVersion) { return; }
            builtVersion = version;
            effect = new VisualElement { style = { position = Position.Absolute, left = 0f, top = 0f, width = exportRequest.size.x, height = exportRequest.size.y } };
            effect.generateVisualContent += meshGenerationContext => ShaderQuad.Draw(meshGenerationContext, new Rect(0f, 0f, 1f, 1f), Color.white); // Tinted in the shader
            root.Add(effect);
            ApplyFrameTime();
        }

        private void ApplyFrameTime()
        {
            if (effect == null) { return; }
            effect.style.unityMaterial = new StyleMaterialDefinition(renderSettings.CreateMaterialDefinition((frame + 0.5f) / exportRequest.frameRate, retroToggles));
            effect.MarkDirtyRepaint();
        }

        private void Update()
        {
            // Waits a few editor updates per frame, so the panel has rendered the new time before it's read back
            if (effect == null || --settleUpdates > 0) { return; }

            Capture(Path.Combine(exportRequest.folder, $"frame_{frame + 1:0000}.png"));
            frame++;
            if (frame < exportRequest.frameCount)
            {
                ApplyFrameTime();
                settleUpdates = _settleUpdates;
                return;
            }

            EditorApplication.update -= Update;
            Debug.Log($"Exported {exportRequest.frameCount} battle effect frames ({exportRequest.size.x}x{exportRequest.size.y} @ {exportRequest.frameRate:0.#} fps) to {exportRequest.folder}");
            Cleanup();
            finished?.Invoke();
        }

        private void Capture(string path)
        {
            RenderTexture previousActive = RenderTexture.active;
            RenderTexture.active = target;
            readback.ReadPixels(new Rect(0f, 0f, exportRequest.size.x, exportRequest.size.y), 0, 0);
            RenderTexture.active = previousActive;
            if (exportRequest.isTransparent) { UnpremultiplyReadback(); }
            readback.Apply();
            File.WriteAllBytes(path, readback.EncodeToPNG());
        }

        private void UnpremultiplyReadback()
        {
            // UI Toolkit blends colour * alpha (in linear) into the clear target, so it holds premultiplied colour while PNGs expect straight alpha
            //  - target is sRGB (linear in project) -> stored colour is gamma-encoded -> decode, divide by alpha, re-encode
            Color[] pixels = readback.GetPixels();
            for (int i = 0; i < pixels.Length; i++)
            {
                Color pixel = pixels[i];
                if (pixel.a is <= 0f or >= 1f) { continue; }
                pixels[i] = new Color(Unpremultiply(pixel.r, pixel.a), Unpremultiply(pixel.g, pixel.a), Unpremultiply(pixel.b, pixel.a), pixel.a);
            }
            readback.SetPixels(pixels);
        }

        private static float Unpremultiply(float encodedChannel, float alpha) => Mathf.LinearToGammaSpace(Mathf.Min(Mathf.GammaToLinearSpace(encodedChannel) / alpha, 1f));

        private void Cleanup()
        {
            if (host != null) { Object.DestroyImmediate(host); }
            if (panelSettings != null) { Object.DestroyImmediate(panelSettings); }
            if (readback != null) { Object.DestroyImmediate(readback); }
            if (target != null)
            {
                target.Release();
                Object.DestroyImmediate(target);
            }
            host = null;
            panelSettings = null;
            readback = null;
            target = null;
            effect = null;
        }
        #endregion
    }
}
#endif

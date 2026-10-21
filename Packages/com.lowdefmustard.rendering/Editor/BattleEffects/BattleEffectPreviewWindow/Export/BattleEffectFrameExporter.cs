#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace LowDefMustard.Rendering.Editor
{
    public sealed class BattleEffectFrameExporter
    {
        // Notes:
        //  - Renders a recipe frame by frame through a BattleEffectOffscreenRenderer (i.e. via the game's colour path), writing frame_####.png
        //  - Read back from the render texture, never from the screen (OS screen reads crash the editor on macOS)
        //  - Frames are sampled mid-frame, so stepped time + reference frames land on the same frame index
        //  - Internal methods/properties are for test visibility

        // Const Tunables
        private const int _initialSettleUpdates = 10; // First render of a new panel takes longer
        private const int _settleUpdates = 3;

        // State
        private readonly BattleEffectRenderSettings renderSettings;
        private readonly BattleEffectRetroToggles retroToggles;
        private readonly BattleEffectExportRequest exportRequest;
        
        private BattleEffectOffscreenRenderer offscreenRenderer;
        private Texture2D readback;
        private int frame;
        private int settleUpdates;

        // Events
        public event Action finished;

        public BattleEffectFrameExporter(BattleEffectRenderSettings renderSettings, BattleEffectRetroToggles retroToggles, BattleEffectExportRequest exportRequest)
        {
            this.renderSettings = renderSettings;
            this.retroToggles = retroToggles;
            this.exportRequest = exportRequest;
            Vector2Int size = exportRequest.size;
            readback = new Texture2D(size.x, size.y, exportRequest.isTransparent ? TextureFormat.RGBA32 : TextureFormat.RGB24, false) { hideFlags = HideFlags.HideAndDontSave };
        }

        public void Start()
        {
            Directory.CreateDirectory(exportRequest.folder);
            foreach (string staleFrame in Directory.GetFiles(exportRequest.folder, "frame_*.png")) { File.Delete(staleFrame); }

            offscreenRenderer = new BattleEffectOffscreenRenderer(exportRequest.size, exportRequest.isTransparent ? Color.clear : renderSettings.backgroundColour);
            frame = 0;
            ApplyFrameTime();
            settleUpdates = _initialSettleUpdates;
            EditorApplication.update += Update;
        }

        public void Cancel()
        {
            EditorApplication.update -= Update;
            Cleanup();
        }

        #region PrivateMethods
        private void ApplyFrameTime()
        {
            offscreenRenderer?.SetMaterial(new StyleMaterialDefinition(renderSettings.CreateMaterialDefinition((frame + 0.5f) / exportRequest.frameRate, retroToggles)));
        }

        private void Update()
        {
            // Waits a few editor updates per frame, so the panel has rendered the new time before it's read back
            if (offscreenRenderer == null || !offscreenRenderer.IsBuilt() || --settleUpdates > 0) { return; }

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
            RenderTexture.active = offscreenRenderer.GetTexture();
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

        internal static float Unpremultiply(float encodedChannel, float alpha) => Mathf.LinearToGammaSpace(Mathf.Min(Mathf.GammaToLinearSpace(encodedChannel) / alpha, 1f));

        private void Cleanup()
        {
            offscreenRenderer?.Dispose();
            if (readback != null) { Object.DestroyImmediate(readback); }
            offscreenRenderer = null;
            readback = null;
        }
        #endregion
    }
}
#endif

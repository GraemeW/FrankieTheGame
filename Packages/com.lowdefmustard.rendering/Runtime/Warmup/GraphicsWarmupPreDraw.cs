using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace LowDefMustard.Rendering
{
    public sealed class GraphicsWarmupPreDraw : IDisposable
    {
        //  - UI Toolkit:  one PanelRenderer per panel settings w/ a probe element per entry
        //  - Sprites:  a 1px clear SpriteRenderer per entry

        // Const Tunables
        private const float _spriteCameraDistance = 1f;

        // State
        private readonly List<Object> createdObjects = new();
        private readonly Dictionary<PanelSettings, List<GraphicsWarmupEntry>> pendingPanelEntries = new();

        public GraphicsWarmupPreDraw(Transform parent, IEnumerable<GraphicsWarmupEntry> entries)
        {
            Camera camera = Camera.main;
            foreach (GraphicsWarmupEntry entry in entries)
            {
                if (entry == null || !entry.IsValid()) { continue; }
                if (entry.IsUIToolkit()) { AddPanelEntry(entry); }
                else if (camera != null) { CreateSprite(parent, camera, entry.GetMaterial()); }
                else { Debug.LogWarning($"[GraphicsWarmup] No main camera:  skipped pre-drawing {entry.GetMaterial().name}"); }
            }
            foreach (PanelSettings panelSettings in new List<PanelSettings>(pendingPanelEntries.Keys)) { CreatePanel(parent, panelSettings); }
        }

        #region PublicMethods
        public bool IsReady() => pendingPanelEntries.Count == 0; // i.e. every panel's probes are attached

        public void Dispose()
        {
            foreach (Object createdObject in createdObjects) { if (createdObject != null) { Object.Destroy(createdObject); } } // Do not LINQ, destroying objects as we iterate
            createdObjects.Clear();
            pendingPanelEntries.Clear();
        }
        #endregion

        #region PrivateMethods
        private void AddPanelEntry(GraphicsWarmupEntry entry)
        {
            PanelSettings panelSettings = entry.GetPanelSettings();
            if (!pendingPanelEntries.TryGetValue(panelSettings, out List<GraphicsWarmupEntry> panelEntries))
            {
                panelEntries = new List<GraphicsWarmupEntry>();
                pendingPanelEntries.Add(panelSettings, panelEntries);
            }
            panelEntries.Add(entry);
        }

        private void CreatePanel(Transform parent, PanelSettings panelSettings)
        {
            var host = new GameObject($"GraphicsWarmup ({panelSettings.name})");
            host.SetActive(false);
            host.transform.SetParent(parent, false);
            createdObjects.Add(host);

            var panelRenderer = host.AddComponent<PanelRenderer>();
            panelRenderer.panelSettings = panelSettings;
            panelRenderer.RegisterUIReloadCallback(HandleUIReload);
            host.SetActive(true);
            return;

            
            // Local Functions
            void HandleUIReload(PanelRenderer reloadedPanelRenderer, VisualElement rootElement, int version)
            {
                reloadedPanelRenderer.UnregisterUIReloadCallback(HandleUIReload);
                if (!pendingPanelEntries.Remove(panelSettings, out List<GraphicsWarmupEntry> panelEntries)) { return; }
                foreach (GraphicsWarmupEntry entry in panelEntries) { rootElement.Add(new GraphicsWarmupProbe(entry)); }
            }
        }

        private void CreateSprite(Transform parent, Camera camera, Material material)
        {
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
            texture.SetPixel(0, 0, Color.clear);
            texture.Apply(false, true);
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 100f);
            sprite.hideFlags = HideFlags.HideAndDontSave;

            var host = new GameObject($"GraphicsWarmup ({material.name})");
            host.transform.SetParent(parent, false);
            Transform cameraTransform = camera.transform;
            host.transform.SetPositionAndRotation(cameraTransform.position + cameraTransform.forward * (camera.nearClipPlane + _spriteCameraDistance), cameraTransform.rotation);
            var spriteRenderer = host.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = sprite;
            spriteRenderer.sharedMaterial = material;

            createdObjects.Add(host);
            createdObjects.Add(sprite);
            createdObjects.Add(texture);
        }
        #endregion
    }
}

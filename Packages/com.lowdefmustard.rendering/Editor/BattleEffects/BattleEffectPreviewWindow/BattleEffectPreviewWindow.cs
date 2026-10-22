#if UNITY_EDITOR
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace LowDefMustard.Rendering.Editor
{
    public sealed class BattleEffectPreviewWindow : EditorWindow
    {
        // Notes:
        //  - Time is driven from here, recipe edits show live
        //  - Optional reference frames (a folder of PNGs) play alongside / over the effect at their own frame rate
        //  - The preview + export both render offscreen through a runtime panel (BattleEffectOffscreenRenderer) to ensure accurate colour repro
        //  - Export renders every frame to Temp/BattleEffectPreview/<recipe>/
        //  - Internal methods/properties are for test visibility

        // Time Preview Settings
        private const int _tickMilliseconds = 16;
        private const float _endTailSeconds = 0.25f;
        private const float _defaultStepFrameRate = 30f;
        private const float _controlSpacing = 4f;
        private const int _previewSettleTicks = 3; // Repaints after a redraw while the offscreen renderer panel catches up
        
        // Export Settings
        private const string _exportRoot = "Temp/BattleEffectPreview";
        
        // Reference Settings
        [NoAutoStaticsCleanup] private static readonly float[] _referencePreviewScales = { 0.25f, 0.5f, 1f, 2f }; // Listed after the size presets

        // Configuration State ~ serialized to persist across reloads
        [SerializeField] private BattleEffectRenderSettings render = new();
        [SerializeField] private BattleEffectRetroToggles retro = new();
        [SerializeField] private BattleEffectPlaybackSettings playback = new();
        [SerializeField] private BattleEffectReferenceSettings reference = new();
        [SerializeField] private int sizePresetIndex = 0;
        [SerializeField] private bool isTransparentExport = false;

        // UI State
        private bool isPlaying = true;
        private float time;
        private double lastTickTime;
        private bool isRedrawPending = true;
        private BattleEffectRecipe drawnRecipe;
        private int drawnRecipeRevision = -1;
        
        private ObjectField recipeField;
        private Image preview;
        private BattleEffectOffscreenRenderer previewRenderer;
        private int previewRepaintTicks;
        private VisualElement previewFrame;
        private Button playButton;
        private Label timeLabel;
        private BattleEffectTimelineElement timeline;
        private BattleEffectFrameExporter exporter;
        
        private BattleEffectReferenceFrames referenceFrames;
        private Image overlayImage;
        private Image referenceImage;
        
        
        #region Menus
        [MenuItem("Tools/Battle Effects/Preview")]
        private static void Open()
        {
            BattleEffectPreviewWindow window = GetWindow<BattleEffectPreviewWindow>("Battle Effect Preview");
            if (Selection.activeObject is BattleEffectRecipe selectedRecipe) { window.SetRecipe(selectedRecipe); }
        }

        [OnOpenAsset(1)]
        private static bool OpenFromDoubleClick(EntityId entityId, int line)
        {
            if (EditorUtility.EntityIdToObject(entityId) is not BattleEffectRecipe openedRecipe) { return false; }
            GetWindow<BattleEffectPreviewWindow>("Battle Effect Preview").SetRecipe(openedRecipe);
            return true;
        }

        [MenuItem("CONTEXT/" + nameof(BattleEffectRecipe) + "/Open in Battle Effect Preview")]
        private static void OpenFromContext(MenuCommand menuCommand)
        {
            if (menuCommand.context is not BattleEffectRecipe contextRecipe) { return; }
            GetWindow<BattleEffectPreviewWindow>("Battle Effect Preview").SetRecipe(contextRecipe);
        }
        #endregion

        #region UnityMethods
        private void CreateGUI()
        {
            render.FindMaterialIfUnset();

            ScrollView root = new();
            rootVisualElement.Add(root);
            BuildEffectSection(root);
            BuildTransportRow(root);
            BuildPlaybackOptions(root);
            BuildReferenceSection(root);
            BuildTimelineAndStage(root);

            LoadReferenceFrames();
            ApplyLayout();
            lastTickTime = EditorApplication.timeSinceStartup;
            root.schedule.Execute(Tick).Every(_tickMilliseconds);
        }

        private void OnDisable()
        {
            exporter?.Cancel();
            exporter = null;
            previewRenderer?.Dispose();
            previewRenderer = null;
            referenceFrames?.Dispose();
            referenceFrames = null;
        }
        #endregion

        #region GUI
        private void BuildEffectSection(VisualElement root)
        {
            recipeField = new ObjectField("Recipe") { objectType = typeof(BattleEffectRecipe), value = render.recipe };
            recipeField.RegisterValueChangedCallback(changeEvent => SetRecipe(changeEvent.newValue as BattleEffectRecipe));
            root.Add(recipeField);

            var materialField = new ObjectField("Material") { objectType = typeof(Material), value = render.material, tooltip = "BattleEffect material" };
            materialField.RegisterValueChangedCallback(changeEvent =>
            {
                render.material = changeEvent.newValue as Material;
                RequestRedraw();
            });
            root.Add(materialField);

            var serializedWindow = new SerializedObject(this);
            var tintField = new PropertyField(serializedWindow.FindProperty(nameof(render)).FindPropertyRelative(BattleEffectRenderSettings.tintField), "Effect tint");
            tintField.Bind(serializedWindow);
            tintField.TrackSerializedObjectValue(serializedWindow, _ => RequestRedraw());
            root.Add(tintField);

            var backgroundField = new ColorField("Background") { value = render.backgroundColour };
            backgroundField.RegisterValueChangedCallback(changeEvent =>
            {
                render.backgroundColour = changeEvent.newValue;
                ApplyLayout();
            });
            root.Add(backgroundField);

            List<string> sizeNames = GetSizePresetNames();
            var sizeField = new DropdownField("Size", sizeNames, Mathf.Clamp(sizePresetIndex, 0, sizeNames.Count - 1));
            sizeField.RegisterValueChangedCallback(_ =>
            {
                sizePresetIndex = sizeField.index;
                ApplyLayout();
            });
            root.Add(sizeField);

            var fitField = new EnumField("Fit", render.fit) { tooltip = "Aspect fit:  effect units span the shorter side (e.g. over a slide). Stretch:  fills the whole preview (e.g. full screen)" };
            fitField.RegisterValueChangedCallback(changeEvent =>
            {
                render.fit = (BattleEffectFit)changeEvent.newValue;
                RequestRedraw();
            });
            root.Add(fitField);
        }

        private void BuildTransportRow(VisualElement root)
        {
            var transport = new VisualElement { style = { flexDirection = FlexDirection.Row, marginTop = _controlSpacing } };
            playButton = new Button(TogglePlaying);
            transport.Add(playButton);
            transport.Add(new Button(() => Seek(0f)) { text = "Restart" });
            transport.Add(new Button(() => Step(-1)) { text = "< Frame" });
            transport.Add(new Button(() => Step(1)) { text = "Frame >" });

            Toggle loopToggle = CreateToggle("Loop", playback.isLooping, null, value => playback.isLooping = value);
            loopToggle.style.marginLeft = _controlSpacing;
            loopToggle.labelElement.style.minWidth = StyleKeyword.Auto;
            loopToggle.labelElement.style.marginRight = _controlSpacing;
            transport.Add(loopToggle);

            transport.Add(new VisualElement { style = { flexGrow = 1f } });

            Toggle transparentToggle = CreateToggle("Transparent", isTransparentExport, "Export w/o the background (PNGs w/ alpha)", value => isTransparentExport = value);
            transparentToggle.labelElement.style.minWidth = StyleKeyword.Auto;
            transparentToggle.labelElement.style.marginRight = _controlSpacing;
            transparentToggle.style.marginRight = _controlSpacing;
            transport.Add(transparentToggle);
            transport.Add(new Button(StartExport) { text = "Export frames", tooltip = "Renders each frame offscreen to Temp/BattleEffectPreview/<recipe>/ (at the reference fps if loaded, else the step rate)" });
            root.Add(transport);
        }

        private void BuildPlaybackOptions(VisualElement root)
        {
            root.Add(CreateSlider("Speed", 0.1f, 2f, playback.speed, value => playback.speed = value));
            root.Add(CreateToggle("Stepped time", retro.isSteppedTime, "Off:  ignore the recipe's frame rate (smooth)", value => retro.isSteppedTime = value));
            root.Add(CreateToggle("Flicker", retro.isFlickerEnabled, "Off:  ignore every item's flicker", value => retro.isFlickerEnabled = value));
            root.Add(CreateToggle("Jitter", retro.isJitterEnabled, "Off:  ignore every item's jitter", value => retro.isJitterEnabled = value));
            timeLabel = new Label { style = { marginTop = _controlSpacing } };
            root.Add(timeLabel);
        }

        private void BuildReferenceSection(VisualElement root)
        {
            var referenceFolderRow = new VisualElement { style = { flexDirection = FlexDirection.Row, marginTop = _controlSpacing } };
            var referenceFolderField = new TextField("Reference frames") { value = reference.folder, isDelayed = true, style = { flexGrow = 1f } };
            referenceFolderField.RegisterValueChangedCallback(changeEvent => SetReferenceFolder(changeEvent.newValue));
            referenceFolderRow.Add(referenceFolderField);
            referenceFolderRow.Add(new Button(() =>
            {
                string folder = EditorUtility.OpenFolderPanel("Reference frames (PNGs)", reference.folder, "");
                if (!string.IsNullOrEmpty(folder)) { referenceFolderField.value = folder; }
            }) { text = "Browse" });
            root.Add(referenceFolderRow);

            var referenceRateField = new FloatField("Reference fps") { value = reference.frameRate };
            referenceRateField.RegisterValueChangedCallback(changeEvent =>
            {
                reference.frameRate = Mathf.Max(changeEvent.newValue, 0f);
                RequestRedraw();
            });
            root.Add(referenceRateField);

            var referenceModeField = new EnumField("Reference view", reference.view);
            referenceModeField.RegisterValueChangedCallback(changeEvent =>
            {
                reference.view = (BattleEffectReferenceView)changeEvent.newValue;
                ApplyLayout();
            });
            root.Add(referenceModeField);

            root.Add(CreateSlider("Overlay opacity", 0f, 1f, reference.overlayOpacity, value =>
            {
                reference.overlayOpacity = value;
                ApplyLayout();
            }));
        }

        private void BuildTimelineAndStage(VisualElement root)
        {
            timeline = new BattleEffectTimelineElement();
            timeline.scrubbed += scrubTime =>
            {
                isPlaying = false;
                Seek(scrubTime);
            };
            root.Add(timeline);

            // Preview (w/ the overlay reference on top) + the side-by-side reference
            var stage = new VisualElement { style = { flexDirection = FlexDirection.Row, marginTop = 8f } };
            previewFrame = new VisualElement();
            preview = new Image { scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore, style = { position = Position.Absolute, left = 0f, top = 0f, right = 0f, bottom = 0f } };
            overlayImage = new Image { scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore, style = { position = Position.Absolute, left = 0f, top = 0f, right = 0f, bottom = 0f } };
            previewFrame.Add(preview);
            previewFrame.Add(overlayImage);
            referenceImage = new Image { scaleMode = ScaleMode.ScaleToFit, style = { marginLeft = 8f } };
            stage.Add(previewFrame);
            stage.Add(referenceImage);
            root.Add(stage);
        }

        private Toggle CreateToggle(string label, bool value, string tooltip, System.Action<bool> onChanged)
        {
            var toggle = new Toggle(label) { value = value, tooltip = tooltip };
            toggle.RegisterValueChangedCallback(changeEvent =>
            {
                onChanged(changeEvent.newValue);
                RequestRedraw();
            });
            return toggle;
        }

        private Slider CreateSlider(string label, float low, float high, float value, System.Action<float> onChanged)
        {
            var slider = new Slider(label, low, high) { value = value, showInputField = true };
            slider.RegisterValueChangedCallback(changeEvent =>
            {
                onChanged(changeEvent.newValue);
                RequestRedraw();
            });
            return slider;
        }
        #endregion

        #region PrivateInternalMethods
        internal float GetTime() => time;
        internal bool IsPlaying() => isPlaying;
        internal bool IsExporting() => exporter != null;
        internal BattleEffectRenderSettings GetRenderSettings() => render;
        
        internal void SetRecipe(BattleEffectRecipe setRecipe)
        {
            render.recipe = setRecipe;
            recipeField?.SetValueWithoutNotify(setRecipe);
            Seek(0f);
        }

        internal void SetReferenceFolder(string folder)
        {
            reference.folder = folder;
            LoadReferenceFrames();
            ApplyLayout();
        }

        private void LoadReferenceFrames()
        {
            referenceFrames?.Dispose();
            referenceFrames = new BattleEffectReferenceFrames(reference.folder);
        }

        private void RequestRedraw() => isRedrawPending = true;

        private void ApplyLayout()
        {
            RequestRedraw();
            if (previewFrame == null) { return; }
            Vector2 size = GetPreviewSize();
            previewFrame.style.width = size.x;
            previewFrame.style.height = size.y;
            previewFrame.style.backgroundColor = render.backgroundColour;
            RefreshPreviewRenderer(size);
            referenceImage.style.width = size.x;
            referenceImage.style.height = size.y;
            referenceImage.style.backgroundColor = render.backgroundColour;

            bool hasReference = referenceFrames is { count: > 0 };
            referenceImage.style.display = hasReference && reference.view == BattleEffectReferenceView.SideBySide ? DisplayStyle.Flex : DisplayStyle.None;
            overlayImage.style.display = hasReference && reference.view == BattleEffectReferenceView.Overlay ? DisplayStyle.Flex : DisplayStyle.None;
            overlayImage.style.opacity = reference.overlayOpacity;
        }

        private void RefreshPreviewRenderer(Vector2 size)
        {
            var pixelSize = new Vector2Int(Mathf.Max(Mathf.RoundToInt(size.x), 1), Mathf.Max(Mathf.RoundToInt(size.y), 1));
            if (previewRenderer == null || previewRenderer.GetSize() != pixelSize)
            {
                previewRenderer?.Dispose();
                previewRenderer = new BattleEffectOffscreenRenderer(pixelSize, render.backgroundColour);
                preview.image = previewRenderer.GetTexture();
            }
            else { previewRenderer.SetClearColour(render.backgroundColour); }
        }

        internal RenderTexture GetPreviewTexture() => preview?.image as RenderTexture;

        internal Vector2 GetPreviewSize()
        {
            IReadOnlyList<BattleEffectPreviewSize> presets = BattleEffectPreviewSizes.GetPresets();
            
            int referenceScaleIndex = sizePresetIndex - presets.Count;
            bool usingReferenceSize = referenceScaleIndex >= 0 && referenceScaleIndex < _referencePreviewScales.Length;
            if (usingReferenceSize && referenceFrames != null && referenceFrames.TryGetSize(out Vector2 referenceSize)) { return referenceSize * _referencePreviewScales[referenceScaleIndex]; }
            
            return presets[Mathf.Clamp(sizePresetIndex, 0, presets.Count - 1)].size;
        }

        internal void StartExport()
        {
            if (!render.isRenderable || exporter != null) { return; }

            float exportRate = referenceFrames is { count: > 0 } && reference.frameRate > 0f ? reference.frameRate : GetStepRate();
            int frameCount = Mathf.Max(Mathf.CeilToInt((GetTotalTime() - _endTailSeconds) * exportRate), 1);
            Vector2 size = GetPreviewSize();
            var request = new BattleEffectExportRequest(new Vector2Int(Mathf.RoundToInt(size.x), Mathf.RoundToInt(size.y)), exportRate, frameCount, System.IO.Path.Combine(_exportRoot, render.recipe.name), isTransparentExport);
            exporter = new BattleEffectFrameExporter(render, retro, request);
            exporter.finished += () => exporter = null;
            exporter.Start();
        }

        internal float GetTotalTime()
        {
            float effectTime = render.recipe != null ? render.recipe.GetTotalEndTime() : 0f;
            float referenceTime = referenceFrames is { count: > 0 } && reference.frameRate > 0f ? referenceFrames.count / reference.frameRate : 0f;
            return Mathf.Max(effectTime, referenceTime) + _endTailSeconds;
        }

        internal float GetStepRate()
        {
            float recipeFrameRate = retro.GetFrameRate(render.recipe);
            if (recipeFrameRate > 0f) { return recipeFrameRate; }
            return referenceFrames is { count: > 0 } && reference.frameRate > 0f ? reference.frameRate : _defaultStepFrameRate;
        }

        internal void TogglePlaying()
        {
            isPlaying = !isPlaying;
            RequestRedraw();
            if (isPlaying && time >= GetTotalTime()) { time = 0f; }
        }

        internal void Step(int frames)
        {
            isPlaying = false;
            float stepRate = GetStepRate();
            Seek((Mathf.Round(time * stepRate) + frames) / stepRate);
        }

        internal void Seek(float seekTime)
        {
            time = Mathf.Clamp(seekTime, 0f, GetTotalTime());
            Redraw();
        }

        private void Tick()
        {
            double now = EditorApplication.timeSinceStartup;
            float deltaTime = (float)(now - lastTickTime);
            lastTickTime = now;

            if (isPlaying)
            {
                time += deltaTime * playback.speed;
                float totalTime = GetTotalTime();
                if (time > totalTime)
                {
                    if (playback.isLooping) { time = 0f; }
                    else
                    {
                        time = totalTime;
                        isPlaying = false;
                    }
                }
                RequestRedraw();
            }

            // Note:  paused + unchanged = nothing to draw (recipe edits are caught by its revision)
            bool isRecipeChanged = render.recipe != drawnRecipe || (render.recipe != null && render.recipe.GetRevision() != drawnRecipeRevision);
            if (isRedrawPending || isRecipeChanged) { Redraw(); }
            if (previewRepaintTicks > 0)
            {
                previewRepaintTicks--;
                preview.MarkDirtyRepaint();
            }
        }

        private void Redraw()
        {
            if (preview == null) { return; }
            isRedrawPending = false;
            drawnRecipe = render.recipe;
            drawnRecipeRevision = render.recipe != null ? render.recipe.GetRevision() : -1;
            playButton.text = isPlaying ? "Pause" : "Play";

            preview.style.display = render.isRenderable ? DisplayStyle.Flex : DisplayStyle.None;
            if (render.isRenderable) { previewRenderer?.SetMaterial(new StyleMaterialDefinition(render.CreateMaterialDefinition(time, retro))); }
            previewRepaintTicks = _previewSettleTicks;

            string referenceText = "";
            if (referenceFrames != null && referenceFrames.TryGetFrame(time, reference.frameRate, out Texture2D frame, out int frameIndex))
            {
                overlayImage.image = frame;
                referenceImage.image = frame;
                referenceText = $"   reference frame {frameIndex + 1} / {referenceFrames.count}";
            }

            string status = render.recipe == null ? "Assign a recipe" : render.material == null ? "Assign the BattleEffect material" : "";
            timeLabel.text = $"{time:F2}s / {GetTotalTime():F2}s   frame {Mathf.FloorToInt(time * GetStepRate()) + 1} @ {GetStepRate():0.#} fps{referenceText}   {status}";
            timeline.Refresh(render.recipe, time, GetTotalTime());
        }

        private static List<string> GetSizePresetNames()
        {
            var names = BattleEffectPreviewSizes.GetPresets().Select(preset => preset.name).ToList();
            names.AddRange(_referencePreviewScales.Select(scale => $"Reference frames x{scale.ToString("0.##", CultureInfo.InvariantCulture)}"));
            return names;
        }
        #endregion
    }
}
#endif

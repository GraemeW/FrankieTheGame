#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Frankie.Combat.UI;

namespace Frankie.Combat.Editor
{
    public sealed class BattleEffectPreviewWindow : EditorWindow
    {
        // Notes:
        //  - Time is driven from here, recipe edits show live
        //  - Optional reference frames (a folder of PNGs) play alongside / over the effect at their own frame rate
        //  - Export renders every frame offscreen to Temp/BattleEffectPreview/<recipe>/

        // Time Preview Settings
        private const int _tickMilliseconds = 16;
        private const float _endTailSeconds = 0.25f;
        private const float _defaultStepFrameRate = 30f;
        private const float _controlSpacing = 4f;
        
        // Export Settings
        private const string _exportRoot = "Temp/BattleEffectPreview";
        
        // Reference Settings
        private const float _referencePreviewScale = 2f;
        private const string _referenceSizeName = "Reference frames x2";
        private static readonly (string name, Vector2 size)[] _sizePresets =
        {
            // Note:  slide sizes from EnemySlide.uss (--enemy-slide-width / height) - keep in sync for accurate preview
            ("Standard slide (280x320)", new Vector2(280f, 320f)),
            ("Mook slide (175x200)", new Vector2(175f, 200f)),
            ("Boss slide (420x480)", new Vector2(420f, 480f)),
            ("Full screen (640x360)", new Vector2(640f, 360f))
        };

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
        private VisualElement preview;
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

            var sizeField = new DropdownField("Size", GetSizePresetNames(), Mathf.Clamp(sizePresetIndex, 0, _sizePresets.Length));
            sizeField.RegisterValueChangedCallback(_ =>
            {
                sizePresetIndex = sizeField.index;
                ApplyLayout();
            });
            root.Add(sizeField);
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
            preview = new VisualElement { pickingMode = PickingMode.Ignore, style = { position = Position.Absolute, left = 0f, top = 0f, right = 0f, bottom = 0f } };
            preview.generateVisualContent += meshGenerationContext => ShaderQuad.Draw(meshGenerationContext, new Rect(0f, 0f, 1f, 1f), Color.white); // Tinted in the shader
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

        #region PrivateMethods
        private void SetRecipe(BattleEffectRecipe setRecipe)
        {
            render.recipe = setRecipe;
            recipeField?.SetValueWithoutNotify(setRecipe);
            Seek(0f);
        }

        private void SetReferenceFolder(string folder)
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
            referenceImage.style.width = size.x;
            referenceImage.style.height = size.y;
            referenceImage.style.backgroundColor = render.backgroundColour;

            bool hasReference = referenceFrames is { count: > 0 };
            referenceImage.style.display = hasReference && reference.view == BattleEffectReferenceView.SideBySide ? DisplayStyle.Flex : DisplayStyle.None;
            overlayImage.style.display = hasReference && reference.view == BattleEffectReferenceView.Overlay ? DisplayStyle.Flex : DisplayStyle.None;
            overlayImage.style.opacity = reference.overlayOpacity;
        }

        private Vector2 GetPreviewSize()
        {
            if (sizePresetIndex == _sizePresets.Length && referenceFrames != null && referenceFrames.TryGetSize(out Vector2 referenceSize)) { return referenceSize * _referencePreviewScale; }
            return _sizePresets[Mathf.Clamp(sizePresetIndex, 0, _sizePresets.Length - 1)].size;
        }

        private void StartExport()
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

        private float GetTotalTime()
        {
            float effectTime = render.recipe != null ? render.recipe.GetEndTime() : 0f;
            float referenceTime = referenceFrames is { count: > 0 } && reference.frameRate > 0f ? referenceFrames.count / reference.frameRate : 0f;
            return Mathf.Max(effectTime, referenceTime) + _endTailSeconds;
        }

        private float GetStepRate()
        {
            float recipeFrameRate = retro.GetFrameRate(render.recipe);
            if (recipeFrameRate > 0f) { return recipeFrameRate; }
            return referenceFrames is { count: > 0 } && reference.frameRate > 0f ? reference.frameRate : _defaultStepFrameRate;
        }

        private void TogglePlaying()
        {
            isPlaying = !isPlaying;
            RequestRedraw();
            if (isPlaying && time >= GetTotalTime()) { time = 0f; }
        }

        private void Step(int frames)
        {
            isPlaying = false;
            float stepRate = GetStepRate();
            Seek((Mathf.Round(time * stepRate) + frames) / stepRate);
        }

        private void Seek(float seekTime)
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
        }

        private void Redraw()
        {
            if (preview == null) { return; }
            isRedrawPending = false;
            drawnRecipe = render.recipe;
            drawnRecipeRevision = render.recipe != null ? render.recipe.GetRevision() : -1;
            playButton.text = isPlaying ? "Pause" : "Play";

            if (render.isRenderable) { preview.style.unityMaterial = new StyleMaterialDefinition(render.CreateMaterialDefinition(time, retro)); }
            else { preview.style.unityMaterial = StyleKeyword.Null; }
            preview.MarkDirtyRepaint();

            string referenceText = "";
            if (referenceFrames != null && referenceFrames.TryGetFrame(time, reference.frameRate, out Texture2D frame, out int frameIndex))
            {
                overlayImage.image = frame;
                referenceImage.image = frame;
                referenceText = $"   reference frame {frameIndex + 1} / {referenceFrames.count}";
            }

            string status = render.recipe == null ? "Assign a recipe" : render.material == null ? "Assign the BattleEffect material" : "";
            timeLabel.text = $"{time:F2}s / {GetTotalTime():F2}s   frame {Mathf.FloorToInt(time * GetStepRate()) + 1} @ {GetStepRate():0.#} fps{referenceText}   {status}";
            timeline.Refresh(render.recipe, GetTotalTime(), time);
        }

        private static List<string> GetSizePresetNames()
        {
            var names = new List<string>();
            foreach ((string name, Vector2 _) in _sizePresets) { names.Add(name); }
            names.Add(_referenceSizeName);
            return names;
        }
        #endregion
    }
}
#endif

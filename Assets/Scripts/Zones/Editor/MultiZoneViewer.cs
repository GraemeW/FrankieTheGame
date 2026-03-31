using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Profile;
using UnityEditor.UIElements;
using UnityEngine.Tilemaps;

namespace Frankie.ZoneManagement.UIEditor
{
    public class SceneSnapshotViewer : EditorWindow
    {
        // UI Tunables
        private const int _zoneViewWidth = 260;
        private const int _zoneViewHeight = 200;
        private const int _zoneViewHeaderHeight = 24;
        private const int _snapshotWidth = 512;
        private const int _snapshotHeight = 288;
        private const float _zoneViewPadding  = 20f;
 
        // Path Tunables
        private const string _assetsFolder = "Assets";
        private const string _multiZoneViewSubFolder = "MultiZoneViewer";
        private static readonly string _multiZoneViewAssetsDirectory = Path.Combine(_assetsFolder, _multiZoneViewSubFolder);
        private static readonly string _snapshotPNGDirectory = Path.Combine(Directory.GetCurrentDirectory(), _multiZoneViewSubFolder);

        // State & Editable Configurations
        [SerializeField] private bool keepExistingPositions = true;
        [SerializeField] private MultiZoneView activeMultiZoneView; 
        private readonly List<ZoneView> zoneViews = new();
        
        // UI State
        private VisualElement canvas;
        private VisualElement zoneViewLayer;
        private ObjectField multiZoneViewField;
        private Label statusLabel;
        private Button clearButton;
        private Vector2 panOffset = Vector2.zero;
        
        #region UnityMethods
        [MenuItem("Tools/Multi-Zone Viewer")]
        public static void Open()
        {
            var win = GetWindow<SceneSnapshotViewer>("Multi-Zone Viewer");
            win.minSize = new Vector2(600, 400);
            win.Show();
        }

        private void OnEnable()
        {
            SubscribeCanvasToDrawGrid(true);
            if (activeMultiZoneView != null)
            {
                TryLoadSnapshots();
            }
        }

        private void OnDisable()
        {
            SubscribeCanvasToDrawGrid(false);
            DisposeRuntimeTextures();
        }
        
        public void CreateGUI()
        {
            VisualElement root = rootVisualElement;
            root.style.flexDirection = FlexDirection.Column;
            root.style.flexGrow = 1;

            BuildToolbar(root);
            BuildCanvas(root);
            AddAllZoneViews();
            RefreshToolbarState();
        }
        #endregion
        
        #region Toolbar
        private void BuildToolbar(VisualElement root)
        {
            VisualElement toolbar = MakeEmptyToolbar();
            root.Add(toolbar);
            
            Label fieldLabel = MakeToolbarLabel("Snapshot:");
            toolbar.Add(fieldLabel);

            multiZoneViewField = MakeMultiZoneViewField(activeMultiZoneView);
            multiZoneViewField.RegisterValueChangedCallback(OnSnapshotFieldChanged);
            toolbar.Add(multiZoneViewField);

            var captureButton = new Button(OnCaptureClicked) { text = "Capture Zones" };
            StyleButton(captureButton);
            toolbar.Add(captureButton);
            
            var refreshButton = new Button(OnRefreshClicked) { text = "Refresh" };
            StyleButton(refreshButton);
            toolbar.Add(refreshButton);
            
            clearButton = new Button(OnClearClicked) { text = "Clear" };
            StyleButton(clearButton);
            toolbar.Add(clearButton);
            
            Toggle keepPositionToggle = MakeToggle("Keep positions", keepExistingPositions);
            keepPositionToggle.RegisterValueChangedCallback(changeEvent => keepExistingPositions = changeEvent.newValue);
            toolbar.Add(keepPositionToggle);

            VisualElement spacer = MakeSpacer();
            toolbar.Add(spacer);

            statusLabel = MakeToolbarLabel("");
            toolbar.Add(statusLabel);
        }

        private void RefreshToolbarState()
        {
            bool hasZoneViews = zoneViews.Count > 0;
            if (clearButton != null) { clearButton.SetEnabled(hasZoneViews); }
            if (statusLabel != null)
            {
                statusLabel.text = hasZoneViews ? $"{zoneViews.Count} scene(s)  —  middle-click or alt+drag to pan" : string.Empty;
            }
        }
        
        private void OnSnapshotFieldChanged(ChangeEvent<Object> evt)
        {
            var selected = evt.newValue as MultiZoneView;
            SetActiveMultiZoneView(selected);
        }
        
        private void SetActiveMultiZoneView(MultiZoneView multiZoneView)
        {
            ClearRenderedZoneViews();

            activeMultiZoneView = multiZoneView;
            if (activeMultiZoneView != null) { TryLoadSnapshots(); }
            
            AddAllZoneViews();
            canvas?.MarkDirtyRepaint();
            RefreshToolbarState();
        }

        private void OnCaptureClicked()
        {
            if (activeMultiZoneView == null)
            {
                activeMultiZoneView = CreateMultiZoneViewAsset();
                if (activeMultiZoneView == null) { return; }
                multiZoneViewField?.SetValueWithoutNotify(activeMultiZoneView);
            }
            CaptureAllZones();
            
            RefreshZoneViews();
            RefreshToolbarState();
        }

        private void OnClearClicked()
        {
            activeMultiZoneView = null;
            multiZoneViewField?.SetValueWithoutNotify(null);
            ClearRenderedZoneViews();
            canvas?.MarkDirtyRepaint();
            RefreshToolbarState();
        }

        private void OnRefreshClicked()
        {
            RefreshZoneViews();
            RefreshToolbarState();
        }
        #endregion

        #region Canvas
        private void BuildCanvas(VisualElement root)
        {
            canvas = MakeEmptyCanvas();
            SubscribeCanvasToDrawGrid(true);
            canvas.AddManipulator(new PanManipulator(OnCanvasPanned));
            root.Add(canvas);

            zoneViewLayer = MakeEmptyZoneViewLayer();
            canvas.Add(zoneViewLayer);
        }
        
        private void SubscribeCanvasToDrawGrid(bool enable)
        {
            if (canvas == null) { return; }
            
            canvas.generateVisualContent -= DrawGrid;
            if (enable) { canvas.generateVisualContent += DrawGrid; }
        }
        
        private void OnCanvasPanned(Vector2 delta)
        {
            panOffset += delta;
            zoneViewLayer.style.left = panOffset.x;
            zoneViewLayer.style.top = panOffset.y;
            canvas.MarkDirtyRepaint();
        }
        #endregion
        
        #region ZoneViews

        private void RefreshZoneViews()
        {
            ClearRenderedZoneViews();
            TryLoadSnapshots();
            AddAllZoneViews();
            canvas?.MarkDirtyRepaint();
        }
        
        private void AddAllZoneViews()
        {
            foreach (ZoneView zoneView in zoneViews)
            {
                AddZoneViewElement(zoneView);
            }
        }
        
        private void AddZoneViewElement(ZoneView zoneView)
        {
            ZoneViewData zoneViewData = zoneView.data;
            if (zoneViewData == null) { return; }
            
            VisualElement zoneViewElement = MakeEmptyZoneViewElement(zoneViewData.zoneName, zoneViewData.topLeftPosition.x, zoneViewData.topLeftPosition.y);
            Label zoneViewElementHeader = MakeZoneViewElementHeader(zoneViewData.zoneName);
            zoneViewElementHeader.AddManipulator(new DragManipulator(zoneView, zoneViewElement, null));
            zoneViewElement.Add(zoneViewElementHeader);
            
            VisualElement imageArea = AddImageToZoneViewElement(zoneView, zoneViewElement);
            imageArea.AddManipulator(new DragManipulator(zoneView, zoneViewElement, () => TryLoadScene(zoneView)));
            
            zoneViewLayer.Add(zoneViewElement);
        }

        private static VisualElement AddImageToZoneViewElement(ZoneView zoneView, VisualElement zoneViewElement)
        {
            // Returns a reference to the imageArea on the zoneViewElement
            VisualElement imageArea;
            
            if (zoneView != null && zoneView.texture2D != null)
            {
                Image zoneSnapshot = MakeImage(zoneView.texture2D);
                zoneViewElement.Add(zoneSnapshot);
                imageArea = zoneSnapshot;
            }
            else
            {
                Label noSnapshotLabel = MakeImageLabel("No snapshot");
                zoneViewElement.Add(noSnapshotLabel);
                imageArea = noSnapshotLabel;
            }
            AddHoverOverStyle(imageArea);

            return imageArea;
        }

        private void TryLoadScene(ZoneView zoneView)
        {
            string scenePath = zoneView?.data?.scenePath;
            if (scenePath == null) { return; }
            
            if (string.IsNullOrEmpty(scenePath) || !File.Exists(scenePath) || !scenePath.EndsWith(".unity")) { return; }

            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                RefreshZoneViews();
            }
        }
        #endregion
        
        #region Capture
        private void CaptureAllZones()
        {
            if (activeMultiZoneView == null) { return; }
            activeMultiZoneView.CleanDanglingZoneViewData();
            
            List<string> scenePaths = GetBuildProfileScenePaths();
            if (scenePaths == null || scenePaths.Count == 0)
            {
                EditorUtility.DisplayDialog("Scene Snapshot Viewer",
                    "No scenes found in the active build profile / build settings.", "OK");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) { return; }

            string originalScene = SceneManager.GetActiveScene().path;
            Directory.CreateDirectory(_snapshotPNGDirectory);
            EnsureAssetFolder();
            
            try
            {
                for (int i = 0; i < scenePaths.Count; i++)
                {
                    string path = scenePaths[i];
                    EditorUtility.DisplayProgressBar("Scene Snapshot Viewer",
                        $"Processing: {Path.GetFileNameWithoutExtension(path)}  ({i + 1}/{scenePaths.Count})",
                        (float)i / scenePaths.Count);
                    
                    Vector2 defaultPosition = new Vector2(
                        _zoneViewPadding + (i % 4) * (_zoneViewWidth + _zoneViewPadding),
                        _zoneViewPadding + (i / 4) * (_zoneViewHeight + _zoneViewHeaderHeight + _zoneViewPadding));
                    
                    CaptureZone(path, defaultPosition);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                if (!string.IsNullOrEmpty(originalScene) && File.Exists(originalScene))
                {
                    EditorSceneManager.OpenScene(originalScene, OpenSceneMode.Single);
                }
                else
                {
                    EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
                }
            }
            
            EditorUtility.SetDirty(activeMultiZoneView);
            AssetDatabase.SaveAssetIfDirty(activeMultiZoneView);
        }
        
        private void CaptureZone(string scenePath, Vector2 defaultPosition)
        {
            if (activeMultiZoneView == null) { return; }
            
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            Camera captureCamera = Camera.main;
            if (captureCamera == null) { return; }
            
            PositionCameraToFrameScene(captureCamera, scene);
            Texture2D snapshotTexture = CameraClick(captureCamera);

            string zoneName = GetSafeNameFromPath(scenePath);
            string snapshotPNGPath = GetSnapshotPathForScene(zoneName);
            File.WriteAllBytes(snapshotPNGPath, snapshotTexture.EncodeToPNG());

            activeMultiZoneView.CreateOrUpdateZoneViewData(zoneName, scenePath, snapshotPNGPath, defaultPosition, keepExistingPositions);
        }

        private static void PositionCameraToFrameScene(Camera camera, Scene scene)
        {
            List<Tilemap> tilemaps = FindObjectsByType<Tilemap>().ToList();
            Debug.Log($"On Scene: {scene.name}");
            
            Bounds maxBounds = new Bounds();
            if (tilemaps.Count == 0)
            {
                List<Renderer> renderers = FindObjectsByType<Renderer>().ToList();
                Debug.Log($"Using standard renderers to calculate bounds with {renderers.Count} renderers");
                
                bool maxBoundsSet = false;
                foreach (Renderer renderer in renderers)
                {
                    if (!maxBoundsSet) { maxBounds = renderer.bounds; maxBoundsSet = true; }
                    else { maxBounds.Encapsulate(renderer.bounds); }
                }
            }
            else
            {
                Debug.Log($"Using tilemap renderers to calculate bounds with {tilemaps.Count} tilemaps");
                bool maxBoundsSet = false;
                foreach (Tilemap tilemap in tilemaps)
                {
                    tilemap.CompressBounds();

                    BoundsInt cellBounds = tilemap.cellBounds;
                    Vector2 minPosition = tilemap.CellToWorld(cellBounds.min);
                    Vector2 maxPosition = tilemap.CellToWorld(cellBounds.max);

                    if (!maxBoundsSet)
                    {
                        maxBounds.SetMinMax(minPosition, maxPosition); 
                        maxBoundsSet = true;
                    }
                    else
                    {
                        Bounds newBounds = new Bounds();
                        newBounds.SetMinMax(minPosition, maxPosition);
                        maxBounds.Encapsulate(newBounds);
                    }
                }
            }
            
            camera.transform.position = new Vector3(maxBounds.center.x, maxBounds.center.y, camera.transform.position.z);
            camera.orthographicSize = Mathf.Max(maxBounds.extents.x, maxBounds.extents.y) / 1.5f;
        }
        
        private static Texture2D CameraClick(Camera captureCamera)
        {
            var renderTexture = new RenderTexture(_snapshotWidth, _snapshotHeight, 24, RenderTextureFormat.ARGB32);
            captureCamera.targetTexture = renderTexture;
            captureCamera.Render();

            RenderTexture.active = renderTexture;
            var snapshotTexture = new Texture2D(_snapshotWidth, _snapshotHeight, TextureFormat.RGBA32, false);
            snapshotTexture.ReadPixels(new Rect(0, 0, _snapshotWidth, _snapshotHeight), 0, 0);
            snapshotTexture.Apply();

            RenderTexture.active = null;
            captureCamera.targetTexture = null;
            renderTexture.Release();
            DestroyImmediate(renderTexture);
            return snapshotTexture;
        }
        
        private void TryLoadSnapshots()
        {
            zoneViews.Clear();
            if (activeMultiZoneView == null) { return; }

            foreach (ZoneViewData zoneViewData in activeMultiZoneView.zoneViewDataSet)
            {
                if (zoneViewData == null) { continue; }
                if (string.IsNullOrEmpty(zoneViewData.snapshotPath) || !File.Exists(zoneViewData.snapshotPath)) { continue; }
                
                Texture2D texture2D = new Texture2D(2, 2,  TextureFormat.RGBA32, false);
                if (!texture2D.LoadImage(File.ReadAllBytes(zoneViewData.snapshotPath)))
                {
                    DestroyImmediate(texture2D);
                    continue;
                }
                
                zoneViews.Add(new ZoneView(zoneViewData, texture2D));
            }
        }

        private void DisposeRuntimeTextures()
        {
            foreach (ZoneView zoneView in zoneViews.Where(zoneView => zoneView.texture2D != null))
            {
                DestroyImmediate(zoneView.texture2D);
            }
        }
        #endregion
        
        #region AssetManagement
        private static MultiZoneView CreateMultiZoneViewAsset()
        {
            EnsureAssetFolder();

            string path = EditorUtility.SaveFilePanelInProject(
                "Save MultiZone View",
                "MultiZoneView",
                "asset",
                "Choose where to save the MultiZone View asset.",
                _multiZoneViewAssetsDirectory);

            if (string.IsNullOrEmpty(path)) { return null; }

            var asset = CreateInstance<MultiZoneView>();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            return asset;
        }
        #endregion
        
        #region PathHandling
        private static List<string> GetBuildProfileScenePaths()
        {
            var paths = new List<string>();
            
            var profile = BuildProfile.GetActiveBuildProfile();
            if (profile == null || profile.scenes == null) { return paths; }

            paths.AddRange(from scene in profile.scenes where scene.enabled select scene.path);
            return paths;
        }

        private static string GetSafeNameFromPath(string path)
        {
            string safeName = Path.GetFileNameWithoutExtension(path);
            return Path.GetInvalidFileNameChars().Aggregate(safeName, (current, c) => current.Replace(c, '_'));
        }
        
        private static string GetSnapshotPathForScene(string sceneName)
        {
            return Path.Combine(_snapshotPNGDirectory, $"Snapshot_{sceneName}.png");
        }
        
        private static void EnsureAssetFolder()
        {
            if (!AssetDatabase.IsValidFolder(_multiZoneViewAssetsDirectory))
            {
                AssetDatabase.CreateFolder(_assetsFolder, _multiZoneViewSubFolder);
            }
        }
        #endregion
        
        #region UIHelpers
        private void ClearRenderedZoneViews()
        {
            DisposeRuntimeTextures();
            zoneViews.Clear();
            zoneViewLayer?.Clear();
            panOffset = Vector2.zero;
            ApplyPanOffset();
        }
        
        private void ApplyPanOffset()
        {
            if (zoneViews == null) { return; }
            zoneViewLayer.style.left = panOffset.x;
            zoneViewLayer.style.top  = panOffset.y;
        }
        
        private static VisualElement MakeEmptyCanvas()
        {
            return new VisualElement
            {
                style =
                {
                    flexGrow = 1,
                    overflow = Overflow.Hidden,
                    backgroundColor = new StyleColor(new Color(0.18f, 0.18f, 0.18f))
                }
            };
        }
        
        private void DrawGrid(MeshGenerationContext ctx)
        {
            Painter2D painter = ctx.painter2D;
            Rect area = canvas.contentRect;
            DrawGridLines(painter, area, 30f,  new Color(1f, 1f, 1f, 0.05f));
            DrawGridLines(painter, area, 150f, new Color(1f, 1f, 1f, 0.10f));
        }

        private void DrawGridLines(Painter2D painter, Rect area, float spacing, Color color)
        {
            painter.strokeColor = color;
            painter.lineWidth = 1f;

            float xOffset = panOffset.x % spacing;
            float yOffset = panOffset.y % spacing;

            for (float x = -xOffset; x < area.width; x += spacing)
            {
                painter.BeginPath();
                painter.MoveTo(new Vector2(x, 0));
                painter.LineTo(new Vector2(x, area.height));
                painter.Stroke();
            }

            for (float y = -yOffset; y < area.height; y += spacing)
            {
                painter.BeginPath();
                painter.MoveTo(new Vector2(0, y));
                painter.LineTo(new Vector2(area.width, y));
                painter.Stroke();
            }
        }

        private static VisualElement MakeEmptyZoneViewLayer()
        {
            return new VisualElement
            {
                style =
                {
                    position = Position.Absolute,
                    left = 0,
                    top = 0,
                    width = 1,
                    height = 1
                }
            };
        }
        
        private static VisualElement MakeEmptyToolbar()
        {
            return new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    height = 22,
                    paddingLeft = 4,
                    paddingRight = 4,
                    backgroundColor = new StyleColor(new Color(0.22f, 0.22f, 0.22f)),
                    borderBottomWidth = 1,
                    borderBottomColor = new StyleColor(new Color(0.1f, 0.1f, 0.1f))
                }
            };
        }

        private static ObjectField MakeMultiZoneViewField(MultiZoneView multiZoneView)
        {
            return new ObjectField
            {
                objectType = typeof(MultiZoneView),
                allowSceneObjects = false,
                value = multiZoneView,
                style =
                {
                    width = 220,
                    marginRight = 6,
                }
            };
        }
        
        private static void StyleButton(Button button)
        {
            button.style.height = 18;
            button.style.paddingLeft = 8;
            button.style.paddingRight = 8;
            button.style.marginRight = 2;
            button.style.fontSize = 11;
            button.style.backgroundColor = new StyleColor(new Color(0.3f, 0.3f, 0.3f));
            button.style.color = new StyleColor(Color.white);
            button.style.borderTopLeftRadius = 3;
            button.style.borderTopRightRadius = 3;
            button.style.borderBottomLeftRadius = 3;
            button.style.borderBottomRightRadius = 3;
            button.style.borderTopWidth = 1;
            button.style.borderBottomWidth = 1;
            button.style.borderLeftWidth = 1;
            button.style.borderRightWidth = 1;
            button.style.borderTopColor = new StyleColor(new Color(0.15f, 0.15f, 0.15f));
            button.style.borderBottomColor = new StyleColor(new Color(0.15f, 0.15f, 0.15f));
            button.style.borderLeftColor = new StyleColor(new Color(0.15f, 0.15f, 0.15f));
            button.style.borderRightColor = new StyleColor(new Color(0.15f, 0.15f, 0.15f));
        }

        private static Toggle MakeToggle(string label, bool state)
        {
            return new Toggle
            {
                label = label,
                value = state,
                style =
                {
                    marginLeft = 8,
                    marginRight = 4,
                    fontSize = 11,
                    color = new StyleColor(new Color(0.7f, 0.7f, 0.7f)),
                    unityTextAlign = TextAnchor.MiddleRight,
                }
            };
        }

        private static VisualElement MakeSpacer()
        {
            return new VisualElement { style = { flexGrow = 1 } };
        }
        
        private static Label MakeToolbarLabel(string labelText)
        {
            return new Label
            {
                text = labelText,
                style =
                {
                    color = new StyleColor(new Color(0.6f, 0.6f, 0.6f)),
                    fontSize = 11,
                    unityTextAlign = TextAnchor.MiddleRight,
                    marginRight = 4
                }
            };
        }

        private static VisualElement MakeEmptyZoneViewElement(string zoneName, float xPosition, float yPosition)
        {
            var zoneViewElement = new VisualElement
            {
                style =
                {
                    position = Position.Absolute,
                    left = xPosition,
                    top = yPosition,
                    width = _zoneViewWidth,
                    height = _zoneViewHeaderHeight + _zoneViewHeight,
                    backgroundColor = new StyleColor(new Color(0.25f, 0.25f, 0.27f)),
                    borderTopLeftRadius = 4,
                    borderTopRightRadius = 4,
                    borderBottomLeftRadius = 4,
                    borderBottomRightRadius = 4,
                    borderTopWidth = 1,
                    borderBottomWidth = 1,
                    borderLeftWidth = 1,
                    borderRightWidth = 1,
                    borderTopColor = new StyleColor(new Color(0.5f, 0.5f, 0.5f, 0.5f)),
                    borderBottomColor = new StyleColor(new Color(0.5f, 0.5f, 0.5f, 0.5f)),
                    borderLeftColor = new StyleColor(new Color(0.5f, 0.5f, 0.5f, 0.5f)),
                    borderRightColor = new StyleColor(new Color(0.5f, 0.5f, 0.5f, 0.5f)),
                    overflow = Overflow.Hidden
                }
            };
            
            return zoneViewElement;
        }

        private static Label MakeZoneViewElementHeader(string zoneName)
        {
            return new Label(zoneName)
            {
                style =
                {
                    height = _zoneViewHeaderHeight,
                    backgroundColor = new StyleColor(new Color(0.13f, 0.45f, 0.72f)),
                    unityTextAlign = TextAnchor.MiddleCenter,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    fontSize = 11,
                    color = new StyleColor(Color.white),
                    paddingLeft = 4,
                    paddingRight = 4,
                    overflow = Overflow.Hidden
                }
            };
        }

        private static Image MakeImage(Texture2D texture2D)
        {
            return new Image
            {
                image = texture2D, scaleMode = ScaleMode.ScaleToFit,
                style =
                {
                    flexGrow = 1,
                    backgroundColor = new StyleColor(new Color(0.12f, 0.12f, 0.12f))
                }
            };
        }

        private static Label MakeImageLabel(string labelText)
        {
            return new Label(labelText)
            {
                style =
                {
                    flexGrow = 1,
                    unityTextAlign = TextAnchor.MiddleCenter,
                    color = new StyleColor(new Color(0.5f, 0.5f, 0.5f)),
                    backgroundColor = new StyleColor(new Color(0.12f, 0.12f, 0.12f))
                }
            };
        }

        private static void AddHoverOverStyle(VisualElement visualElement)
        {
            var normalBg = new Color(0.12f, 0.12f, 0.12f);
            var hoverBg  = new Color(0.20f, 0.30f, 0.38f);   // subtle blue tint
            visualElement.RegisterCallback<MouseEnterEvent>(_ =>
                visualElement.style.backgroundColor = new StyleColor(hoverBg));
            visualElement.RegisterCallback<MouseLeaveEvent>(_ =>
                visualElement.style.backgroundColor = new StyleColor(normalBg));
        }
        #endregion
    }
}

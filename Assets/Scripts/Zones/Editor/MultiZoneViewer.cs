using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;
using UnityEditor;
using UnityEditor.Build.Profile;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;

namespace Frankie.ZoneManagement.UIEditor
{
    public class MultiZoneViewer : EditorWindow
    {
        // Path Tunables
        private const string _assetsFolder = "Assets";
        private const string _multiZoneViewSubFolder = "MultiZoneViewer";
        private static readonly string _multiZoneViewAssetsDirectory = Path.Combine(_assetsFolder, _multiZoneViewSubFolder);
        private static readonly string _snapshotPNGDirectory = Path.Combine(Directory.GetCurrentDirectory(), _multiZoneViewSubFolder);
        
        // UI Tunables
        private static readonly Vector2 _defaultZoneViewDimensions = new(156, 120);
        private const int _zoneViewHeaderHeight = 24;
        private static readonly Vector2 _dummySnapshotDimensions = new(10, 10);
        private static readonly Vector2 _targetMinSnapshotDimensions = new(1920, 1080);
        private static readonly Vector2 _targetMaxSnapshotDimensions = new(7680, 4320);
        private const float _zoneViewPadding  = 20f;
        private const int _defaultNumberViewsPerRow = 4;
 
        // UI Styles
        private static readonly StyleColor _uiCanvasBackgroundColour = new(new Color(0.18f, 0.18f, 0.18f));
        private static readonly Color _uiGridLineMinorColour = new(1f, 1f, 1f, 0.05f);
        private static readonly Color _uiGridLineMajorColour = new(1f, 1f, 1f, 0.10f);
        private static readonly StyleColor _uiStandardBackgroundColour = new(new Color(0.22f, 0.22f, 0.22f));
        private static readonly StyleColor _uiViewBackgroundColour = new(new Color(0.25f, 0.25f, 0.27f));
        private static readonly StyleColor _uiViewHeaderColour = new(new Color(0.13f, 0.45f, 0.72f));
        private static readonly StyleColor _uiImageBackgroundColour = new(new Color(0.12f, 0.12f, 0.12f));
        private static readonly StyleColor _uiImageHoverBackgroundColour = new(new Color(0.20f, 0.30f, 0.38f));
        private static readonly StyleColor _uiBorderDarkColour = new(new Color(0.125f, 0.125f, 0.125f));
        private static readonly StyleColor _uiBorderBrightColour = new(new Color(0.5f, 0.5f, 0.5f, 0.5f));
        private static readonly StyleColor _uiButtonColour = new(new Color(0.3f, 0.3f, 0.3f));
        private static readonly StyleColor _uiLabelTextColour = new(new Color(0.6f, 0.6f, 0.6f));
        private static readonly float _uiStandardFontSize = 11f;
        private static readonly float _uiBezierLineWidth = 0.8f;
        private static readonly Color _uiBezierLineColour = new(1.0f, 0f, 0f, 0.8f); 

        // Editable Configurations
        [SerializeField] private MultiZoneView activeMultiZoneView;
        [SerializeField] private Vector2 panOffset = Vector2.zero;
        [SerializeField] private bool useZoneHandlerCrawl = true;
        [SerializeField] private Zone rootZone;
        [SerializeField] private bool drawConnections = true;
        [SerializeField] private bool keepExistingPositions = true;
        [SerializeField] private bool keepExistingDimensions = true;
        [SerializeField] private float worldToSnapshotScalingFactor = 80.0f;
        [SerializeField] private float snapshotToZoneViewScalingFactor = 0.15f;
        [SerializeField] private float additionalMaxScalingFactor = 5.0f;
        
        // State
        private bool isToolAvailable = true;
        private readonly List<ZoneView> zoneViews = new();
        private readonly Dictionary<string, ZoneView> zoneViewLookup = new();
        
        // UI State
        private VisualElement canvas;
        private VisualElement zoneViewLayer;
        private VisualElement curvesLayer;
        private ObjectField multiZoneViewField;
        private ObjectField startingZoneField;
        private Label statusLabel;
        private Button clearButton;
        
        #region UnityMethods
        [MenuItem("Tools/Multi-Zone Viewer")]
        public static void Open()
        {
            var win = GetWindow<MultiZoneViewer>("Multi-Zone Viewer");
            win.minSize = new Vector2(600, 400);
            win.Show();
        }

        private void OnEnable()
        {
            SubscribeCanvasToDrawGrid(true);
            SubscribeCurvesLayerToDrawCurves(true);
            SubscribeToOnSceneLoaded(true);
            SubscribeToPlayModeStateChanges(true);
        }

        private void OnDisable()
        {
            SubscribeCanvasToDrawGrid(false);
            SubscribeCurvesLayerToDrawCurves(false);
            SubscribeToOnSceneLoaded(false);
            SubscribeToPlayModeStateChanges(false);
            DisposeRuntimeTextures();
        }
        
        public void CreateGUI()
        {
            VisualElement root = rootVisualElement;
            root.style.flexDirection = FlexDirection.Column;
            root.style.flexGrow = 1;

            if (activeMultiZoneView != null) { TryLoadSnapshots(); }
            BuildToolbar(root);
            BuildCanvas(root);
            BuildParametersPanel(canvas);
            AddAllZoneViews();
            RefreshToolbarState();
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            switch (state)
            {
                case PlayModeStateChange.EnteredEditMode:
                    isToolAvailable = true;
                    OnRefreshClicked(false);
                    break;
                case PlayModeStateChange.EnteredPlayMode:
                    OnRefreshClicked(false);
                    isToolAvailable = false;
                    break;
            }
        }

        private void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            OnRefreshClicked(false);
        }
        
        private void SubscribeCanvasToDrawGrid(bool enable)
        {
            if (canvas == null) { return; }
            
            canvas.generateVisualContent -= DrawGrid;
            if (enable) { canvas.generateVisualContent += DrawGrid; }
        }

        private void SubscribeCurvesLayerToDrawCurves(bool enable)
        {
            if (curvesLayer == null) { return; }

            curvesLayer.generateVisualContent -= DrawCurves;
            if (enable) { curvesLayer.generateVisualContent += DrawCurves; }
        }

        private void SubscribeToPlayModeStateChanges(bool enable)
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            if (enable) { EditorApplication.playModeStateChanged += OnPlayModeStateChanged; }
        }

        private void SubscribeToOnSceneLoaded(bool enable)
        {
            EditorSceneManager.sceneOpened += OnSceneOpened;
            if (enable) { EditorSceneManager.sceneOpened  += OnSceneOpened; }
        }
        #endregion
        
        #region Toolbar
        private void BuildToolbar(VisualElement root)
        {
            VisualElement toolbar = MakeEmptyToolbar();
            root.Add(toolbar);
            
            VisualElement toolbarTopRow = MakeEmptyToolbarRow();
            toolbar.Add(toolbarTopRow);
            
            Label fieldLabel = MakeToolbarLabel("Snapshot:");
            toolbarTopRow.Add(fieldLabel);

            multiZoneViewField = MakeMultiZoneViewField(activeMultiZoneView);
            multiZoneViewField.RegisterValueChangedCallback(OnSnapshotFieldChanged);
            toolbarTopRow.Add(multiZoneViewField);

            VisualElement fieldToButtonSpacer = MakeSpacer(20f, 0f);
            toolbarTopRow.Add(fieldToButtonSpacer);
            
            var captureButton = new Button(OnCaptureClicked) { text = "Capture Zones" };
            StyleButton(captureButton);
            toolbarTopRow.Add(captureButton);
            
            var refreshButton = new Button(OnRefreshClicked) { text = "Refresh" };
            StyleButton(refreshButton);
            toolbarTopRow.Add(refreshButton);
            
            clearButton = new Button(OnClearClicked) { text = "Clear" };
            StyleButton(clearButton);
            toolbarTopRow.Add(clearButton);
            
            VisualElement topSpacer = MakeSpacer();
            toolbarTopRow.Add(topSpacer);

            statusLabel = MakeToolbarLabel("");
            toolbarTopRow.Add(statusLabel);
            
            VisualElement toolbarBottomRow = MakeEmptyToolbarRow();
            toolbar.Add(toolbarBottomRow);
            
            Toggle useZoneHandlerCrawlToggle = MakeToggle("Crawl ZoneHandlers", useZoneHandlerCrawl);
            useZoneHandlerCrawlToggle.RegisterValueChangedCallback(changeEvent => useZoneHandlerCrawl = changeEvent.newValue);
            toolbarBottomRow.Add(useZoneHandlerCrawlToggle);

            startingZoneField = MakeZoneField(rootZone);
            startingZoneField.RegisterValueChangedCallback(OnStartingZoneFieldChanged);
            toolbarBottomRow.Add(startingZoneField);
            
            Toggle drawConnectionsToggle = MakeToggle("Draw Connections", drawConnections, TextAnchor.MiddleRight);
            drawConnectionsToggle.RegisterValueChangedCallback(changeEvent => drawConnections = changeEvent.newValue);
            toolbarBottomRow.Add(drawConnectionsToggle);
            
            VisualElement bottomSpacer = MakeSpacer();
            toolbarBottomRow.Add(bottomSpacer);
            
            Toggle keepPositionToggle = MakeToggle("Keep positions", keepExistingPositions, TextAnchor.MiddleRight);
            keepPositionToggle.RegisterValueChangedCallback(changeEvent => keepExistingPositions = changeEvent.newValue);
            toolbarBottomRow.Add(keepPositionToggle);
            
            Toggle keepDimensionsToggle = MakeToggle("Keep dimensions", keepExistingDimensions, TextAnchor.MiddleRight);
            keepDimensionsToggle.RegisterValueChangedCallback(changeEvent => keepExistingDimensions = changeEvent.newValue);
            toolbarBottomRow.Add(keepDimensionsToggle);
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
        
        private void OnSnapshotFieldChanged(ChangeEvent<Object> changeEvent)
        {
            if (!isToolAvailable)
            {
                multiZoneViewField.SetValueWithoutNotify(changeEvent.previousValue);
                return;
            }
            
            var selected = changeEvent.newValue as MultiZoneView;
            SetActiveMultiZoneView(selected);
        }
        
        private void SetActiveMultiZoneView(MultiZoneView multiZoneView)
        {
            if (!isToolAvailable) { return; }
            ClearRenderedZoneViews();

            activeMultiZoneView = multiZoneView;
            
            if (activeMultiZoneView != null) { TryLoadSnapshots(); }
            AddAllZoneViews();
            curvesLayer?.MarkDirtyRepaint();
            canvas?.MarkDirtyRepaint();
            RefreshToolbarState();
        }

        private void OnStartingZoneFieldChanged(ChangeEvent<Object> changeEvent)
        {
            var selected = changeEvent.newValue as Zone;
            rootZone = selected;
        }

        private void OnCaptureClicked()
        {
            if (!isToolAvailable) { return; }
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
            if (!isToolAvailable) { return; }
            activeMultiZoneView = null;
            multiZoneViewField?.SetValueWithoutNotify(null);
            ClearRenderedZoneViews();
            curvesLayer?.MarkDirtyRepaint();
            canvas?.MarkDirtyRepaint();
            RefreshToolbarState();
        }

        private void OnRefreshClicked()
        {
            OnRefreshClicked(true);
        }

        private void OnRefreshClicked(bool clearPanOffset)
        {
            if (!isToolAvailable) { return; }
            RefreshZoneViews(clearPanOffset);
            RefreshToolbarState();
        }
        #endregion
        
        #region ParametersPanel
        private void BuildParametersPanel(VisualElement setCanvas)
        {
            VisualElement parametersPanel = MakeEmptyParametersPanel("Scaling Factors");
            setCanvas.Add(parametersPanel);

            VisualElement worldToSnapshotScalingField = MakeFloatInputField("World-to-Snapshot Scaling", worldToSnapshotScalingFactor, newValue => worldToSnapshotScalingFactor = newValue);
            parametersPanel.Add(worldToSnapshotScalingField);

            VisualElement snapshotToZoneViewScalingField = MakeFloatInputField("Snapshot-to-ZoneView Scaling", snapshotToZoneViewScalingFactor, newValue => snapshotToZoneViewScalingFactor = newValue);
            parametersPanel.Add(snapshotToZoneViewScalingField);

            VisualElement additionalMaxScalingField = MakeFloatInputField("Additional Max Scaling", additionalMaxScalingFactor, newValue => additionalMaxScalingFactor = newValue);
            parametersPanel.Add(additionalMaxScalingField);
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
            
            curvesLayer = MakeEmptyCurvesLayer();
            SubscribeCurvesLayerToDrawCurves(true);
            canvas.Add(curvesLayer);
        }
        
        private void OnCanvasPanned(Vector2 delta)
        {
            panOffset += delta;
            zoneViewLayer.style.left = panOffset.x;
            zoneViewLayer.style.top = panOffset.y;
            curvesLayer?.MarkDirtyRepaint();
            canvas?.MarkDirtyRepaint();
        }
        #endregion
        
        #region ZoneViews

        private void RefreshZoneViews(bool clearPanOffset = true)
        {
            ClearRenderedZoneViews(clearPanOffset);
            TryLoadSnapshots();
            AddAllZoneViews();
            curvesLayer?.MarkDirtyRepaint();
            canvas?.MarkDirtyRepaint();
        }
        
        private void AddAllZoneViews()
        {
            ApplyPanOffset();
            foreach (ZoneView zoneView in zoneViews)
            {
                AddZoneViewElement(zoneView);
            }
        }
        
        private void AddZoneViewElement(ZoneView zoneView)
        {
            ZoneViewData zoneViewData = zoneView.data;
            if (zoneViewData == null) { return; }
            
            VisualElement zoneViewElement = MakeEmptyZoneViewElement(zoneViewData.topLeftPosition, zoneViewData.dimensions);
            
            Label zoneViewElementHeader = MakeZoneViewElementHeader(zoneViewData.zoneName);
            void OnClickedHeader() => Selection.activeObject = zoneViewData;
            void OnDraggedCurveRepaint() => curvesLayer?.MarkDirtyRepaint();
            zoneViewElementHeader.AddManipulator(new DragManipulator(zoneView, zoneViewElement, OnClickedHeader, OnDraggedCurveRepaint));
            zoneViewElement.Add(zoneViewElementHeader);
            
            VisualElement imageArea = AddImageToZoneViewElement(zoneView, zoneViewElement);
            
            void OnClickedImage() => TryLoadScene(zoneView);
            imageArea.AddManipulator(new DragManipulator(zoneView, zoneViewElement, OnClickedImage, OnDraggedCurveRepaint));
            
            zoneViewLayer.Add(zoneViewElement);
        }

        private VisualElement AddImageToZoneViewElement(ZoneView zoneView, VisualElement zoneViewElement)
        {
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
            if (!isToolAvailable) { return; }
            string scenePath = zoneView?.data?.scenePath;
            if (scenePath == null) { return; }
            
            if (string.IsNullOrEmpty(scenePath) || !File.Exists(scenePath) || !scenePath.EndsWith(".unity")) { return; }

            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                RefreshZoneViews(false);
            }
        }
        #endregion
        
        #region Capture
        private void CaptureAllZones()
        {
            if (activeMultiZoneView == null) { return; }
            activeMultiZoneView.CleanDanglingZoneViewData();
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) { return; }

            Dictionary<string, Bounds> zoneDimensionsLookup = new();
            List<ZoneHandlerNodeData> zoneHandlerNodeDataSet = new();

            string originalScenePath = SceneManager.GetActiveScene().path;
            Directory.CreateDirectory(_snapshotPNGDirectory);
            EnsureAssetFolder();
            
            try
            {
                Vector2 currentPosition = new Vector2(_zoneViewPadding, _zoneViewPadding);
                float yOffset = _zoneViewPadding;

                int sceneCount = 1;
                foreach (string scenePath in OpenNextViableScenePath())
                {
                    string zoneName = GetSafeNameFromPath(scenePath);
                    Bounds zoneBounds = CalculateZoneBounds();
                    zoneDimensionsLookup[zoneName] = zoneBounds;
                    
                    Debug.Log($"Positioning camera on Scene: {zoneName}");
                    Texture2D texture2D = CaptureZone(zoneBounds);
                    string snapshotPNGPath = GetSnapshotPathForScene(zoneName);
                    File.WriteAllBytes(snapshotPNGPath, texture2D.EncodeToPNG());

                    if (useZoneHandlerCrawl) { zoneHandlerNodeDataSet.AddRange(ZoneHandlerConduit.BuildZoneHandlerNodeData()); }
                    
                    Vector2 zoneViewDimensions = GetIdealZoneViewDimensions(texture2D, false);
                    ZoneViewData zoneViewData = activeMultiZoneView.CreateOrUpdateZoneViewData(zoneName, scenePath, snapshotPNGPath, zoneViewDimensions, currentPosition, keepExistingPositions, keepExistingDimensions);

                    bool isyOffset = sceneCount % _defaultNumberViewsPerRow == 0;
                    yOffset = Mathf.Max(yOffset, zoneViewData.dimensions.y + _zoneViewPadding); // EditorWindow Top Padding
                    currentPosition = GetUpdatedZoneViewPosition(currentPosition, zoneViewData.dimensions.x, isyOffset, yOffset);
                    if (isyOffset) { yOffset = _zoneViewPadding; }
                    
                    sceneCount++;
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                if (!string.IsNullOrEmpty(originalScenePath) && File.Exists(originalScenePath))
                {
                    EditorSceneManager.OpenScene(originalScenePath, OpenSceneMode.Single);
                }
                else
                {
                    EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
                }
            }

            if (useZoneHandlerCrawl && activeMultiZoneView != null)
            {
                List<ZoneHandlerLinkData> zoneHandlerLinkData = ZoneHandlerConduit.GenerateZoneHandlerLinks(zoneHandlerNodeDataSet, zoneDimensionsLookup);
                Debug.Log($"count of zone handler link data is {zoneHandlerLinkData.Count}");
                activeMultiZoneView.UpdateZoneHandlerLinkData(zoneHandlerLinkData);
            }
            
            EditorUtility.SetDirty(activeMultiZoneView);
            AssetDatabase.SaveAssetIfDirty(activeMultiZoneView);
        }

        private IEnumerable<string> OpenNextViableScenePath()
        {
            List<string> allScenePaths = GetBuildProfileScenePaths();
            
            // Pass existing scene placement if we want to place those views first, then we can place new views after to check for overlaps
            HashSet<string> existingViewScenePaths = new HashSet<string>();
            if (keepExistingPositions) { existingViewScenePaths = activeMultiZoneView.GetScenePaths(); }
            
            return !useZoneHandlerCrawl ? OpenBuildScenePaths(allScenePaths, allScenePaths.Count, existingViewScenePaths) : ZoneHandlerConduit.OpenLinkedScenePaths(rootZone, allScenePaths.Count, existingViewScenePaths);
        }
        
        private static IEnumerable<string> OpenBuildScenePaths(List<string> buildScenePaths, int maxZoneCount, HashSet<string> existingViewScenePaths)
        {
            Queue<string> scenePaths = new();
            foreach (string existingScenePath in existingViewScenePaths) { scenePaths.Enqueue(existingScenePath); }
            foreach (var buildScenePath in buildScenePaths.Where(buildScenePath => !existingViewScenePaths.Contains(buildScenePath))) { scenePaths.Enqueue(buildScenePath); }
            
            int currentZoneCount = 0;
            while (scenePaths.Count > 0)
            {
                string currentScenePath = scenePaths.Dequeue();
                EditorUtility.DisplayProgressBar("MultiZone Viewer", "Capturing all build profile zones", (float)currentZoneCount / maxZoneCount);
                EditorSceneManager.OpenScene(currentScenePath, OpenSceneMode.Single);
                yield return currentScenePath;

                currentZoneCount++;
            }
        }

        private static Vector2 GetUpdatedZoneViewPosition(Vector2 currentPosition, float lastZoneViewWidth, bool isyOffset, float yOffset)
        {
            Vector2 newPosition = new Vector2(currentPosition.x, currentPosition.y);
            newPosition.x += lastZoneViewWidth + _zoneViewPadding;
            
            if (isyOffset)
            {
                newPosition.x = _zoneViewPadding; 
                newPosition.y += yOffset + _zoneViewPadding; // ZoneView-to-ZoneView padding
            }
            return newPosition;
        }
        
        private static Bounds CalculateZoneBounds()
        {
            Bounds zoneBounds = new Bounds();
            
            List<Tilemap> tilemaps = FindObjectsByType<Tilemap>().ToList();
            if (tilemaps.Count == 0)
            {
                List<Renderer> renderers = FindObjectsByType<Renderer>().ToList();
                Debug.Log($"Using standard renderers to calculate bounds with {renderers.Count} renderers");
                
                bool maxBoundsSet = false;
                foreach (Renderer renderer in renderers)
                {
                    if (!maxBoundsSet) { zoneBounds = renderer.bounds; maxBoundsSet = true; }
                    else { zoneBounds.Encapsulate(renderer.bounds); }
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
                        zoneBounds.SetMinMax(minPosition, maxPosition); 
                        maxBoundsSet = true;
                    }
                    else
                    {
                        Bounds newBounds = new Bounds();
                        newBounds.SetMinMax(minPosition, maxPosition);
                        zoneBounds.Encapsulate(newBounds);
                    }
                }
            }
            Debug.Log($"Max Bounding at {zoneBounds.center} with extents: {zoneBounds.extents}");
            return zoneBounds;
        }
        
        private Texture2D CaptureZone(Bounds zoneBounds)
        {
            if (activeMultiZoneView == null) { return null; }

            Camera captureCamera = Camera.main;
            if (captureCamera == null) { return null; }
            
            Vector2 snapshotDimensions = PositionCameraToFrameScene(captureCamera, zoneBounds);
            Texture2D snapshotTexture = CameraClick(captureCamera, snapshotDimensions);
            Debug.Log($"Snapshot texture dimensions are {snapshotTexture.width}, {snapshotTexture.height}");

            return snapshotTexture;
        }

        private Vector2 PositionCameraToFrameScene(Camera camera, Bounds zoneBounds)
        {
            camera.transform.position = new Vector3(zoneBounds.center.x, zoneBounds.center.y, camera.transform.position.z);
            Vector2 snapshotDimensions = GetIdealSnapshotDimensions(zoneBounds.extents.x, zoneBounds.extents.y);
            
            float aspectRatio = snapshotDimensions.x / snapshotDimensions.y;
            float orthoSize = aspectRatio > 1.0f ? 
                Mathf.Max(zoneBounds.extents.x / aspectRatio, zoneBounds.extents.y) : 
                Mathf.Max(zoneBounds.extents.x, zoneBounds.extents.y);
            camera.orthographicSize = orthoSize;
            
            return snapshotDimensions;
        }
        
        private static Texture2D CameraClick(Camera captureCamera, Vector2 snapshotDimensions)
        {
            int snapshotWidth = Mathf.RoundToInt(snapshotDimensions.x);
            int snapshotHeight = Mathf.RoundToInt(snapshotDimensions.y);
            
            var renderTexture = new RenderTexture(snapshotWidth, snapshotHeight, 24, RenderTextureFormat.ARGB32);
            captureCamera.targetTexture = renderTexture;
            captureCamera.Render();

            RenderTexture.active = renderTexture;
            var snapshotTexture = new Texture2D(snapshotWidth, snapshotHeight, TextureFormat.RGBA32, false);
            snapshotTexture.ReadPixels(new Rect(0, 0, snapshotWidth, snapshotHeight), 0, 0);
            snapshotTexture.Apply();

            RenderTexture.active = null;
            captureCamera.targetTexture = null;
            renderTexture.Release();
            DestroyImmediate(renderTexture);
            return snapshotTexture;
        }

        private Vector2 GetIdealSnapshotDimensions(float xWorldSize, float yWorldSize)
        {
            if (Mathf.Approximately(xWorldSize, 0f) || Mathf.Approximately(yWorldSize, 0f)) { return _dummySnapshotDimensions; }
            
            float xScaled = xWorldSize * worldToSnapshotScalingFactor;
            float yScaled = yWorldSize * worldToSnapshotScalingFactor;
            
            if (xScaled < _targetMinSnapshotDimensions.x || yScaled < _targetMinSnapshotDimensions.y)
            {
                float xMinMultiplier = Mathf.Min(_targetMinSnapshotDimensions.x / xScaled, additionalMaxScalingFactor);
                float yMinMultiplier = Mathf.Min(_targetMinSnapshotDimensions.y / yScaled, additionalMaxScalingFactor);
                
                xScaled *= xMinMultiplier > yMinMultiplier ? xMinMultiplier : yMinMultiplier;
                yScaled *= xMinMultiplier > yMinMultiplier ? xMinMultiplier : yMinMultiplier;
            }

            if (xScaled > _targetMaxSnapshotDimensions.x || yScaled > _targetMaxSnapshotDimensions.y)
            {
                // Straight floor, don't preserve aspect ratio (to be handled separately via ortho size)
                xScaled = _targetMaxSnapshotDimensions.x;
                yScaled = _targetMaxSnapshotDimensions.y;
            }
            
            return new Vector2(xScaled, yScaled);
        }
        
        private Vector2 GetIdealZoneViewDimensions(Texture2D texture2D, bool bypassChecks)
        {
            if (texture2D == null) { return _defaultZoneViewDimensions; }
            
            float tryWidth = texture2D.width * snapshotToZoneViewScalingFactor;
            float tryHeight = texture2D.height * snapshotToZoneViewScalingFactor;
            if (bypassChecks) { return new Vector2(tryWidth, tryHeight); }
            
            if (texture2D.width < _defaultZoneViewDimensions.x || texture2D.height < _defaultZoneViewDimensions.y) { return _defaultZoneViewDimensions; }
            if (tryWidth < _defaultZoneViewDimensions.x || tryHeight < _defaultZoneViewDimensions.y) { return _defaultZoneViewDimensions; }
            
            return new Vector2(tryWidth, tryHeight);
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

                // When the image doesn't fill the zone view completely, we need to account and offset
                Vector2 targetImageDimensions = GetIdealZoneViewDimensions(texture2D, true);
                Vector2 renderedImageDimensions = ScaleToFit(zoneViewData.dimensions, targetImageDimensions);
                Vector2 imageOffset = GetRenderedImageOffset(zoneViewData.dimensions, renderedImageDimensions);
                
                ZoneView zoneView = new ZoneView(zoneViewData, texture2D, renderedImageDimensions, imageOffset);
                zoneViews.Add(zoneView);
                zoneViewLookup[zoneViewData.zoneName] = zoneView;
            }
        }

        private Vector2 ScaleToFit(Vector2 zoneViewDimensions, Vector2 targetImageDimensions)
        {
            float imageAspectRatio = targetImageDimensions.x / targetImageDimensions.y;
            float zoneViewAspectRatio = zoneViewDimensions.x / zoneViewDimensions.y;
            if (zoneViewAspectRatio > imageAspectRatio)
            {
                float aspectRatioRatio = imageAspectRatio / zoneViewAspectRatio;
                return new Vector2(aspectRatioRatio * zoneViewDimensions.x, zoneViewDimensions.y);
            }
            else
            {
                float aspectRatioRatio = zoneViewAspectRatio / imageAspectRatio;
                return new Vector2(zoneViewDimensions.x, aspectRatioRatio * zoneViewDimensions.y);
            }
        }

        private Vector2 GetRenderedImageOffset(Vector2 zoneViewDimensions, Vector2 renderedImageDimensions)
        {
            float xOffset = (zoneViewDimensions.x - renderedImageDimensions.x) / 2;
            xOffset = xOffset > 0f ? xOffset : 0f;
            float yOffset =  (zoneViewDimensions.y - renderedImageDimensions.y) / 2;
            yOffset = yOffset > 0f ? yOffset : 0f;
            Vector2 imageOffset = new Vector2(xOffset, yOffset);
                
            return imageOffset;
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
        private void ClearRenderedZoneViews(bool clearPanOffset = true)
        {
            DisposeRuntimeTextures();
            zoneViews.Clear();
            zoneViewLookup.Clear();
            zoneViewLayer?.Clear();

            if (clearPanOffset)
            {
                panOffset = Vector2.zero;
                ApplyPanOffset();
            }
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
                    backgroundColor = _uiCanvasBackgroundColour
                }
            };
        }
        
        private void DrawGrid(MeshGenerationContext meshGenerationContext)
        {
            Painter2D painter = meshGenerationContext.painter2D;
            Rect area = canvas.contentRect;
            DrawGridLines(painter, area, 30f,  _uiGridLineMinorColour);
            DrawGridLines(painter, area, 150f, _uiGridLineMajorColour);
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

        private static VisualElement MakeEmptyCurvesLayer()
        {
            return new VisualElement
            {
                style =
                {
                    position = Position.Absolute,
                    top = 0,
                    bottom = 0,
                    left = 0,
                    right = 0,
                },
                pickingMode = PickingMode.Ignore // Transparent to mouse events
            };
        }

        private void DrawCurves(MeshGenerationContext meshGenerationContext)
        {
            if (!drawConnections || !useZoneHandlerCrawl) { return; }
            
            var painter2D = meshGenerationContext.painter2D;
            painter2D.strokeColor = _uiBezierLineColour;
            painter2D.lineWidth   = _uiBezierLineWidth;

            foreach (ZoneView zoneView in zoneViews)
            {
                if (zoneView == null) { continue; }
                
                foreach (ZoneHandlerLinkData zoneHandlerLinkData in zoneView.data.zoneHandlerLinkDataSet)
                {
                    ZoneView sourceZoneView = zoneView;
                    if (!zoneViewLookup.TryGetValue(zoneHandlerLinkData.targetZoneName, out ZoneView targetZoneView)) { continue; }

                    Vector2 start = NodeRelativePosition(sourceZoneView, zoneHandlerLinkData.sourceNodeRelativePosition);
                    Vector2 end = NodeRelativePosition(targetZoneView, zoneHandlerLinkData.targetNodeRelativePosition); 
                
                    start += panOffset;
                    end += panOffset;

                    // Horizontal control-point offsets so the handles never collapse on tightly-placed nodes.
                    float clampedOffset = Mathf.Max(Mathf.Abs(end.x - start.x) * 0.15f, 60f);
                    Vector2 clampPoint1 = new Vector2(start.x + clampedOffset, start.y);
                    Vector2 clampPoint2 = new Vector2(end.x   - clampedOffset, end.y);

                    painter2D.BeginPath();
                    painter2D.MoveTo(start);
                    painter2D.BezierCurveTo(clampPoint1, clampPoint2, end);
                    painter2D.Stroke();
                    DrawEndDot(painter2D, end);
                }
            }
        }
        
        private static Vector2 NodeRelativePosition(ZoneView zoneView, Vector2 relativePosition) =>
            new(zoneView.data.topLeftPosition.x + zoneView.renderedImageOffset.x + zoneView.renderedImageDimensions.x * relativePosition.x, zoneView.data.topLeftPosition.y + _zoneViewHeaderHeight + zoneView.renderedImageOffset.y + zoneView.renderedImageDimensions.y * relativePosition.y);
        
        private static void DrawEndDot(Painter2D p, Vector2 centre)
        {
            const float r = 5f;
            p.fillColor = _uiBezierLineColour;
            p.BeginPath();
            p.Arc(centre, r, 0f, 360f);
            p.Fill();
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
                    flexDirection = FlexDirection.Column,
                    alignSelf = Align.Stretch,
                    alignItems = Align.FlexStart,
                    height = 44,
                    backgroundColor = _uiStandardBackgroundColour,
                    borderBottomWidth = 1,
                    borderBottomColor = _uiBorderDarkColour
                }
            };
        }
        
        private static VisualElement MakeEmptyToolbarRow()
        {
            return new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    flexGrow = 1,
                    alignItems = Align.Center,
                    height = 22,
                    paddingLeft = 4,
                    paddingRight = 4,
                    backgroundColor = _uiStandardBackgroundColour,
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
        
        private static ObjectField MakeZoneField(Zone zone)
        {
            return new ObjectField
            {
                objectType = typeof(Zone),
                allowSceneObjects = false,
                value = zone,
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
            button.style.fontSize = _uiStandardFontSize;
            button.style.backgroundColor = _uiButtonColour;
            button.style.color = new StyleColor(Color.white);
            button.style.borderTopLeftRadius = 3;
            button.style.borderTopRightRadius = 3;
            button.style.borderBottomLeftRadius = 3;
            button.style.borderBottomRightRadius = 3;
            button.style.borderTopWidth = 1;
            button.style.borderBottomWidth = 1;
            button.style.borderLeftWidth = 1;
            button.style.borderRightWidth = 1;
            button.style.borderTopColor = _uiBorderDarkColour;
            button.style.borderBottomColor = _uiBorderDarkColour;
            button.style.borderLeftColor = _uiBorderDarkColour;
            button.style.borderRightColor = _uiBorderDarkColour;
        }

        private static Toggle MakeToggle(string label, bool state, TextAnchor textAnchor = TextAnchor.MiddleLeft)
        {
            return new Toggle
            {
                label = label,
                value = state,
                style =
                {
                    marginLeft = 8,
                    marginRight = 4,
                    fontSize = _uiStandardFontSize,
                    color = _uiLabelTextColour,
                    unityTextAlign = textAnchor,
                }
            };
        }

        private static VisualElement MakeSpacer(float minWidth = 0.0f, float flexGrow = 1.0f)
        {
            return new VisualElement { style =
            {
                minWidth = minWidth,
                flexGrow = flexGrow
            } };
        }
        
        private static Label MakeToolbarLabel(string labelText)
        {
            return new Label
            {
                text = labelText,
                style =
                {
                    color = _uiLabelTextColour,
                    fontSize = _uiStandardFontSize,
                    unityTextAlign = TextAnchor.MiddleRight,
                    marginRight = 4
                }
            };
        }

        private static VisualElement MakeEmptyParametersPanel(string panelTitle)
        {
            var parametersPanel = new VisualElement
            {
                style =
                {
                    position = Position.Absolute,
                    right = 12,
                    bottom = 12,
                    paddingTop = 10,
                    paddingBottom = 10,
                    paddingLeft = 12,
                    paddingRight = 12,
                    backgroundColor = _uiStandardBackgroundColour,
                    borderTopLeftRadius = 6,
                    borderTopRightRadius = 6,
                    borderBottomLeftRadius = 6,
                    borderBottomRightRadius = 6,
                    borderTopWidth = 1,
                    borderBottomWidth = 1,
                    borderLeftWidth = 1,
                    borderRightWidth = 1,
                    borderTopColor = _uiBorderBrightColour,
                    borderBottomColor = _uiBorderBrightColour,
                    borderLeftColor = _uiBorderBrightColour,
                    borderRightColor = _uiBorderBrightColour,
                }
            };

            var titleLabel = new Label
            {
                text = panelTitle,
                style =
                {
                    fontSize = _uiStandardFontSize,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    color = _uiLabelTextColour,
                    marginBottom = 8,
                    unityTextAlign = TextAnchor.MiddleLeft,
                }
            };
            parametersPanel.Add(titleLabel);

            var divider = new VisualElement
            {
                style =
                {
                    height = 1,
                    marginBottom    = 8,
                    backgroundColor = _uiBorderBrightColour,
                }
            };
            parametersPanel.Add(divider);
            
            return parametersPanel;
        }

        private static VisualElement MakeFloatInputField(string labelText, float initialValue, System.Action<float> onChanged)
        {
            var floatInputField = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    marginBottom = 6
                }
            };

            var label = new Label(labelText)
            {
                style =
                {
                    fontSize = _uiStandardFontSize,
                    color = _uiLabelTextColour,
                    unityTextAlign = TextAnchor.MiddleLeft,
                }
            };
            floatInputField.Add(label);

            var spacer = new VisualElement { style = { flexGrow = 1 } };
            floatInputField.Add(spacer);

            var field = new FloatField
            {
                value = initialValue
            };
            field.RegisterValueChangedCallback(changedEvent => onChanged(changedEvent.newValue));
            floatInputField.Add(field);
            
            return floatInputField;
        }

        private static VisualElement MakeEmptyZoneViewElement(Vector2 position, Vector2 size)
        {
            var zoneViewElement = new VisualElement
            {
                style =
                {
                    position = Position.Absolute,
                    left = position.x,
                    top = position.y,
                    width = (int)size.x,
                    height = _zoneViewHeaderHeight + (int)size.y,
                    backgroundColor = _uiViewBackgroundColour,
                    borderTopLeftRadius = 4,
                    borderTopRightRadius = 4,
                    borderBottomLeftRadius = 4,
                    borderBottomRightRadius = 4,
                    borderTopWidth = 1,
                    borderBottomWidth = 1,
                    borderLeftWidth = 1,
                    borderRightWidth = 1,
                    borderTopColor = _uiBorderBrightColour,
                    borderBottomColor = _uiBorderBrightColour,
                    borderLeftColor = _uiBorderBrightColour,
                    borderRightColor = _uiBorderBrightColour,
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
                    backgroundColor = _uiViewHeaderColour,
                    unityTextAlign = TextAnchor.MiddleCenter,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    fontSize = _uiStandardFontSize,
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
                image = texture2D, 
                scaleMode = ScaleMode.ScaleToFit,
                style =
                {
                    flexGrow = 1,
                    backgroundColor = _uiImageBackgroundColour
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
                    color = _uiLabelTextColour,
                    backgroundColor = _uiImageBackgroundColour
                }
            };
        }

        private static void AddHoverOverStyle(VisualElement visualElement)
        {
            visualElement.RegisterCallback<MouseEnterEvent>(_ =>
                visualElement.style.backgroundColor = _uiImageHoverBackgroundColour);
            visualElement.RegisterCallback<MouseLeaveEvent>(_ =>
                visualElement.style.backgroundColor = _uiImageBackgroundColour);
        }
        #endregion
    }
}

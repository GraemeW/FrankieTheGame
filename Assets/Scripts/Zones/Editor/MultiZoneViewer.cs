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

namespace Frankie.ZoneManagement.Editor
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

        // Zoom Tunables
        private const float _minZoomScale = 0.25f;
        private const float _maxZoomScale = 3.0f;
        private const float _zoomWheelStepFactor = 1.05f;
        
        // Node Link Tunables
        private const float _uiNodeDotMinLinkDragDistance = 100f;
 
        // UI Styles
        private static readonly StyleColor _uiCanvasBackgroundColour =  new(Color.gray2 * 0.8f); 
        private static readonly Color _uiGridLineMinorColour = new(1f, 1f, 1f, 0.05f);
        private static readonly Color _uiGridLineMajorColour = new(1f, 1f, 1f, 0.10f);
        private static readonly StyleColor _uiStandardBackgroundColour = new(Color.gray2);
        private static readonly StyleColor _uiViewBackgroundColour = new(Color.gray3);
        private static readonly StyleColor _uiViewHeaderColour = new(new Color(0.13f, 0.45f, 0.72f));
        private static readonly StyleColor _uiImageBackgroundColour = new(Color.gray1);
        private static readonly StyleColor _uiImageHoverBackgroundColour = new(Color.skyBlue * 0.3f);
        private static readonly StyleColor _uiBorderDarkColour = new(Color.gray1);
        private static readonly StyleColor _uiBorderBrightColour = new(Color.gray4);
        private static readonly StyleColor _uiButtonColour = new(Color.gray4);
        private static readonly StyleColor _uiLabelTextColour = new(Color.gray6);
        private const float _uiStandardFontSize = 11f;
        private const float _uiBezierLineWidth = 1.2f;
        private static readonly Color _uiBezierLineColourStart = Color.red;
        private static readonly Color _uiBezierLineColourEnd = Color.bisque;
        private const float _uiNodeDotBaseDiameter = 10f;
        private const float _uiNodeDotMinDiameter = 6f;
        private const float _uiNodeDotMaxDiameter = 8f;
        private const float _uiNodeDotHoverScale = 1.5f;
        private static readonly StyleColor _uiNodeDotLinkedColor = new(new Color(Color.red.r, Color.red.g, Color.red.b, 0.8f));
        private static readonly StyleColor _uiNodeDotUnlinkedColor = new(new Color(Color.ivory.r, Color.ivory.g, Color.ivory.b, 0.9f));
        private static readonly StyleColor _uiNodeDotHoverBorderColour = new(Color.bisque);
        private static readonly Color _uiNodeDotDragAboveThresholdColour = Color.lightGreen; 
        private static readonly Color _uiNodeDotDragBelowThresholdColour = Color.gray7;
        
        private static readonly Gradient _uiBezierLineGradient = new()
        {
            colorKeys = new[]
            {
                new GradientColorKey(_uiBezierLineColourStart, 0f),
                new GradientColorKey(_uiBezierLineColourEnd, 1f)
            }
        };
        
        // Editable Configurations
        [SerializeField] private MultiZoneView activeMultiZoneView;
        [SerializeField] private Vector2 panOffset = Vector2.zero;
        [SerializeField] private float zoomScale = 1.0f;
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

        // Node Dot State
        private readonly List<(string zoneName, string zoneNodeID, Rect canvasRect)> nodeDotElements = new();
        private bool isDraggingNodeLink;
        private (string zoneName, string zoneNodeID) activeDragSource;
        private Vector2 dragStartCanvasPosition;
        private Vector2 dragCurrentCanvasPosition;
        
        // UI State
        private VisualElement canvas;
        private VisualElement zoneViewLayer;
        private VisualElement curvesLayer;
        private VisualElement nodeDotsLayer;
        private ObjectField multiZoneViewField;
        private ObjectField startingZoneField;
        private Label statusLabel;
        private Button clearButton;
        private Label zoomLabel;
        
        #region UnityMethods
        [MenuItem("Tools/Multi-Zone Viewer", false, 200)]
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
            SubscribeCanvasToZoom(true);
            SubscribeToOnSceneOpened(true);
            SubscribeToPlayModeStateChanges(true);
            SubscribeToUndoRedo(true);
            SubscribeToSelectionChanged(true);
        }

        private void OnDisable()
        {
            SubscribeCanvasToDrawGrid(false);
            SubscribeCurvesLayerToDrawCurves(false);
            SubscribeCanvasToZoom(false);
            SubscribeToOnSceneOpened(false);
            SubscribeToPlayModeStateChanges(false);
            SubscribeToUndoRedo(false);
            SubscribeToSelectionChanged(false);
            DisposeRuntimeTextures();
        }
        
        public void CreateGUI()
        {
            VisualElement root = rootVisualElement;
            root.style.flexDirection = FlexDirection.Column;
            root.style.flexGrow = 1;

            if (activeMultiZoneView != null)
            {
                TryLoadSnapshots();
                ResyncZoneNodeLinks();
            }
            BuildToolbar(root);
            BuildCanvas(root);
            BuildParametersPanel(canvas);
            AddAllZoneViews();
            RefreshToolbarState();
        }
        #endregion
        
        #region SetupCallbacks
        private void OnSceneOpened(Scene scene, OpenSceneMode mode) => OnRefreshClicked(false);

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

        private void SubscribeCanvasToZoom(bool enable)
        {
            if (canvas == null) { return; }

            canvas.UnregisterCallback<WheelEvent>(OnCanvasZoomed);
            if (enable) { canvas.RegisterCallback<WheelEvent>(OnCanvasZoomed); }
        }

        private void SubscribeToPlayModeStateChanges(bool enable)
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            if (enable) { EditorApplication.playModeStateChanged += OnPlayModeStateChanged; }
        }

        private void SubscribeToOnSceneOpened(bool enable)
        {
            EditorSceneManager.sceneOpened -= OnSceneOpened;
            if (enable) { EditorSceneManager.sceneOpened  += OnSceneOpened; }
        }

        private void SubscribeToUndoRedo(bool enable)
        {
            Undo.undoRedoPerformed -= OnUndoRedoPerformed;
            if (enable) { Undo.undoRedoPerformed += OnUndoRedoPerformed; }
        }

        private void OnUndoRedoPerformed()
        {
            if (!isToolAvailable) { return; }
            RefreshZoneViews(false);
        }

        private void SubscribeToSelectionChanged(bool enable)
        {
            Selection.selectionChanged -= OnSelectionChanged;
            if (enable) { Selection.selectionChanged += OnSelectionChanged; }
        }

        private void OnSelectionChanged()
        {
            if (!isToolAvailable) { return; }

            switch (Selection.activeObject)
            {
                case Zone zone:
                    CenterViewOnZone(zone);
                    break;
                case ZoneNode zoneNode:
                    CenterViewOnZoneNode(zoneNode);
                    break;
            }
        }

        private void CenterViewOnZone(Zone zone)
        {
            if (!zoneViewLookup.TryGetValue(zone.name, out ZoneView zoneView) || zoneView?.data == null) { return; }

            Vector2 boxCentre = zoneView.data.topLeftPosition + new Vector2(zoneView.data.dimensions.x / 2f, (_zoneViewHeaderHeight + zoneView.data.dimensions.y) / 2f);
            CenterViewOnWorldPosition(boxCentre);
        }

        private void CenterViewOnZoneNode(ZoneNode zoneNode)
        {
            if (!zoneViewLookup.TryGetValue(zoneNode.GetZoneName(), out ZoneView zoneView) || zoneView?.data == null) { return; }
            if (!zoneView.data.TryGetZoneNodeData(zoneNode.GetNodeID(), out ZoneNodeData zoneNodeData)) { return; }

            CenterViewOnWorldPosition(GetNodeWorldPosition(zoneView, zoneNodeData.relativePosition));
        }

        private void CenterViewOnWorldPosition(Vector2 worldPosition)
        {
            if (canvas == null) { return; }

            panOffset = canvas.contentRect.size / 2f - worldPosition * zoomScale;
            ApplyPanAndZoom();
            RefreshNodeDots();
            curvesLayer?.MarkDirtyRepaint();
            canvas.MarkDirtyRepaint();
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

            VisualElement clearToZoomSpacer = MakeSpacer(20f, 0f);
            toolbarTopRow.Add(clearToZoomSpacer);

            var resetZoomButton = new Button(OnResetZoomClicked) { text = "Reset Zoom" };
            StyleButton(resetZoomButton);
            toolbarTopRow.Add(resetZoomButton);

            zoomLabel = MakeToolbarLabel($"{Mathf.RoundToInt(zoomScale * 100f)}%");
            toolbarTopRow.Add(zoomLabel);
            
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
                statusLabel.text = hasZoneViews ? $"{zoneViews.Count} scene(s)  —  middle-click or alt+drag to pan, scroll to zoom" : string.Empty;
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
            
            if (activeMultiZoneView != null)
            {
                TryLoadSnapshots();
                ResyncZoneNodeLinks();
            }
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
            ResyncZoneNodeLinks();
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
            SubscribeCanvasToZoom(true);
            canvas.AddManipulator(new MultiZonePanManipulator(OnCanvasPanned));
            root.Add(canvas);
            
            zoneViewLayer = MakeEmptyZoneViewLayer();
            canvas.Add(zoneViewLayer);
            
            curvesLayer = MakeEmptyCurvesLayer();
            SubscribeCurvesLayerToDrawCurves(true);
            canvas.Add(curvesLayer);

            nodeDotsLayer = MakeEmptyNodeDotsLayer();
            canvas.Add(nodeDotsLayer);
        }
        
        private void OnCanvasPanned(Vector2 delta)
        {
            panOffset += delta;
            ApplyPanAndZoom();
            RefreshNodeDots();
            curvesLayer?.MarkDirtyRepaint();
            canvas?.MarkDirtyRepaint();
        }

        private void OnCanvasZoomed(WheelEvent wheelEvent)
        {
            if (!isToolAvailable) { return; }
            
            float factor = wheelEvent.delta.y < 0 ? _zoomWheelStepFactor : 1f / _zoomWheelStepFactor;
            ApplyZoom(factor, wheelEvent.mousePosition);
            wheelEvent.StopPropagation();
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
            ApplyPanAndZoom();
            foreach (ZoneView zoneView in zoneViews)
            {
                AddZoneViewElement(zoneView);
            }
            RefreshNodeDots();
        }
        
        private void AddZoneViewElement(ZoneView zoneView)
        {
            if (zoneViewLayer == null) { return; }
            
            ZoneViewData zoneViewData = zoneView.data;
            if (zoneViewData == null) { return; }
            
            VisualElement zoneViewElement = MakeEmptyZoneViewElement(zoneViewData.topLeftPosition, zoneViewData.dimensions);
            
            Label zoneViewElementHeader = MakeZoneViewElementHeader(zoneViewData.zoneName);

            zoneViewElementHeader.AddManipulator(new MultiZoneDragManipulator(zoneView, zoneViewElement, OnClickedHeader, OnDraggedCurveRepaint, () => zoomScale, OnZoneViewDragComplete));
            zoneViewElement.Add(zoneViewElementHeader);
            
            VisualElement imageArea = AddImageToZoneViewElement(zoneView, zoneViewElement);
            imageArea.AddManipulator(new MultiZoneDragManipulator(zoneView, zoneViewElement, OnClickedImage, OnDraggedCurveRepaint, () => zoomScale, OnZoneViewDragComplete));
            
            zoneViewLayer.Add(zoneViewElement);
            return;
            
            
            // Local Functions
            void OnClickedHeader() => Selection.activeObject = zoneViewData;
            void OnClickedImage() => TryLoadScene(zoneView);
            void OnZoneViewDragComplete()
            {
                SaveMultiZoneViewAsset(activeMultiZoneView, "Move Zone View");
            }
            void OnDraggedCurveRepaint()
            {
                curvesLayer?.MarkDirtyRepaint();
                RefreshNodeDots();
            }
        }

        private static VisualElement AddImageToZoneViewElement(ZoneView zoneView, VisualElement zoneViewElement)
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

        #region NodeDots
        private ZoneViewData FindZoneViewDataByName(string zoneName) => zoneViewLookup.TryGetValue(zoneName, out ZoneView zoneView) ? zoneView.data : null;
        private static ZoneNode GetZoneNodeByID(string zoneName, string zoneNodeID)
        {
            Zone zone = Zone.GetFromName(zoneName);
            return zone == null ? null : zone.GetNodeFromID(zoneNodeID);
        }
        private Vector2 NodeRelativePosition(ZoneView zoneView, Vector2 relativePosition) => GetNodeWorldPosition(zoneView, relativePosition) * zoomScale; 
        private Vector2 GetNodeWorldPosition(ZoneView zoneView, Vector2 relativePosition)
        {
            return new Vector2(
                zoneView.data.topLeftPosition.x + zoneView.renderedImageOffset.x + zoneView.renderedImageDimensions.x * relativePosition.x,
                zoneView.data.topLeftPosition.y + _zoneViewHeaderHeight + zoneView.renderedImageOffset.y + zoneView.renderedImageDimensions.y * relativePosition.y);
        }
        private bool IsPastLinkDragThreshold(Vector2 canvasPosition) => Vector2.Distance(dragStartCanvasPosition, canvasPosition) >= _uiNodeDotMinLinkDragDistance;
        
        private void RefreshNodeDots()
        {
            if (nodeDotsLayer == null) { return; }
            nodeDotsLayer.Clear();
            nodeDotElements.Clear();

            foreach (ZoneView zoneView in zoneViews)
            {
                if (zoneView?.data == null) { continue; }

                foreach (ZoneNodeData zoneNodeData in zoneView.data.zoneNodeDataSet)
                {
                    AddNodeDotElement(zoneView, zoneNodeData);
                }
            }
        }

        private void AddNodeDotElement(ZoneView zoneView, ZoneNodeData zoneNodeData)
        {
            float diameter = Mathf.Clamp(_uiNodeDotBaseDiameter / zoomScale, _uiNodeDotMinDiameter, _uiNodeDotMaxDiameter);
            Vector2 centre = NodeRelativePosition(zoneView, zoneNodeData.relativePosition) + panOffset;
            Rect canvasRect = new Rect(centre.x - diameter / 2f, centre.y - diameter / 2f, diameter, diameter);

            VisualElement dot = MakeNodeDotElement(diameter, zoneNodeData.HasLink());
            dot.style.left = canvasRect.x;
            dot.style.top = canvasRect.y;

            string zoneName = zoneView.data.zoneName;
            string zoneNodeID = zoneNodeData.zoneNodeID;
            dot.AddManipulator(new ZoneNodeLinkManipulator(canvas, () => OnNodeDotDragStarted(zoneName, zoneNodeID), OnNodeDotDragUpdated, OnNodeDotDragEnded));

            nodeDotsLayer.Add(dot);
            nodeDotElements.Add((zoneName, zoneNodeID, canvasRect));
        }
        
        private void OnNodeDotDragStarted(string zoneName, string zoneNodeID)
        {
            isDraggingNodeLink = true;
            activeDragSource = (zoneName, zoneNodeID);
            dragStartCanvasPosition = GetDotCanvasPosition(zoneName, zoneNodeID) ?? Vector2.zero;
            dragCurrentCanvasPosition = dragStartCanvasPosition;
            curvesLayer?.MarkDirtyRepaint();
        }

        private void OnNodeDotDragUpdated(Vector2 canvasPosition)
        {
            if (!isDraggingNodeLink) { return; }
            dragCurrentCanvasPosition = canvasPosition;
            curvesLayer?.MarkDirtyRepaint();
        }

        private void OnNodeDotDragEnded(Vector2 canvasPosition)
        {
            if (!isDraggingNodeLink) { return; }
            isDraggingNodeLink = false;

            if (!IsPastLinkDragThreshold(canvasPosition))
            {
                ZoneNode clickedZoneNode = GetZoneNodeByID(activeDragSource.zoneName, activeDragSource.zoneNodeID);
                if (clickedZoneNode != null) { Selection.activeObject = clickedZoneNode; }
                curvesLayer?.MarkDirtyRepaint();
                return;
            }

            (string zoneName, string zoneNodeID)? dropTarget = FindNodeDotAtCanvasPosition(canvasPosition, activeDragSource);
            if (dropTarget.HasValue)
            {
                TryLinkZoneNodes(activeDragSource.zoneName, activeDragSource.zoneNodeID, dropTarget.Value.zoneName, dropTarget.Value.zoneNodeID);
            }
            else
            {
                TryClearZoneNodeLink(activeDragSource.zoneName, activeDragSource.zoneNodeID);
            }

            curvesLayer?.MarkDirtyRepaint();
        }

        private (string zoneName, string zoneNodeID)? FindNodeDotAtCanvasPosition(Vector2 canvasPosition, (string zoneName, string zoneNodeID) excludeSource)
        {
            foreach (var (zoneName, zoneNodeID, canvasRect) in nodeDotElements)
            {
                if (zoneName == excludeSource.zoneName && zoneNodeID == excludeSource.zoneNodeID) { continue; }
                if (canvasRect.Contains(canvasPosition)) { return (zoneName, zoneNodeID); }
            }
            return null;
        }

        private void TryLinkZoneNodes(string sourceZoneName, string sourceZoneNodeID, string targetZoneName, string targetZoneNodeID)
        {
            if (activeMultiZoneView == null) { return; }
            if (sourceZoneName == targetZoneName) { return; } // Only allow inter-zone links (intra-zone handled by ZoneEditor)

            ZoneNode sourceNode = GetZoneNodeByID(sourceZoneName, sourceZoneNodeID);
            ZoneNode targetNode = GetZoneNodeByID(targetZoneName, targetZoneNodeID);
            if (sourceNode == null || targetNode == null) { return; }
            
            // Save On Asset
            if (!sourceNode.TrySetExternalLink(targetNode)) { return; }
            SaveZoneAsset(sourceNode, "Link Zone Nodes");

            // Update MultiZone Serialized Data
            ZoneViewData sourceZoneViewData = FindZoneViewDataByName(sourceZoneName);
            ZoneViewData targetZoneViewData = FindZoneViewDataByName(targetZoneName);
            if (sourceZoneViewData == null || targetZoneViewData == null) { return; }
            if (!targetZoneViewData.TryGetZoneNodeData(targetZoneNodeID, out ZoneNodeData targetZoneNodeData)) { return; }

            Undo.RecordObject(sourceZoneViewData, "Link Zone Nodes");
            sourceZoneViewData.TrySetLink(sourceZoneNodeID, targetZoneName, targetZoneNodeID, targetZoneNodeData.relativePosition);
            SaveMultiZoneViewAsset(activeMultiZoneView, "Link Zone Nodes");

            RefreshNodeDots();
        }

        private void TryClearZoneNodeLink(string zoneName, string zoneNodeID)
        {
            if (activeMultiZoneView == null) { return; }
            ZoneNode zoneNode = GetZoneNodeByID(zoneName, zoneNodeID);
            if (zoneNode == null) { return; }
            
            // Save on Asset
            if (!zoneNode.ClearExternalLink()) { return; }
            SaveZoneAsset(zoneNode, "Clear Zone Node Link");

            ZoneViewData zoneViewData = FindZoneViewDataByName(zoneName);
            if (zoneViewData != null)
            {
                Undo.RecordObject(zoneViewData, "Clear Zone Node Link");
                if (zoneViewData.TryClearLink(zoneNodeID)) { SaveMultiZoneViewAsset(activeMultiZoneView, "Clear Zone Node Link"); }
            }
            
            RefreshNodeDots();
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
                    
                    zoneHandlerNodeDataSet.AddRange(ZoneHandlerConduit.BuildZoneHandlerNodeData());
                    
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

            if (activeMultiZoneView != null)
            {
                Dictionary<string, List<ZoneNodeData>> zoneNodeDataByZoneName = ZoneHandlerConduit.BuildZoneNodeData(zoneHandlerNodeDataSet, zoneDimensionsLookup);
                activeMultiZoneView.UpdateZoneNodeData(zoneNodeDataByZoneName);
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
        
        #region ZoneNodeResyncCapture
        private class PendingLinkClear
        {
            public ZoneViewData zoneViewData { get; private set; }
            public string zoneNodeID { get; private set; }

            public PendingLinkClear(ZoneViewData zoneViewData, string zoneNodeID)
            {
                this.zoneViewData = zoneViewData;
                this.zoneNodeID = zoneNodeID;
            }
        }

        private class PendingLinkUpdate
        {
            public ZoneViewData sourceZoneViewData { get; private set; }
            public string sourceZoneNodeID { get; private set; }
            public string targetZoneName { get; private set; }
            public string targetZoneNodeID { get; private set; }

            public PendingLinkUpdate(ZoneViewData sourceZoneViewData, string sourceZoneNodeID, string targetZoneName, string targetZoneNodeID)
            {
                this.sourceZoneViewData = sourceZoneViewData;
                this.sourceZoneNodeID = sourceZoneNodeID;
                this.targetZoneName = targetZoneName;
                this.targetZoneNodeID = targetZoneNodeID;
            }
        }
        
        private void ResyncZoneNodeLinks()
        {
            if (activeMultiZoneView == null) { return; }

            var pendingLinkClears = new List<PendingLinkClear>();
            var pendingLinkUpdates = new List<PendingLinkUpdate>();
            var zonesNeedingRecheck = new HashSet<string>();
            
            BuildPendingUpdatesByCachedData(ref pendingLinkClears, ref pendingLinkUpdates, ref zonesNeedingRecheck);
            if (pendingLinkClears.Count == 0 && pendingLinkUpdates.Count == 0) { return; }
            if (zonesNeedingRecheck.Count > 0) { RecheckZonePositions(zonesNeedingRecheck); }
            
            bool anyChange = false;
            foreach (PendingLinkClear pendingLinkClear in pendingLinkClears)
            {
                Undo.RecordObject(pendingLinkClear.zoneViewData, "Resync Zone Node Link");
                if (pendingLinkClear.zoneViewData.TryClearLink(pendingLinkClear.zoneNodeID)) { anyChange = true; }
            }

            foreach (PendingLinkUpdate pendingLinkUpdate in pendingLinkUpdates)
            {
                if (!zoneViewLookup.TryGetValue(pendingLinkUpdate.targetZoneName, out ZoneView targetZoneView)) { continue; }
                if (!targetZoneView.data.TryGetZoneNodeData(pendingLinkUpdate.targetZoneNodeID, out ZoneNodeData targetZoneNodeData)) { continue; } // Still unresolved even after recheck

                Undo.RecordObject(pendingLinkUpdate.sourceZoneViewData, "Resync Zone Node Link");
                if (pendingLinkUpdate.sourceZoneViewData.TrySetLink(pendingLinkUpdate.sourceZoneNodeID, pendingLinkUpdate.targetZoneName, pendingLinkUpdate.targetZoneNodeID, targetZoneNodeData.relativePosition)) { anyChange = true; }
            }
            if (!anyChange) { return; }

            SaveMultiZoneViewAsset(activeMultiZoneView, "Resync Zone Node Links");
            RefreshNodeDots();
            curvesLayer?.MarkDirtyRepaint();
        }

        private void BuildPendingUpdatesByCachedData(ref List<PendingLinkClear> pendingLinkClears, ref List<PendingLinkUpdate> pendingLinkUpdates, ref HashSet<string> zonesNeedingRecheck)
        {
            foreach (ZoneView zoneView in zoneViews)
            {
                ZoneViewData zoneViewData = zoneView?.data;
                if (zoneViewData == null) { continue; }

                foreach (ZoneNodeData zoneNodeData in zoneViewData.zoneNodeDataSet)
                {
                    ZoneNode zoneNode = GetZoneNodeByID(zoneViewData.zoneName, zoneNodeData.zoneNodeID);
                    if (zoneNode == null) { continue; } // Node itself no longer exists; leave cached entry alone

                    if (!zoneNode.HasLinkedSceneReference())
                    {
                        if (zoneNodeData.HasLink()) { pendingLinkClears.Add(new PendingLinkClear(zoneViewData, zoneNodeData.zoneNodeID)); }
                        continue;
                    }

                    ZoneNode linkedZoneNode = zoneNode.GetLinkedZoneNode();
                    string targetZoneName = linkedZoneNode.GetZoneName();
                    string targetZoneNodeID = linkedZoneNode.GetNodeID();

                    bool alreadyInSync = zoneNodeData.HasLink() && zoneNodeData.linkedZoneName == targetZoneName && zoneNodeData.linkedZoneNodeID == targetZoneNodeID;
                    if (alreadyInSync) { continue; }

                    if (!zoneViewLookup.TryGetValue(targetZoneName, out ZoneView targetZoneView) || targetZoneView?.data == null)
                    {
                        Debug.LogWarning($"MultiZoneViewer: Zone node '{zoneNodeData.zoneNodeID}' in '{zoneViewData.zoneName}' now links to zone '{targetZoneName}', which isn't part of this Multi-Zone View. Capture that zone to include this connection.");
                        continue;
                    }

                    pendingLinkUpdates.Add(new PendingLinkUpdate(zoneViewData, zoneNodeData.zoneNodeID, targetZoneName, targetZoneNodeID));
                    if (!targetZoneView.data.TryGetZoneNodeData(targetZoneNodeID, out _)) { zonesNeedingRecheck.Add(targetZoneName); }
                }
            }
        }
        
        private void RecheckZonePositions(HashSet<string> zoneNames)
        {
            if (zoneNames.Count == 0) { return; }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) { return; }

            string originalScenePath = SceneManager.GetActiveScene().path;
            try
            {
                foreach (string zoneName in zoneNames)
                {
                    if (!zoneViewLookup.TryGetValue(zoneName, out ZoneView zoneView) || zoneView?.data == null) { continue; }

                    string scenePath = zoneView.data.scenePath;
                    if (string.IsNullOrEmpty(scenePath) || !File.Exists(scenePath)) { continue; }

                    EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                    Bounds zoneBounds = CalculateZoneBounds();
                    List<ZoneHandlerNodeData> zoneHandlerNodeDataSet = ZoneHandlerConduit.BuildZoneHandlerNodeData();
                    Dictionary<string, Bounds> singleZoneBoundsLookup = new() { { zoneName, zoneBounds } };
                    Dictionary<string, List<ZoneNodeData>> freshZoneNodeDataByZoneName = ZoneHandlerConduit.BuildZoneNodeData(zoneHandlerNodeDataSet, singleZoneBoundsLookup);
                    if (!freshZoneNodeDataByZoneName.TryGetValue(zoneName, out List<ZoneNodeData> freshZoneNodeDataSet)) { continue; }

                    Dictionary<string, Vector2> relativePositionByNodeID = new();
                    foreach (ZoneNodeData freshZoneNodeData in freshZoneNodeDataSet)
                    {
                        relativePositionByNodeID[freshZoneNodeData.zoneNodeID] = freshZoneNodeData.relativePosition;
                    }

                    Undo.RecordObject(zoneView.data, "Resync Zone Node Positions");
                    zoneView.data.UpdateZoneNodePositions(relativePositionByNodeID);
                }
            }
            finally
            {
                if (!string.IsNullOrEmpty(originalScenePath) && File.Exists(originalScenePath)) { EditorSceneManager.OpenScene(originalScenePath, OpenSceneMode.Single); }
                else { EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single); }
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
        
        private static void SaveZoneAsset(ZoneNode zoneNode, string undoMessage)
        {
            AssetDatabase.SaveAssetIfDirty(zoneNode);
            
            // Since ZoneNode childed to Zone, mark as dirty and save as well
            Zone sourceZone = zoneNode.GetZone();
            if (sourceZone == null) { return; }
            Undo.RecordObject(sourceZone, undoMessage);
            EditorUtility.SetDirty(sourceZone);
            AssetDatabase.SaveAssetIfDirty(sourceZone);
        }

        private static void SaveMultiZoneViewAsset(MultiZoneView multiZoneView, string undoMessage)
        {
            if (multiZoneView == null) { return; }
            
            Undo.RecordObject(multiZoneView, undoMessage);
            EditorUtility.SetDirty(multiZoneView);
            AssetDatabase.SaveAssetIfDirty(multiZoneView);
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
            nodeDotsLayer?.Clear();
            nodeDotElements?.Clear();
            isDraggingNodeLink = false;

            if (clearPanOffset)
            {
                panOffset = Vector2.zero;
                zoomScale = 1f;
                ApplyPanAndZoom();
                RefreshZoomLabel();
            }
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
            float scaledSpacing = spacing * zoomScale;
            if (scaledSpacing < 2f) { return; } // avoid a degenerate/overly-dense grid at extreme zoom-out

            painter.strokeColor = color;
            painter.lineWidth = 1f;

            float xOffset = panOffset.x % scaledSpacing;
            float yOffset = panOffset.y % scaledSpacing;

            for (float x = -xOffset; x < area.width; x += scaledSpacing)
            {
                painter.BeginPath();
                painter.MoveTo(new Vector2(x, 0));
                painter.LineTo(new Vector2(x, area.height));
                painter.Stroke();
            }

            for (float y = -yOffset; y < area.height; y += scaledSpacing)
            {
                painter.BeginPath();
                painter.MoveTo(new Vector2(0, y));
                painter.LineTo(new Vector2(area.width, y));
                painter.Stroke();
            }
        }
        
        private void ApplyPanAndZoom()
        {
            if (zoneViews == null) { return; }
            if (zoneViewLayer == null) { return; }
            zoneViewLayer.style.left = panOffset.x;
            zoneViewLayer.style.top  = panOffset.y;
            zoneViewLayer.style.scale = new Scale(new Vector3(zoomScale, zoomScale, 1f));
            zoneViewLayer.style.transformOrigin = new TransformOrigin(0, 0, 0);
        }
        
        private void ApplyZoom(float factor, Vector2 pivotScreenPosition)
        {
            float oldZoom = zoomScale;
            float newZoom = Mathf.Clamp(oldZoom * factor, _minZoomScale, _maxZoomScale);
            if (Mathf.Approximately(newZoom, oldZoom)) { return; }
            
            Vector2 worldPointUnderCursor = (pivotScreenPosition - panOffset) / oldZoom;
            panOffset = pivotScreenPosition - worldPointUnderCursor * newZoom;
            zoomScale = newZoom;

            ApplyPanAndZoom();
            RefreshZoomLabel();
            RefreshNodeDots();
            curvesLayer?.MarkDirtyRepaint();
            canvas?.MarkDirtyRepaint();
        }

        private void OnResetZoomClicked()
        {
            if (!isToolAvailable) { return; }
            zoomScale = 1f;
            ApplyPanAndZoom();
            RefreshZoomLabel();
            RefreshNodeDots();
            curvesLayer?.MarkDirtyRepaint();
            canvas?.MarkDirtyRepaint();
        }

        private void RefreshZoomLabel()
        {
            if (zoomLabel == null) { return; }
            zoomLabel.text = $"{Mathf.RoundToInt(zoomScale * 100f)}%";
        }

        private void DrawCurves(MeshGenerationContext meshGenerationContext)
        {
            var painter2D = meshGenerationContext.painter2D;

            if (drawConnections && useZoneHandlerCrawl)
            {
                painter2D.strokeGradient = _uiBezierLineGradient;
                painter2D.lineWidth   = _uiBezierLineWidth * Mathf.Clamp(zoomScale, 0.5f, 2f);

                foreach (ZoneView zoneView in zoneViews)
                {
                    if (zoneView == null) { continue; }

                    foreach (ZoneNodeData zoneNodeData in zoneView.data.zoneNodeDataSet)
                    {
                        if (!zoneNodeData.HasLink()) { continue; }

                        ZoneView sourceZoneView = zoneView;
                        if (!zoneViewLookup.TryGetValue(zoneNodeData.linkedZoneName, out ZoneView targetZoneView)) { continue; }

                        Vector2 start = NodeRelativePosition(sourceZoneView, zoneNodeData.relativePosition);
                        Vector2 end = NodeRelativePosition(targetZoneView, zoneNodeData.linkedRelativePosition);

                        start += panOffset;
                        end += panOffset;

                        // Horizontal control-point offsets so the handles never collapse on tightly-placed nodes.
                        float clampedOffset = Mathf.Max(Mathf.Abs(end.x - start.x) * 0.15f, 60f * zoomScale);
                        Vector2 clampPoint1 = new Vector2(start.x + clampedOffset, start.y);
                        Vector2 clampPoint2 = new Vector2(end.x   - clampedOffset, end.y);

                        painter2D.BeginPath();
                        painter2D.MoveTo(start);
                        painter2D.BezierCurveTo(clampPoint1, clampPoint2, end);
                        painter2D.Stroke();
                    }
                }
            }

            if (isDraggingNodeLink) { DrawActiveLinkDrag(painter2D); }
        }

        private void DrawActiveLinkDrag(Painter2D painter2D)
        {
            Vector2? sourceCanvasPosition = GetDotCanvasPosition(activeDragSource.zoneName, activeDragSource.zoneNodeID);
            if (!sourceCanvasPosition.HasValue) { return; }

            painter2D.strokeColor = IsPastLinkDragThreshold(dragCurrentCanvasPosition) ? _uiNodeDotDragAboveThresholdColour : _uiNodeDotDragBelowThresholdColour;
            painter2D.lineWidth = _uiBezierLineWidth * Mathf.Clamp(zoomScale, 0.5f, 2f);
            painter2D.BeginPath();
            painter2D.MoveTo(sourceCanvasPosition.Value);
            painter2D.LineTo(dragCurrentCanvasPosition);
            painter2D.Stroke();
        }

        private Vector2? GetDotCanvasPosition(string zoneName, string zoneNodeID)
        {
            foreach (var (candidateZoneName, candidateZoneNodeID, canvasRect) in nodeDotElements)
            {
                if (candidateZoneName != zoneName || candidateZoneNodeID != zoneNodeID) { continue; }
                return canvasRect.center;
            }
            return null;
        }
        #endregion

        #region StaticUIBuilders
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
        
        private static VisualElement MakeEmptyNodeDotsLayer()
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
                pickingMode = PickingMode.Ignore  // Layer itself transparent to mouse events, dots are not
            };
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
        
        private static VisualElement MakeNodeDotElement(float diameter, bool isLinked)
        {
            var dot = new VisualElement
            {
                style =
                {
                    position = Position.Absolute,
                    width = diameter,
                    height = diameter,
                    borderTopLeftRadius = diameter / 2f,
                    borderTopRightRadius = diameter / 2f,
                    borderBottomLeftRadius = diameter / 2f,
                    borderBottomRightRadius = diameter / 2f,
                    backgroundColor = isLinked ? _uiNodeDotLinkedColor : _uiNodeDotUnlinkedColor,
                    borderTopWidth = 1,
                    borderBottomWidth = 1,
                    borderLeftWidth = 1,
                    borderRightWidth = 1,
                    borderTopColor = _uiBorderDarkColour,
                    borderBottomColor = _uiBorderDarkColour,
                    borderLeftColor = _uiBorderDarkColour,
                    borderRightColor = _uiBorderDarkColour,
                    // Scale grows/shrinks around the dot's own centre rather than its top-left corner.
                    transformOrigin = new StyleTransformOrigin(new TransformOrigin(Length.Percent(50), Length.Percent(50))),
                }
            };
            AddNodeDotHoverStyle(dot);
            return dot;
        }

        private static void AddNodeDotHoverStyle(VisualElement dot)
        {
            dot.RegisterCallback<MouseEnterEvent>(_ =>
            {
                dot.style.scale = new StyleScale(new Scale(new Vector3(_uiNodeDotHoverScale, _uiNodeDotHoverScale, 1f)));
                dot.style.borderTopColor = _uiNodeDotHoverBorderColour;
                dot.style.borderBottomColor = _uiNodeDotHoverBorderColour;
                dot.style.borderLeftColor = _uiNodeDotHoverBorderColour;
                dot.style.borderRightColor = _uiNodeDotHoverBorderColour;
            });
            dot.RegisterCallback<MouseLeaveEvent>(_ =>
            {
                dot.style.scale = new StyleScale(new Scale(Vector3.one));
                dot.style.borderTopColor = _uiBorderDarkColour;
                dot.style.borderBottomColor = _uiBorderDarkColour;
                dot.style.borderLeftColor = _uiBorderDarkColour;
                dot.style.borderRightColor = _uiBorderDarkColour;
            });
        }
        #endregion
    }
}

using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Profile;

namespace Frankie.ZoneManagement.UIEditor
{
    public class SceneSnapshotViewer : EditorWindow
    {
        // Tunables
        private const int _zoneViewWidth = 260;
        private const int _zoneViewHeight = 200;
        private const int _zoneViewHeaderHeight = 24;
        private const int _snapshotWidth = 512;
        private const int _snapshotHeight = 288;
        private const float _zoneViewPadding  = 20f;
        
        private static readonly string _snapshotDirectory = Path.Combine(Directory.GetCurrentDirectory(), "MultiZoneViewer");

        // State
        private readonly List<ZoneView> zoneViews = new();
        private VisualElement canvas;
        private VisualElement zoneViewLayer;
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
            zoneViews.Clear();
            TryLoadSnapshots();

        }

        private void OnDisable()
        {
            SubscribeCanvasToDrawGrid(false);
            DisposeSnapshots();
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

            var captureButton = new Button(OnCaptureClicked) { text = "Capture Zones" };
            StyleButton(captureButton);
            toolbar.Add(captureButton);

            clearButton = new Button(OnClearClicked) { text = "Clear" };
            StyleButton(clearButton);
            toolbar.Add(clearButton);

            var spacer = MakeSpacer();
            toolbar.Add(spacer);

            statusLabel = MakeEmptyStatusLabel();
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

        private void OnCaptureClicked()
        {
            SnapshotAllZones();
            
            TryLoadSnapshots();
            
            AddAllZoneViews();
            canvas?.MarkDirtyRepaint();
            RefreshToolbarState();
        }

        private void OnClearClicked()
        {
            DisposeSnapshots();
            zoneViews.Clear();
            panOffset = Vector2.zero;

            if (zoneViewLayer != null)
            {
                zoneViewLayer.Clear();
                zoneViewLayer.style.left = 0; zoneViewLayer.style.top = 0;
            }
            
            canvas?.MarkDirtyRepaint();
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
        private void AddAllZoneViews()
        {
            foreach (ZoneView zoneView in zoneViews)
            {
                AddZoneViewElement(zoneView);
            }
        }
        
        private void AddZoneViewElement(ZoneView zoneView)
        {
            var zoneViewElement = MakeEmptyZoneViewElement(zoneView.zoneName, zoneView.topLeftPosition.x, zoneView.topLeftPosition.y);
            
            AddImageToZoneViewElement(zoneView, zoneViewElement);
            zoneViewElement.AddManipulator(new DragManipulator(zoneView, zoneViewElement));
            
            zoneViewLayer.Add(zoneViewElement);
        }

        private static void AddImageToZoneViewElement(ZoneView zoneView, VisualElement zoneViewElement)
        {
            if (zoneView.snapshot != null)
            {
                Image zoneSnapshot = MakeImage(zoneView.snapshot);
                zoneViewElement.Add(zoneSnapshot);
            }
            else
            {
                Label noSnapshotLabel = MakeImageLabel("No snapshot");
                zoneViewElement.Add(noSnapshotLabel);
            }
        }
        #endregion
        
        private void SnapshotAllZones()
        {
            List<string> scenePaths = GetBuildProfileScenePaths();
            if (scenePaths == null || scenePaths.Count == 0)
            {
                EditorUtility.DisplayDialog("Scene Snapshot Viewer",
                    "No scenes found in the active build profile / build settings.", "OK");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) { return; }

            DisposeSnapshots();
            zoneViews.Clear();
            zoneViewLayer?.Clear();
            
            panOffset = Vector2.zero;
            if (zoneViewLayer != null) { zoneViewLayer.style.left = 0; zoneViewLayer.style.top = 0; }

            Directory.CreateDirectory(_snapshotDirectory);
            string originalScene = SceneManager.GetActiveScene().path;

            try
            {
                for (int i = 0; i < scenePaths.Count; i++)
                {
                    string path = scenePaths[i];
                    EditorUtility.DisplayProgressBar("Scene Snapshot Viewer",
                        $"Processing: {Path.GetFileNameWithoutExtension(path)}  ({i + 1}/{scenePaths.Count})",
                        (float)i / scenePaths.Count);
                    SnapshotZone(path);
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
        }
        
        private static void SnapshotZone(string scenePath)
        {
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            Camera captureCamera = Camera.main;
            if (captureCamera == null) { return; }
            
            PositionCameraToFrameScene(captureCamera, scene);

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

            File.WriteAllBytes(SnapshotPathForScene(scenePath), snapshotTexture.EncodeToPNG());
        }
        
        private static void PositionCameraToFrameScene(Camera camera, Scene scene)
        {
            // TODO:  Zoom camera out to fit bounds of scene
            // General approach TBD, but could:
            // - Find all tilemap renderers -> get tilemap -> squish
            // - Get bounds of each in world space, find largest bounds
            // - Get ortho size = divide largest bounds / 2
            // - Set camera ortho -> capture -> reset ortho
        }
        
        private void TryLoadSnapshots()
        {
            if (!Directory.Exists(_snapshotDirectory)) return;

            var pngFiles = Directory.GetFiles(_snapshotDirectory, "Snapshot_*.png");
            if (pngFiles.Length == 0) return;

            System.Array.Sort(pngFiles);

            for (int i = 0; i < pngFiles.Length; i++)
            {
                string file  = pngFiles[i];
                byte[] bytes = File.ReadAllBytes(file);
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);

                if (!tex.LoadImage(bytes))
                {
                    DestroyImmediate(tex);
                    continue;
                }
                
                string fileName  = Path.GetFileNameWithoutExtension(file);
                string sceneName = fileName.Length > 9 ? fileName.Substring(9) : fileName;

                zoneViews.Add(new ZoneView
                {
                    zoneName = sceneName,
                    snapshot  = tex,
                    topLeftPosition  = new Vector2(
                        _zoneViewPadding + (i % 4) * (_zoneViewWidth + _zoneViewPadding),
                        _zoneViewPadding + (i / 4) * (_zoneViewHeight + _zoneViewHeaderHeight + _zoneViewPadding))
                });
            }
        }

        private void DisposeSnapshots()
        {
            foreach (ZoneView zoneView in zoneViews.Where(zoneView => zoneView.snapshot != null))
            {
                DestroyImmediate(zoneView.snapshot);
            }
        }
        
        #region PathHandling
        private static List<string> GetBuildProfileScenePaths()
        {
            var paths = new List<string>();
            
            var profile = BuildProfile.GetActiveBuildProfile();
            if (profile == null || profile.scenes == null) { return paths; }

            paths.AddRange(from scene in profile.scenes where scene.enabled select scene.path);
            return paths;
        }
        
        private static string SnapshotPathForScene(string scenePath)
        {
            string safeName = Path.GetFileNameWithoutExtension(scenePath);
            safeName = Path.GetInvalidFileNameChars().Aggregate(safeName, (current, c) => current.Replace(c, '_'));
            return Path.Combine(_snapshotDirectory, $"Snapshot_{safeName}.png");
        }
        #endregion
        
        #region UIHelpers

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

        private static VisualElement MakeSpacer()
        {
            return new VisualElement { style = { flexGrow = 1 } };
        }
        
        private static Label MakeEmptyStatusLabel()
        {
            return new Label
            {
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
            
            var header = new Label(zoneName)
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
            zoneViewElement.Add(header);
            
            return zoneViewElement;
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
        #endregion
    }
}

using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using LowDefMustard.Utils.Editor;

namespace Frankie.Combat.Editor
{
    public class SkillTreeEditor : EditorWindow
    {
        // State
        private SkillTree selectedSkillTree;
        private SkillTree lastFramedSkillTree;

        private Label treeNameLabel;
        private Label zoomLabel;
        private VisualElement viewportContainer;
        private VisualElement canvasContent;
        private VisualElement nodesLayer;
        private SkillTreeEdgesLayer edgesLayer;
        private StandardCanvasZoomManipulator zoomManipulator;

        [MenuItem("Window/SkillTree Editor")]
        public static void ShowEditorWindow()
        {
            GetWindow(typeof(SkillTreeEditor), false, "SkillTree Editor");
        }

        [OnOpenAsset(1)]
        public static bool OnOpenAsset(EntityId instanceID, int line)
        {
            var skillTree = EditorUtility.EntityIdToObject(instanceID) as SkillTree;
            if (skillTree == null) { return false; }
            skillTree.CreateRootSkillBranchIfMissing();
            ShowEditorWindow();
            return true;
        }

        private void OnEnable()
        {
            Selection.selectionChanged += OnSelectionChanged;
        }

        private void OnDisable()
        {
            Selection.selectionChanged -= OnSelectionChanged;
        }

        private void OnSelectionChanged()
        {
            if (Selection.activeObject is not SkillTree newSkillTree) { return; }
            if (newSkillTree == selectedSkillTree) { return; }

            selectedSkillTree = newSkillTree;
            UpdateTitleLabel();
            RedrawCanvas();
        }

        #region Layout
        public void CreateGUI()
        {
            Toolbar toolbar = MakeToolbar(out treeNameLabel, out zoomLabel, out ToolbarButton resetViewButton);
            resetViewButton.RegisterCallback<ClickEvent>(_ => ResetView());
            
            viewportContainer = MakeViewport();
            canvasContent = MakeCanvasContent();

            canvasContent.Add(new StandardBackgroundLayer(StandardBackgroundType.Lines));

            edgesLayer = new SkillTreeEdgesLayer(() => selectedSkillTree);
            canvasContent.Add(edgesLayer);

            nodesLayer = MakeNodesLayer();
            canvasContent.Add(nodesLayer);

            viewportContainer.Add(canvasContent);

            zoomManipulator = new StandardCanvasZoomManipulator(canvasContent);
            zoomManipulator.zoomChanged += pct => zoomLabel.text = $"{Mathf.RoundToInt(pct * 100f)}%";
            viewportContainer.AddManipulator(zoomManipulator);
            viewportContainer.AddManipulator(new StandardCanvasPanManipulator(canvasContent));

            rootVisualElement.Add(toolbar);
            rootVisualElement.Add(viewportContainer);

            UpdateTitleLabel();
            RedrawCanvas();
        }

        private void UpdateTitleLabel()
        {
            if (treeNameLabel == null) { return; }
            treeNameLabel.text = selectedSkillTree != null ? selectedSkillTree.name : "No tree selected.";
        }
        #endregion

        #region Canvas Building
        private void RedrawCanvas()
        {
            if (nodesLayer == null) { return; } // UI not built yet

            nodesLayer.Clear();

            if (selectedSkillTree == null)
            {
                edgesLayer?.MarkDirtyRepaint();
                return;
            }

            SkillBranch rootSkillBranch = selectedSkillTree.GetRootSkillBranch();
            foreach (SkillBranch skillBranch in selectedSkillTree.GetAllBranches())
            {
                bool isRoot = skillBranch == rootSkillBranch;
                var nodeView = new SkillBranchView(
                    skillBranch, selectedSkillTree, isRoot,
                    RedrawCanvas, OnNodeDragLive, OnNodeDragComplete, () => zoomManipulator.zoomFactor);
                nodesLayer.Add(nodeView);
            }
            edgesLayer.MarkDirtyRepaint();

            if (lastFramedSkillTree != selectedSkillTree)
            {
                lastFramedSkillTree = selectedSkillTree;
                ResetView();
            }
        }

        private void OnNodeDragLive()
        {
            edgesLayer.MarkDirtyRepaint();
        }

        private void OnNodeDragComplete()
        {
            edgesLayer.MarkDirtyRepaint();
        }
        #endregion

        #region View Control
        private void ResetView()
        {
            if (selectedSkillTree == null || viewportContainer == null || canvasContent == null) { return; }

            SkillBranch rootSkillBranch = selectedSkillTree.GetAllBranches().FirstOrDefault();
            if (rootSkillBranch == null) { return; }

            float zoom = zoomManipulator?.zoomFactor ?? 1f;
            Vector2 rootCenterLocal = rootSkillBranch.GetRect().center;
            Vector2 viewportSize = new(viewportContainer.resolvedStyle.width, viewportContainer.resolvedStyle.height);
            Vector2 viewportCenter = viewportSize * 0.5f;
            Vector2 newOffset = viewportCenter - rootCenterLocal * zoom;

            canvasContent.style.left = newOffset.x;
            canvasContent.style.top = newOffset.y;
        }
        #endregion

        #region StaticUIBuilders
        private static VisualElement MakeFlexibleSpacer() => new() { style = { flexGrow = 1 } };
        
        private static Toolbar MakeToolbar(out Label title, out Label zoom, out ToolbarButton resetViewButton)
        {
            var toolbar = new Toolbar();
            
            title = new Label
            {
                style =
                {
                    unityFontStyleAndWeight = FontStyle.Bold,
                    marginLeft = 6f
                }
            };
            toolbar.Add(title);
            toolbar.Add(MakeFlexibleSpacer());
            
            zoom = new Label("100%")
            {
                style =
                {
                    marginRight = 6f,
                    unityTextAlign = TextAnchor.MiddleRight,
                    width = 44f
                }
            };
            toolbar.Add(zoom);
            
            resetViewButton = new ToolbarButton{ text = "Reset View", };
            toolbar.Add(resetViewButton);
            return toolbar;
        }

        private static VisualElement MakeViewport()
        {
            return new VisualElement
            {
                name = "skill-tree-viewport",
                style =
                {
                    flexGrow = 1,
                    overflow = Overflow.Hidden,
                    backgroundColor = new Color(0.15f, 0.15f, 0.16f)
                }
            };
        }

        private static VisualElement MakeCanvasContent()
        {
            return new VisualElement
            {
                name = "skill-tree-canvas-content",
                style =
                {
                    position = Position.Absolute,
                    left = 0f,
                    top = 0f
                }
            };
        }
        
        private static VisualElement MakeNodesLayer() => new() { name = "skill-tree-nodes-layer" };
        #endregion
    }
}

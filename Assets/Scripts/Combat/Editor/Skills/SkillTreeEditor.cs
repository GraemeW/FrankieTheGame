using System;
using System.Collections.Generic;
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
        // Const Tunables
        private const string _themeStyleSheetField = nameof(themeStyleSheet);
        private const string _skillsToggleText = "Skills";
        private const string _skillsToggleTooltip = "Show / hide the skill list";
        
        // Keys
        private const string _skillsPanelPrefsKey = "Frankie.Combat.Editor.SkillTreeEditor.isSkillListShown";

        // Tunables
        [SerializeField] private StyleSheet themeStyleSheet; // Set as the script's default reference

        // State
        private readonly List<Skill> allSkills = new();
        private SkillTree selectedSkillTree;
        private SkillTree lastFramedSkillTree;

        private Label treeNameLabel;
        private Label zoomLabel;
        private VisualElement viewportContainer;
        private VisualElement canvasContent;
        private VisualElement nodesLayer;
        private SkillTreeEdgesLayer edgesLayer;
        private StandardCanvasZoomManipulator zoomManipulator;
        private SkillListPanel skillListPanel;
        private AutocompleteDropdown skillAutocomplete;

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
            EditorApplication.projectChanged += RefreshSkills;
            Undo.undoRedoPerformed += RedrawCanvas;
        }

        private void OnDisable()
        {
            Selection.selectionChanged -= OnSelectionChanged;
            EditorApplication.projectChanged -= RefreshSkills;
            Undo.undoRedoPerformed -= RedrawCanvas;
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
            if (themeStyleSheet == null) { themeStyleSheet = StandardBrownTheme.GetDefaultReference(this, _themeStyleSheetField); }
            StandardBrownTheme.Apply(rootVisualElement, themeStyleSheet);

            Toolbar toolbar = MakeToolbar(out treeNameLabel, out zoomLabel, out ToolbarButton resetViewButton);
            resetViewButton.RegisterCallback<ClickEvent>(_ => ResetView());
            toolbar.Add(MakeSkillsListToggle());
            
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
            
            skillListPanel = new SkillListPanel();
            SetSkillListShown(IsSkillListShown());
            
            var body = new VisualElement { style = { flexGrow = 1 } };
            body.Add(viewportContainer);
            body.Add(skillListPanel);

            skillAutocomplete = new AutocompleteDropdown();
            
            rootVisualElement.Add(toolbar);
            rootVisualElement.Add(body);
            rootVisualElement.Add(skillAutocomplete);

            RefreshSkills();
            UpdateTitleLabel();
            RedrawCanvas();
        }

        private void UpdateTitleLabel()
        {
            if (treeNameLabel == null) { return; }
            treeNameLabel.text = selectedSkillTree != null ? selectedSkillTree.name : "No tree selected.";
        }
        #endregion

        #region CanvasDraws
        private void RedrawCanvas()
        {
            if (nodesLayer == null) { return; } // UI not built yet

            nodesLayer.Clear();
            RefreshSelectedSkillTreeSkills();

            if (selectedSkillTree == null)
            {
                edgesLayer?.MarkDirtyRepaint();
                return;
            }

            SkillBranch rootSkillBranch = selectedSkillTree.GetRootSkillBranch();
            foreach (SkillBranch skillBranch in selectedSkillTree.GetAllBranches())
            {
                bool isRoot = skillBranch == rootSkillBranch;
                var nodeView = new SkillBranchView(skillBranch, selectedSkillTree, isRoot, RedrawCanvas, RefreshSelectedSkillTreeSkills, OnNodeDragLive, OnNodeDragComplete, () => zoomManipulator.zoomFactor, AttachSkillAutocomplete);
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

        #region SkillsList
        private ToolbarToggle MakeSkillsListToggle()
        {
            var toggle = new ToolbarToggle { text = _skillsToggleText, tooltip = _skillsToggleTooltip, value = IsSkillListShown() };
            toggle.RegisterValueChangedCallback(changeEvent => SetSkillListShown(changeEvent.newValue));
            return toggle;
        }
        
        private static bool IsSkillListShown() => EditorPrefs.GetBool(_skillsPanelPrefsKey, true);
        
        private void SetSkillListShown(bool isShown)
        {
            EditorPrefs.SetBool(_skillsPanelPrefsKey, isShown);
            if (skillListPanel != null) { skillListPanel.style.display = isShown ? DisplayStyle.Flex : DisplayStyle.None; }
        }
        
        private void RefreshSkills()
        {
            allSkills.Clear();
            allSkills.AddRange(AssetDatabase.FindAssets($"t:{nameof(Skill)}")
                .Select(guid => AssetDatabase.LoadAssetAtPath<Skill>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(skill => skill != null)
                .OrderBy(skill => skill.name, StringComparer.OrdinalIgnoreCase));
            skillListPanel?.SetSkills(allSkills);
        }

        private void RefreshSelectedSkillTreeSkills() => skillListPanel?.SetSelectedSkillTreeSkills(selectedSkillTree != null ? selectedSkillTree.GetAllBranches().SelectMany(branch => branch.GetAllSkills()) : null);

        private void AttachSkillAutocomplete(TextField skillField) => skillAutocomplete?.Attach(skillField, allSkills.Where(skill => skill != null).Select(skill => skill.name)); // Deferred so it can follow RefreshSkills
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
            var viewport = new VisualElement
            {
                name = "skill-tree-viewport",
                style =
                {
                    flexGrow = 1,
                    overflow = Overflow.Hidden
                }
            };
            viewport.AddToClassList(StandardBrownTheme.canvasClass);
            return viewport;
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

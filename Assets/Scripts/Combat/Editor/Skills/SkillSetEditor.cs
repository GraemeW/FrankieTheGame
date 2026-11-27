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
    public class SkillSetEditor : EditorWindow
    {
        // Const Tunables
        private const string _windowTitle = "Skill Set Editor";
        private const string _skillsToggleText = "Skills";
        private const string _skillsToggleTooltip = "Show / hide the skill list";
        private const string _noSelectionText = "No tree or pool selected.";
        private const string _inTreeText = "in tree";
        private const string _inPoolText = "in pool";
        private const float _poolCardTopMargin = 40f;
        
        // Field Names
        private const string _themeStyleSheetField = nameof(themeStyleSheet);
        
        // Keys
        private const string _skillsPanelPrefsKey = "Frankie.Combat.Editor.SkillSetEditor.isSkillListShown";

        // Tunables
        [SerializeField] private StyleSheet themeStyleSheet; // Set as the script's default reference

        // State
        private readonly List<Skill> allSkills = new();
        private SkillSet selectedSkillSet;
        private SkillSet lastFramedSkillSet;
        private readonly UndoChangeSnapshot undoSnapshot = new();

        // UI State
        private Label treeNameLabel;
        private Label zoomLabel;
        private VisualElement viewportContainer;
        private VisualElement canvasContent;
        private VisualElement nodesLayer;
        private SkillTreeEdgesLayer edgesLayer;
        private StandardCanvasZoomManipulator zoomManipulator;
        private SkillListPanel skillListPanel;
        private AutocompleteDropdown skillAutocomplete;

        #region UnityMethods
        [MenuItem("Window/Skill Set Editor")]
        public static void ShowEditorWindow()
        {
            GetWindow(typeof(SkillSetEditor), false, _windowTitle);
        }

        [OnOpenAsset(1)]
        public static bool OnOpenAsset(EntityId instanceID, int line)
        {
            if (EditorUtility.EntityIdToObject(instanceID) is not SkillSet skillSet) { return false; }
            ShowSkillSet(skillSet);
            return true;
        }

        private void OnEnable()
        {
            Selection.selectionChanged += OnSelectionChanged;
            EditorApplication.projectChanged += OnProjectChanged;
            Undo.undoRedoPerformed += OnUndoRedoPerformed;
        }

        private void OnDisable()
        {
            Selection.selectionChanged -= OnSelectionChanged;
            EditorApplication.projectChanged -= OnProjectChanged;
            Undo.undoRedoPerformed -= OnUndoRedoPerformed;
        }

        private void OnProjectChanged()
        {
            RefreshSkills();
            if (IsSelectedSkillSetDeleted()) { RedrawCanvas(); } // Clears the stale views (their branches are destroyed)
        }
        #endregion

        #region SkillSetSelection
        public static void ShowSkillSet(SkillSet skillSet)
        {
            if (skillSet == null) { return; }
            var window = (SkillSetEditor)GetWindow(typeof(SkillSetEditor), false, _windowTitle);
            window.SetSelectedSkillSet(skillSet);
        }
        
        private bool IsSelectedSkillSetDeleted() => selectedSkillSet is not null && selectedSkillSet == null; // Destroyed but still referenced ~ Unity-null, though type checks still match

        private void OnSelectionChanged()
        {
            if (Selection.activeObject is not SkillSet newSkillSet) { return; }
            SetSelectedSkillSet(newSkillSet);
        }

        private void SetSelectedSkillSet(SkillSet newSkillSet)
        {
            if (newSkillSet == selectedSkillSet) { return; }
            if (newSkillSet is SkillTree skillTree) { skillTree.CreateRootSkillBranchIfMissing(); } // Every path in (open, selection, inspector) ~ a new tree has no branches yet

            selectedSkillSet = newSkillSet;
            UpdateTitleLabel();
            RedrawCanvas();
        }
        #endregion

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

            edgesLayer = new SkillTreeEdgesLayer(() => selectedSkillSet as SkillTree); // Nothing to draw for a pool
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
            treeNameLabel.text = selectedSkillSet != null ? selectedSkillSet.name : _noSelectionText;
        }
        #endregion

        #region CanvasDraws
        private void RedrawCanvas()
        {
            if (nodesLayer == null) { return; } // UI not built yet
            if (IsSelectedSkillSetDeleted()) { selectedSkillSet = null; UpdateTitleLabel(); } // e.g. an undo landing before the project refresh

            nodesLayer.Clear();
            RefreshSelectedSkillSetSkills();

            switch (selectedSkillSet)
            {
                case SkillTree skillTree:
                    DrawSkillTree(skillTree);
                    break;
                case SkillPool skillPool:
                    DrawSkillPool(skillPool);
                    break;
            }
            edgesLayer?.MarkDirtyRepaint();
            CaptureUndoSnapshot();

            if (selectedSkillSet == null || lastFramedSkillSet == selectedSkillSet) { return; }
            lastFramedSkillSet = selectedSkillSet;
            ResetView();
        }

        private void DrawSkillTree(SkillTree skillTree)
        {
            SkillBranch rootSkillBranch = skillTree.GetRootSkillBranch();
            foreach (SkillBranch skillBranch in skillTree.GetAllBranches())
            {
                bool isRoot = skillBranch == rootSkillBranch;
                var nodeView = new SkillBranchView(skillBranch, skillTree, isRoot, RedrawCanvas, RefreshSelectedSkillSetSkills, OnNodeDragLive, OnNodeDragComplete, () => zoomManipulator.zoomFactor, ResolveSkill, AttachSkillAutocomplete);
                nodesLayer.Add(nodeView);
            }
        }

        private void DrawSkillPool(SkillPool skillPool)
        {
            nodesLayer.Add(new SkillPoolView(skillPool, ResolveSkill, RefreshSelectedSkillSetSkills, AttachSkillAutocomplete));
        }
        
        private void ResetView()
        {
            if (selectedSkillSet == null || viewportContainer == null || canvasContent == null) { return; }

            float zoom = zoomManipulator?.zoomFactor ?? 1f;
            Vector2 viewportSize = new(viewportContainer.resolvedStyle.width, viewportContainer.resolvedStyle.height);
            Vector2 viewportCenter = viewportSize * 0.5f;
            Vector2 newOffset;
            if (selectedSkillSet is SkillTree skillTree)
            {
                SkillBranch rootSkillBranch = skillTree.GetAllBranches().FirstOrDefault();
                if (rootSkillBranch == null) { return; }
                newOffset = viewportCenter - rootSkillBranch.GetRect().center * zoom;
            }
            else
            {
                newOffset = new Vector2(viewportCenter.x - SkillPoolView.cardWidth * 0.5f * zoom, _poolCardTopMargin); // Card grows downwards ~ pin its top
            }

            canvasContent.style.left = newOffset.x;
            canvasContent.style.top = newOffset.y;
        }
        
        private void OnUndoRedoPerformed()
        {
            if (!undoSnapshot.HasChanged()) { return; }
            RedrawCanvas();
        }

        private void CaptureUndoSnapshot()
        {
            switch (selectedSkillSet)
            {
                case SkillTree skillTree:
                    undoSnapshot.Capture(skillTree.GetAllBranches().Prepend<UnityEngine.Object>(skillTree));
                    break;
                case SkillPool skillPool:
                    undoSnapshot.Capture(skillPool);
                    break;
                default:
                    undoSnapshot.Clear();
                    break;
            }
        }

        private void OnNodeDragLive() => edgesLayer.MarkDirtyRepaint();
        private void OnNodeDragComplete() => edgesLayer.MarkDirtyRepaint();
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

        private void RefreshSelectedSkillSetSkills()
        {
            if (skillListPanel == null) { return; }
            switch (selectedSkillSet)
            {
                case SkillTree skillTree:
                    skillListPanel.SetSelectedSkillSetSkills(skillTree.GetAllBranches().SelectMany(branch => branch.GetAllSkills()), _inTreeText);
                    break;
                case SkillPool skillPool:
                    skillListPanel.SetSelectedSkillSetSkills(skillPool.GetWeightedSkills().Where(pair => pair?.value != null).Select(pair => pair.value), _inPoolText);
                    break;
                default:
                    skillListPanel.SetSelectedSkillSetSkills(null, null);
                    break;
            }
        }
        
        private void AttachSkillAutocomplete(TextField skillField) => skillAutocomplete?.Attach(skillField, allSkills.Where(skill => skill != null).Select(skill => skill.name)); // Deferred so it can follow RefreshSkills
        
        private Skill ResolveSkill(string skillName) => ResolveSkill(allSkills, skillName);

        internal static Skill ResolveSkill(IReadOnlyList<Skill> skills, string skillName)
        {
            // Exact name first, then case-insensitive (typed names)
            return skills.FirstOrDefault(skill => skill != null && skill.name == skillName)
                ?? skills.FirstOrDefault(skill => skill != null && string.Equals(skill.name, skillName, StringComparison.OrdinalIgnoreCase));
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

using System;
using UnityEngine;
using UnityEngine.UIElements;
using Frankie.Utils.Editor;

namespace Frankie.Combat.Editor
{
    public class SkillBranchView : VisualElement
    {
        // Const Tunables
        private const float _nodeCornerRadius = 6f;
        private const float _rootNodeBorderWidth = 3f;
        private const float _nodeBorderWidth = 2f;
        private const float _nodePadding = 6f;
        private const float _headerHeight = 30f;
        private const float _smallButtonSize = 20f;
        private static readonly Color _nodeBackgroundColour = Color.burlywood * 0.7f;
        private static readonly Color _nodeBorderColour = Color.blanchedAlmond * 0.6f;
        private static readonly Color _nodeHeaderBackgroundColour = Color.peru * 0.4f;
        private static readonly Color _rootNodeBackgroundColour = Color.saddleBrown * 0.7f;
        private static readonly Color _rootNodeBorderColour = Color.goldenRod * 0.6f;
        private static readonly Color _rootHeaderBackgroundColour = Color.gray1 * 0.4f;

        // State
        private readonly SkillBranch skillBranch;
        private readonly SkillTree skillTree;
        private readonly bool isRoot;
        private readonly Action onStructureChanged;
        
        // UI State
        private readonly VisualElement body;

        public SkillBranchView(SkillBranch skillBranch, SkillTree skillTree, bool isRoot,
            Action onStructureChanged, Action onPositionChangedLive, Action onPositionChangedComplete, Func<float> zoomProvider)
        {
            this.skillBranch = skillBranch;
            this.skillTree = skillTree;
            this.isRoot = isRoot;
            this.onStructureChanged = onStructureChanged;
            if (skillBranch == null || skillTree == null) { return; }

            name = $"skill-node-{skillBranch.name}";
            InitializeStyle();

            Rect rect = skillBranch.GetRect();
            style.position = Position.Absolute;
            style.left = rect.x;
            style.top = rect.y;
            style.width = rect.width;
            style.height = rect.height;

            VisualElement header = MakeHeader(isRoot ? "Root Skill Branch" : "Skill Branch", isRoot ? _rootHeaderBackgroundColour : _nodeHeaderBackgroundColour, _headerHeight);
            header.AddManipulator(new StandardNodeDragManipulator(this, skillBranch, onPositionChangedLive, onPositionChangedComplete, zoomProvider));
            header.Add(MakeFlexibleSpacer());
            Add(header);

            if (!isRoot)
            {
                VisualElement removeButton = MakeSmallButton("-", true);
                removeButton.RegisterCallback<ClickEvent>(_ =>
                {
                    skillTree.DeleteSkillBranch(skillBranch, skillBranch.GetParentBranchMapping());
                    onStructureChanged?.Invoke();
                });
                header.Add(removeButton);
            }
            
            body = MakeBody();
            Add(body);
            RefreshSkillBranchMapping();
        }

        #region PrivateMethods
        private void InitializeStyle()
        {
            style.backgroundColor = isRoot ? _rootNodeBackgroundColour : _nodeBackgroundColour;

            Color borderColor = isRoot ? _rootNodeBorderColour : _nodeBorderColour;
            float borderWidth = isRoot ? _rootNodeBorderWidth : _nodeBorderWidth;
            style.borderTopColor = borderColor;
            style.borderBottomColor = borderColor;
            style.borderLeftColor = borderColor;
            style.borderRightColor = borderColor;
            style.borderTopWidth = borderWidth;
            style.borderBottomWidth = borderWidth;
            style.borderLeftWidth = borderWidth;
            style.borderRightWidth = borderWidth;

            style.borderTopLeftRadius = _nodeCornerRadius;
            style.borderTopRightRadius = _nodeCornerRadius;
            style.borderBottomLeftRadius = _nodeCornerRadius;
            style.borderBottomRightRadius = _nodeCornerRadius;

            style.paddingBottom = _nodePadding;
        }

        private bool ShouldShowAddButton(SkillBranchMapping skillBranchMapping) => skillBranch.HasSkill(skillBranchMapping) && !skillBranch.HasBranch(skillBranchMapping);
        private void UpdateSkillField(string newSkillName, SkillBranchMapping skillBranchMapping) => skillBranch.SetSkill(newSkillName, skillBranchMapping);
        private void CreateChildSkillBranch(SkillBranch parentSkillBranch, SkillBranchMapping skillBranchMapping) => skillTree.CreateChildSkillBranch(parentSkillBranch, skillBranchMapping);
        
        private void RefreshSkillBranchMapping()
        {
            if (body == null) { return; }
            
            body.Clear();
            foreach (SkillBranchMapping skillBranchMapping in Enum.GetValues(typeof(SkillBranchMapping)))
            {
                VisualElement detailRow = MakeDetailRow(skillBranchMapping.ToString(), skillBranch.GetSkill(skillBranchMapping), ShouldShowAddButton(skillBranchMapping), out TextField skillField, out Button addButton);
                skillField?.RegisterValueChangedCallback(changeEvent =>
                {
                    UpdateSkillField(changeEvent.newValue, skillBranchMapping);
                    addButton.style.display = ShouldShowAddButton(skillBranchMapping) ? DisplayStyle.Flex : DisplayStyle.None;
                });
                addButton?.RegisterCallback<ClickEvent>(_ =>
                {
                    CreateChildSkillBranch(skillBranch, skillBranchMapping);
                    onStructureChanged?.Invoke();
                });
                
                body.Add(detailRow);
            }
        }
        #endregion
        
        #region StaticUIBuilders
        private static VisualElement MakeFlexibleSpacer() => new() { style = { flexGrow = 1 } };
        
        private static VisualElement MakeHeader(string label, Color backgroundColour, float height)
        {
            var header = new VisualElement
            {
                style =
                {
                    backgroundColor = backgroundColour,
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    paddingLeft = _nodePadding,
                    paddingRight = _nodePadding,
                    marginBottom = 4f,
                    borderTopLeftRadius = _nodeCornerRadius - 2f,
                    borderTopRightRadius = _nodeCornerRadius - 2f,
                    height = height,
                }
            };

            var headerLabel = new Label(label);
            header.Add(headerLabel);
            return header;
        }
        
        private static VisualElement MakeBody()
        {
            return new VisualElement
            {
                style =
                {
                    flexGrow = 1,
                    paddingLeft = _nodePadding,
                    paddingRight = _nodePadding
                }
            };
        }
        
        private static VisualElement MakeDetailRow(string skillBranchMappingName, Skill currentSkill, bool shouldShowAddButton, out TextField skillField, out Button addButton)
        {
            var row = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    marginBottom = 2f
                }
            };

            var mappingLabel = new Label(skillBranchMappingName) { style = { width = 40f } };
            row.Add(mappingLabel);
            
            skillField = new TextField
            {
                value = currentSkill != null ? currentSkill.name : string.Empty, 
                isDelayed = true,
                style =
                {
                    flexGrow = 1
                }
            };
            row.Add(skillField);

            addButton = MakeSmallButton("+", shouldShowAddButton);
            row.Add(addButton);
            return row;
        }
        
        private static Button MakeSmallButton(string text, bool shouldDisplay)
        {
            return new Button
            {
                text = text,
                style =
                {
                    width = _smallButtonSize,
                    marginLeft = 2f,
                    display = shouldDisplay ? DisplayStyle.Flex : DisplayStyle.None
                }
            };
        }
        #endregion
    }
}

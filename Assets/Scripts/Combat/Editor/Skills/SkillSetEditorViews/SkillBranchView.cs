using System;
using UnityEngine;
using UnityEngine.UIElements;
using LowDefMustard.Utils.Editor;

namespace Frankie.Combat.Editor
{
    public class SkillBranchView : VisualElement
    {
        // State
        private readonly SkillBranch skillBranch;
        private readonly SkillTree skillTree;
        private readonly bool isRoot;
        private readonly Action onStructureChanged;
        private readonly Action onSkillsChanged;
        private readonly Func<string, Skill> resolveSkill;
        private readonly Action<TextField> attachSkillAutocomplete;
        
        // UI State
        private readonly VisualElement body;

        public SkillBranchView(SkillBranch skillBranch, SkillTree skillTree, bool isRoot, Action onStructureChanged, Action onSkillsChanged, Action onPositionChangedLive, Action onPositionChangedComplete, Func<float> zoomProvider, Func<string, Skill> resolveSkill, Action<TextField> attachSkillAutocomplete)
        {
            this.skillBranch = skillBranch;
            this.skillTree = skillTree;
            this.isRoot = isRoot;
            this.onStructureChanged = onStructureChanged;
            this.onSkillsChanged = onSkillsChanged;
            this.resolveSkill = resolveSkill;
            this.attachSkillAutocomplete = attachSkillAutocomplete;
            if (skillBranch == null || skillTree == null) { return; }

            name = $"skill-node-{skillBranch.name}";
            SkillNodeStyle.ApplyCardStyle(this, isRoot);

            Rect rect = skillBranch.GetRect();
            style.position = Position.Absolute;
            style.left = rect.x;
            style.top = rect.y;
            style.width = rect.width;
            style.height = rect.height;

            VisualElement header = SkillNodeStyle.MakeHeader(isRoot ? "Root Skill Branch" : "Skill Branch", isRoot);
            header.AddManipulator(new StandardNodeDragManipulator(this, skillBranch, onPositionChangedLive, onPositionChangedComplete, zoomProvider));
            header.Add(SkillNodeStyle.MakeFlexibleSpacer());
            Add(header);

            if (!isRoot)
            {
                VisualElement removeButton = SkillNodeStyle.MakeSmallButton("-", true);
                removeButton.RegisterCallback<ClickEvent>(_ =>
                {
                    skillTree.DeleteSkillBranch(skillBranch, skillBranch.GetParentBranchMapping());
                    onStructureChanged?.Invoke();
                });
                header.Add(removeButton);
            }
            
            body = SkillNodeStyle.MakeBody();
            Add(body);
            RefreshSkillBranchMapping();
        }

        #region PrivateMethods
        private bool IsAssignable(Skill skill) => SkillNodeStyle.IsAssignableSkill(skill, resolveSkill);
        private bool ShouldShowAddButton(SkillBranchMapping skillBranchMapping) => skillBranch.HasSkill(skillBranchMapping) && !skillBranch.HasBranch(skillBranchMapping);
        private void CreateChildSkillBranch(SkillBranch parentSkillBranch, SkillBranchMapping skillBranchMapping) => skillTree.CreateChildSkillBranch(parentSkillBranch, skillBranchMapping);
        
        private void RefreshSkillBranchMapping()
        {
            if (body == null) { return; }
            
            body.Clear();
            foreach (SkillBranchMapping skillBranchMapping in Enum.GetValues(typeof(SkillBranchMapping)))
            {
                VisualElement detailRow = MakeDetailRow(skillBranchMapping.ToString(), skillBranch.GetSkill(skillBranchMapping), ShouldShowAddButton(skillBranchMapping), out TextField skillField, out Button addButton);
                attachSkillAutocomplete?.Invoke(skillField);
                SkillNodeStyle.AddSkillDropTarget(skillField, IsAssignable);
                skillField?.RegisterValueChangedCallback(changeEvent => OnSkillNameChanged(skillField, addButton, skillBranchMapping, changeEvent.newValue));
                addButton?.RegisterCallback<ClickEvent>(_ =>
                {
                    CreateChildSkillBranch(skillBranch, skillBranchMapping);
                    onStructureChanged?.Invoke();
                });
                
                body.Add(detailRow);
            }
        }

        private void OnSkillNameChanged(TextField skillField, Button addButton, SkillBranchMapping skillBranchMapping, string newName)
        {
            if (!SkillNodeStyle.TryResolveTypedSkill(newName, resolveSkill, out Skill skill)) { skillField.SetValueWithoutNotify(SkillNodeStyle.GetSkillName(skillBranch.GetSkill(skillBranchMapping))); return; }

            skillBranch.SetSkill(SkillNodeStyle.GetSkillName(skill), skillBranchMapping);
            skillField.SetValueWithoutNotify(SkillNodeStyle.GetSkillName(skill));
            addButton.style.display = ShouldShowAddButton(skillBranchMapping) ? DisplayStyle.Flex : DisplayStyle.None;
            onSkillsChanged?.Invoke();
        }
        #endregion

        #region StaticUIBuilders
        private static VisualElement MakeDetailRow(string skillBranchMappingName, Skill currentSkill, bool shouldShowAddButton, out TextField skillField, out Button addButton)
        {
            VisualElement row = SkillNodeStyle.MakeRow();
            row.Add(new Label(skillBranchMappingName) { style = { width = 40f } });

            skillField = SkillNodeStyle.MakeSkillField(currentSkill);
            row.Add(skillField);

            addButton = SkillNodeStyle.MakeSmallButton("+", shouldShowAddButton);
            row.Add(addButton);
            return row;
        }
        #endregion
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Combat
{
    public class SkillHandler : MonoBehaviour
    {
        // Tunables
        [SerializeField] SkillTree skillTree = null;

        // State
        SkillBranch currentBranch = null;
        Skill activeSkill = null;

        public void SetBranch(SkillBranchMapping skillBranchMapping)
        {
            if (currentBranch == null) { ResetCurrentBranch(); }

            string nextBranch = currentBranch.GetBranch(skillBranchMapping);
            if (!string.IsNullOrWhiteSpace(nextBranch)) 
            {
                activeSkill = currentBranch.GetSkill(skillBranchMapping);
                currentBranch = skillTree.GetSkillBranchFromID(nextBranch); 
            }
        }

        public void ResetCurrentBranch()
        {
            currentBranch = skillTree.GetRootSkillBranch();
            activeSkill = null;
        }

        public void GetSkillsForCurrentBranch(out Skill up, out Skill left, out Skill right, out Skill down)
        {
            if (currentBranch == null) { ResetCurrentBranch(); }

            up = currentBranch.GetSkill(SkillBranchMapping.up);
            left = currentBranch.GetSkill(SkillBranchMapping.left);
            right = currentBranch.GetSkill(SkillBranchMapping.right);
            down = currentBranch.GetSkill(SkillBranchMapping.down);
        }

        public List<Skill> GetAvailableSkills()
        {
            if (currentBranch == null) { ResetCurrentBranch(); }

            List<Skill> availableSkills = new List<Skill>();
            if (currentBranch.HasSkill(SkillBranchMapping.up)) { availableSkills.Add(currentBranch.GetSkill(SkillBranchMapping.up)); }
            if (currentBranch.HasSkill(SkillBranchMapping.left)) { availableSkills.Add(currentBranch.GetSkill(SkillBranchMapping.left)); }
            if (currentBranch.HasSkill(SkillBranchMapping.right)) { availableSkills.Add(currentBranch.GetSkill(SkillBranchMapping.right)); }
            if (currentBranch.HasSkill(SkillBranchMapping.down)) { availableSkills.Add(currentBranch.GetSkill(SkillBranchMapping.down)); }

            return availableSkills;
        }

        public List<SkillBranchMapping> GetAvailableBranchMappings()
        {
            if (currentBranch == null) { ResetCurrentBranch(); }

            List<SkillBranchMapping> availableBranches = new List<SkillBranchMapping>();
            if (currentBranch.HasBranch(SkillBranchMapping.up)) { availableBranches.Add(SkillBranchMapping.up); }
            if (currentBranch.HasBranch(SkillBranchMapping.left)) { availableBranches.Add(SkillBranchMapping.left); }
            if (currentBranch.HasBranch(SkillBranchMapping.right)) { availableBranches.Add(SkillBranchMapping.right); }
            if (currentBranch.HasBranch(SkillBranchMapping.down)) { availableBranches.Add(SkillBranchMapping.down); }

            return availableBranches;
        }

        public Skill GetActiveSkill()
        {
            return activeSkill;
        }

    }
}

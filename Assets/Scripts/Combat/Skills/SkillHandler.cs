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

        public Skill GetActiveSkill()
        {
            return activeSkill;
        }

    }
}
